using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace GhostFx.Core;

public class MigrationEngine
{
    private readonly MarkdownConverter _converter;
    private readonly GhostJsonParser _jsonParser;

    public MigrationEngine()
    {
        _converter = new MarkdownConverter();
        _jsonParser = new GhostJsonParser();
    }

    public async Task<MigrationResult> ExecuteAsync(
        GhostFxConfig config,
        string? jsonContentOverride = null,
        Action<int, int, string>? onProgress = null,
        Func<string, string, Task<bool>>? onManualThemeRequested = null,
        Func<string, Task<bool>>? onConfirmTemplatePurge = null)
    {
        var result = new MigrationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            List<GhostPost> allPosts = [];
            List<GhostTag> allTags = [];
            List<GhostUser> allUsers = [];
            string? siteDescription = null;
            string? siteIcon = null;
            string? siteLogo = null;
            string? siteCover = null;
            List<GhostNavItem> navItems = [];
            string? siteLocale = null;
            string? twitter = null;
            string? facebook = null;

            if (!string.IsNullOrWhiteSpace(jsonContentOverride))
            {
                var (posts, tags, users, jsonTitle, jsonDesc, jsonIcon, jsonLogo, jsonCover, jsonNav, jsonLocale, jsonTwitter, jsonFacebook, jsonHead, jsonFoot) = _jsonParser.ParseJsonExport(jsonContentOverride);
                allPosts = posts;
                allTags = tags;
                allUsers = users;
                if (jsonNav.Count > 0) navItems = jsonNav;
                if (!string.IsNullOrWhiteSpace(jsonTitle)) config.SiteTitle = jsonTitle;
                if (!string.IsNullOrWhiteSpace(jsonDesc)) siteDescription = jsonDesc;
                if (!string.IsNullOrWhiteSpace(jsonIcon)) siteIcon = jsonIcon;
                if (!string.IsNullOrWhiteSpace(jsonLogo)) siteLogo = jsonLogo;
                if (!string.IsNullOrWhiteSpace(jsonCover)) siteCover = jsonCover;
                if (!string.IsNullOrWhiteSpace(jsonLocale)) siteLocale = jsonLocale;
                if (!string.IsNullOrWhiteSpace(jsonTwitter)) twitter = jsonTwitter;
                if (!string.IsNullOrWhiteSpace(jsonFacebook)) facebook = jsonFacebook;
                if (!string.IsNullOrWhiteSpace(jsonHead)) result.HeaderCodeInjection = jsonHead;
                if (!string.IsNullOrWhiteSpace(jsonFoot)) result.FooterCodeInjection = jsonFoot;
            }
            else if (!string.IsNullOrWhiteSpace(config.GhostExportJson) && File.Exists(config.GhostExportJson))
            {
                string json = await File.ReadAllTextAsync(config.GhostExportJson);
                var (posts, tags, users, jsonTitle, jsonDesc, jsonIcon, jsonLogo, jsonCover, jsonNav, jsonLocale, jsonTwitter, jsonFacebook, jsonHead, jsonFoot) = _jsonParser.ParseJsonExport(json);
                allPosts = posts;
                allTags = tags;
                allUsers = users;
                if (jsonNav.Count > 0) navItems = jsonNav;
                if (!string.IsNullOrWhiteSpace(jsonTitle)) config.SiteTitle = jsonTitle;
                if (!string.IsNullOrWhiteSpace(jsonDesc)) siteDescription = jsonDesc;
                if (!string.IsNullOrWhiteSpace(jsonIcon)) siteIcon = jsonIcon;
                if (!string.IsNullOrWhiteSpace(jsonLogo)) siteLogo = jsonLogo;
                if (!string.IsNullOrWhiteSpace(jsonCover)) siteCover = jsonCover;
                if (!string.IsNullOrWhiteSpace(jsonLocale)) siteLocale = jsonLocale;
                if (!string.IsNullOrWhiteSpace(jsonTwitter)) twitter = jsonTwitter;
                if (!string.IsNullOrWhiteSpace(jsonFacebook)) facebook = jsonFacebook;
                if (!string.IsNullOrWhiteSpace(jsonHead)) result.HeaderCodeInjection = jsonHead;
                if (!string.IsNullOrWhiteSpace(jsonFoot)) result.FooterCodeInjection = jsonFoot;
 
                if (!string.IsNullOrWhiteSpace(config.GhostUrl) && (navItems.Count == 0 || string.IsNullOrWhiteSpace(config.SiteTitle) || string.IsNullOrWhiteSpace(siteIcon) || string.IsNullOrWhiteSpace(siteCover) || string.IsNullOrWhiteSpace(twitter) || string.IsNullOrWhiteSpace(facebook)))
                {
                    try
                    {
                        var (apiTitle, apiDesc, apiIcon, apiLogo, apiCover, apiNav, apiLocale, apiTwitter, apiFacebook, apiHead, apiFoot) =
                            !string.IsNullOrWhiteSpace(config.ContentApiKey)
                                ? await GhostAdminClient.FetchSiteSettingsViaContentApiAsync(config.GhostUrl, config.ContentApiKey)
                                : await GhostAdminClient.FetchSiteBrandInfoAsync(config.GhostUrl, config.AdminApiKey ?? "");
                        if (navItems.Count == 0 && apiNav.Count > 0) navItems = apiNav;
                        if (string.IsNullOrWhiteSpace(config.SiteTitle) && !string.IsNullOrWhiteSpace(apiTitle)) config.SiteTitle = apiTitle;
                        if (string.IsNullOrWhiteSpace(siteDescription) && !string.IsNullOrWhiteSpace(apiDesc)) siteDescription = apiDesc;
                        if (string.IsNullOrWhiteSpace(siteIcon) && !string.IsNullOrWhiteSpace(apiIcon)) siteIcon = apiIcon;
                        if (string.IsNullOrWhiteSpace(siteLogo) && !string.IsNullOrWhiteSpace(apiLogo)) siteLogo = apiLogo;
                        if (string.IsNullOrWhiteSpace(siteCover) && !string.IsNullOrWhiteSpace(apiCover)) siteCover = apiCover;
                        if (string.IsNullOrWhiteSpace(siteLocale) && !string.IsNullOrWhiteSpace(apiLocale)) siteLocale = apiLocale;
                        if (string.IsNullOrWhiteSpace(twitter) && !string.IsNullOrWhiteSpace(apiTwitter)) twitter = apiTwitter;
                        if (string.IsNullOrWhiteSpace(facebook) && !string.IsNullOrWhiteSpace(apiFacebook)) facebook = apiFacebook;
                        if (!string.IsNullOrWhiteSpace(apiHead)) result.HeaderCodeInjection = apiHead;
                        if (!string.IsNullOrWhiteSpace(apiFoot)) result.FooterCodeInjection = apiFoot;
                    }
                    catch { }
                }

                if (!string.IsNullOrWhiteSpace(config.GhostUrl) && !string.IsNullOrWhiteSpace(config.AdminApiKey))
                {
                    try
                    {
                        var (head, foot) = await GhostAdminClient.GetCodeInjectionsAsync(config.GhostUrl, config.AdminApiKey);
                        if (!string.IsNullOrWhiteSpace(head)) result.HeaderCodeInjection = head;
                        if (!string.IsNullOrWhiteSpace(foot)) result.FooterCodeInjection = foot;
                    }
                    catch { }
                }
            }
            else if (!string.IsNullOrWhiteSpace(config.GhostUrl) && (!string.IsNullOrWhiteSpace(config.AdminApiKey) || !string.IsNullOrWhiteSpace(config.ContentApiKey)))
            {
                List<GhostPost> posts = [];
                string version = "v5";
                bool fetchedFromAdmin = false;

                if (!string.IsNullOrWhiteSpace(config.AdminApiKey))
                {
                    try
                    {
                        var (head, foot) = await GhostAdminClient.GetCodeInjectionsAsync(config.GhostUrl, config.AdminApiKey);
                        result.HeaderCodeInjection = head;
                        result.FooterCodeInjection = foot;

                        var (adminPosts, ver) = await GhostAdminClient.FetchPostsFromApiAsync(config.GhostUrl, config.AdminApiKey, config.IncludeDrafts);
                        posts = adminPosts;
                        version = ver;
                        fetchedFromAdmin = true;
                    }
                    catch (Exception ex)
                    {
                        if (string.IsNullOrWhiteSpace(config.ContentApiKey))
                        {
                            throw new InvalidOperationException($"Ghost Admin API error ({ex.Message}). Check your Admin API Key or switch to Offline JSON Mode.");
                        }
                    }
                }

                if (!fetchedFromAdmin && !string.IsNullOrWhiteSpace(config.ContentApiKey))
                {
                    var (contentPosts, ver) = await GhostAdminClient.FetchPostsFromContentApiAsync(config.GhostUrl, config.ContentApiKey);
                    posts = contentPosts;
                    version = ver;
                }

                allPosts = posts;
                result.DetectedGhostVersion = version;
                allTags = allPosts.SelectMany(p => p.Tags ?? []).GroupBy(t => t.Id).Select(g => g.First()).ToList();

                try
                {
                    if (!string.IsNullOrWhiteSpace(config.AdminApiKey))
                    {
                        allUsers = await GhostAdminClient.FetchUsersFromApiAsync(config.GhostUrl, config.AdminApiKey);
                    }
                }
                catch { }

                try
                {
                    var (apiTitle, apiDesc, apiIcon, apiLogo, apiCover, apiNav, apiLocale, apiTwitter, apiFacebook, apiHead, apiFoot) =
                        !string.IsNullOrWhiteSpace(config.ContentApiKey)
                            ? await GhostAdminClient.FetchSiteSettingsViaContentApiAsync(config.GhostUrl, config.ContentApiKey)
                            : await GhostAdminClient.FetchSiteBrandInfoAsync(config.GhostUrl, config.AdminApiKey ?? "");
                    if (apiNav.Count > 0) navItems = apiNav;
                    if (!string.IsNullOrWhiteSpace(apiTitle)) config.SiteTitle = apiTitle;
                    if (!string.IsNullOrWhiteSpace(apiDesc)) siteDescription = apiDesc;
                    if (!string.IsNullOrWhiteSpace(apiIcon)) siteIcon = apiIcon;
                    if (!string.IsNullOrWhiteSpace(apiLogo)) siteLogo = apiLogo;
                    if (!string.IsNullOrWhiteSpace(apiCover)) siteCover = apiCover;
                    if (!string.IsNullOrWhiteSpace(apiLocale)) siteLocale = apiLocale;
                    if (!string.IsNullOrWhiteSpace(apiTwitter)) twitter = apiTwitter;
                    if (!string.IsNullOrWhiteSpace(apiFacebook)) facebook = apiFacebook;
                    if (!string.IsNullOrWhiteSpace(apiHead)) result.HeaderCodeInjection = apiHead;
                    if (!string.IsNullOrWhiteSpace(apiFoot)) result.FooterCodeInjection = apiFoot;
                }
                catch { }
            }
            else
            {
                result.Success = false;
                result.Message = "Missing credentials or input file. Provide ghostExportJson or GhostUrl + AdminApiKey / ContentApiKey.";
                return result;
            }

            if (allPosts.Count > 0)
            {
                string ghostBaseUrl = !string.IsNullOrWhiteSpace(config.GhostUrl) ? config.GhostUrl : "https://localhost";
                var mediaFiles = await MediaDownloader.ProcessAndDownloadMediaAsync(allPosts, ghostBaseUrl, config.OutputDir, onProgress);
                result.GeneratedFiles.AddRange(mediaFiles);
            }

            if (!string.IsNullOrWhiteSpace(config.GhostUrl) || !string.IsNullOrWhiteSpace(siteIcon) || !string.IsNullOrWhiteSpace(siteLogo) || !string.IsNullOrWhiteSpace(siteCover))
            {
                var (favFile, logoFile, coverFile) = await GhostAdminClient.DownloadSiteBrandAssetsAsync(config.GhostUrl ?? "", config.AdminApiKey ?? "", config.OutputDir, siteIcon, siteLogo, siteCover);
                if (!string.IsNullOrEmpty(favFile) && !result.GeneratedFiles.Contains(favFile)) result.GeneratedFiles.Add(favFile);
                if (!string.IsNullOrEmpty(logoFile) && !result.GeneratedFiles.Contains(logoFile)) result.GeneratedFiles.Add(logoFile);
                if (!string.IsNullOrEmpty(coverFile) && !result.GeneratedFiles.Contains(coverFile)) result.GeneratedFiles.Add(coverFile);
            }

            if (config.DownloadTheme)
            {
                if (File.Exists(config.ThemePath) || Directory.Exists(config.ThemePath))
                {
                    if (!result.GeneratedFiles.Contains(config.ThemePath))
                    {
                        result.GeneratedFiles.Add(config.ThemePath);
                    }
                }
                else
                {
                    try
                    {
                        await GhostAdminClient.DownloadActiveThemeAsync(config.GhostUrl ?? "", config.AdminApiKey ?? "", config.ThemePath);
                        result.GeneratedFiles.Add(config.ThemePath);
                    }
                    catch (Exception ex)
                    {
                        bool manualProvided = false;
                        if (onManualThemeRequested != null)
                        {
                            manualProvided = await onManualThemeRequested(config.ThemePath, result.DetectedGhostVersion);
                        }

                        if (manualProvided && (File.Exists(config.ThemePath) || Directory.Exists(config.ThemePath)))
                        {
                            if (!result.GeneratedFiles.Contains(config.ThemePath))
                            {
                                result.GeneratedFiles.Add(config.ThemePath);
                            }
                        }
                        else
                        {
                            result.ThemeDownloadWarning = $"Active theme API download unsupported by Ghost host ({ex.Message}). Using default DocFX modern theme template.";
                        }
                    }
                }
            }

            Directory.CreateDirectory(config.OutputDir);

            var postsToProcess = allPosts
                .Where(p => p.Status == "published" || (config.IncludeDrafts && (p.Status == "draft" || p.Status == "scheduled")))
                .ToList();

            List<BlogPostMetadata> publishedMetaList = [];
            List<BlogPostMetadata> pageMetaList = [];
            List<BlogPostMetadata> scheduledMetaList = [];
            List<BlogPostMetadata> draftMetaList = [];

            int totalPostsCount = postsToProcess.Count;
            for (int i = 0; i < totalPostsCount; i++)
            {
                var post = postsToProcess[i];
                onProgress?.Invoke(i + 1, totalPostsCount, string.IsNullOrWhiteSpace(post.Title) ? post.Slug : post.Title);

                bool isDraft = string.Equals(post.Status, "draft", StringComparison.OrdinalIgnoreCase);
                bool isScheduled = string.Equals(post.Status, "scheduled", StringComparison.OrdinalIgnoreCase);
                bool isPage = string.Equals(post.Type, "page", StringComparison.OrdinalIgnoreCase);

                DateTime postDate = post.PublishedAt ?? post.CreatedAt ?? DateTime.UtcNow;
                string dateStr = postDate.ToString("yyyy-MM-dd");

                var tagNames = post.Tags.Select(t => t.Name).ToList();

                string titleSuffix = isDraft ? " (Draft)" : (isScheduled ? " (Scheduled)" : "");

                string fallbackExcerpt = ExtractExcerpt(post.Html ?? "", config.ExcerptMaxLength);

                var primaryAuthor = post.Authors?.FirstOrDefault();
                string authorName = primaryAuthor?.Name ?? "";
                string authorTwitter = primaryAuthor?.Twitter ?? "";
                if (!string.IsNullOrWhiteSpace(authorTwitter) && !authorTwitter.StartsWith("@"))
                {
                    authorTwitter = "@" + authorTwitter;
                }
                string authorFacebook = primaryAuthor?.Facebook ?? "";
                if (!string.IsNullOrWhiteSpace(authorFacebook) && !authorFacebook.StartsWith("http"))
                {
                    authorFacebook = "https://facebook.com/" + authorFacebook;
                }
                string authorImage = primaryAuthor?.ProfileImage ?? "";
                string authorSlug = primaryAuthor?.Slug ?? "";

                string appUrl = config.GhostUrl?.TrimEnd('/') ?? "";
                string canonicalUrl = !string.IsNullOrWhiteSpace(appUrl) && !string.IsNullOrWhiteSpace(post.Slug) ? $"{appUrl}/{post.Slug}/" : "";

                string finalImage = !string.IsNullOrWhiteSpace(post.OgImage) ? post.OgImage : post.FeatureImage ?? "";
                string imageUrl = string.Empty;
                if (!string.IsNullOrWhiteSpace(finalImage))
                {
                    imageUrl = finalImage.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? finalImage : (string.IsNullOrWhiteSpace(appUrl) ? finalImage : $"{appUrl}/{finalImage.TrimStart('/')}");
                }

                string finalTwitterImg = !string.IsNullOrWhiteSpace(post.TwitterImage) ? post.TwitterImage : (!string.IsNullOrWhiteSpace(post.FeatureImage) ? post.FeatureImage : (!string.IsNullOrWhiteSpace(post.OgImage) ? post.OgImage : ""));
                string twitterImageUrl = string.Empty;
                if (!string.IsNullOrWhiteSpace(finalTwitterImg))
                {
                    twitterImageUrl = finalTwitterImg.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? finalTwitterImg : (string.IsNullOrWhiteSpace(appUrl) ? finalTwitterImg : $"{appUrl}/{finalTwitterImg.TrimStart('/')}");
                }

                string authorImageUrl = string.Empty;
                if (!string.IsNullOrWhiteSpace(authorImage))
                {
                    authorImageUrl = authorImage.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? authorImage : (string.IsNullOrWhiteSpace(appUrl) ? authorImage : $"{appUrl}/{authorImage.TrimStart('/')}");
                }
                else if (!string.IsNullOrWhiteSpace(appUrl))
                {
                    authorImageUrl = $"{appUrl}/content/images/2018/10/JoKi_StAubin_100px.jpg";
                }

                string authorPageUrl = !string.IsNullOrWhiteSpace(appUrl) && !string.IsNullOrWhiteSpace(authorSlug) ? $"{appUrl}/author/{authorSlug}/" : "";

                string cleanCustomExcerpt = CleanMetadataText(post.CustomExcerpt);
                string cleanMetaDesc = CleanMetadataText(post.MetaDescription);
                string cleanOgDesc = CleanMetadataText(post.OgDescription);
                string cleanTwitterDesc = CleanMetadataText(post.TwitterDescription);
                string cleanFacebookDesc = CleanMetadataText(post.FacebookDescription);

                string finalDescription = !string.IsNullOrWhiteSpace(cleanCustomExcerpt) ? cleanCustomExcerpt : (!string.IsNullOrWhiteSpace(cleanMetaDesc) ? cleanMetaDesc : fallbackExcerpt);
                string finalMetaDesc = !string.IsNullOrWhiteSpace(cleanMetaDesc) ? cleanMetaDesc : (!string.IsNullOrWhiteSpace(cleanCustomExcerpt) ? cleanCustomExcerpt : fallbackExcerpt);
                string finalOgDesc = !string.IsNullOrWhiteSpace(cleanOgDesc) ? cleanOgDesc : (!string.IsNullOrWhiteSpace(cleanCustomExcerpt) ? cleanCustomExcerpt : fallbackExcerpt);
                string finalTwitterDesc = !string.IsNullOrWhiteSpace(cleanTwitterDesc) ? cleanTwitterDesc : (!string.IsNullOrWhiteSpace(cleanCustomExcerpt) ? cleanCustomExcerpt : fallbackExcerpt);
                string finalFacebookDesc = !string.IsNullOrWhiteSpace(cleanFacebookDesc) ? cleanFacebookDesc : (!string.IsNullOrWhiteSpace(cleanCustomExcerpt) ? cleanCustomExcerpt : fallbackExcerpt);

                var classTags = tagNames.Select(t => "tag-" + t.ToLowerInvariant().Replace(" ", "-").Replace("_", "-"));
                string tagClasses = string.Join(" ", classTags);
                string calculatedBodyClass = isPage 
                    ? $"page-template page-{post.Slug} {tagClasses}".Trim() 
                    : $"post-template {tagClasses}".Trim();
                string calculatedPostClass = isPage 
                    ? $"post {tagClasses} page".Trim() 
                    : $"post {tagClasses}".Trim();

                var frontMatter = new FrontMatter
                {
                    Uid = post.Slug,
                    Title = post.Title + titleSuffix,
                    BodyClass = calculatedBodyClass,
                    PostClass = calculatedPostClass,
                    Slug = post.Slug,
                    Date = dateStr,
                    Status = post.Status ?? (isDraft ? "draft" : (isScheduled ? "scheduled" : "published")),
                    Type = post.Type ?? (isPage ? "page" : "post"),
                    Tags = tagNames,
                    Keywords = string.Join(", ", tagNames),
                    Author = authorName,
                    AuthorTwitter = authorTwitter,
                    AuthorFacebook = authorFacebook,
                    AuthorImage = authorImage,
                    AuthorSlug = authorSlug,
                    CanonicalUrl = canonicalUrl,
                    ImageUrl = imageUrl,
                    TwitterImageUrl = twitterImageUrl,
                    AuthorImageUrl = authorImageUrl,
                    AuthorPageUrl = authorPageUrl,
                    Description = finalDescription,
                    MetaTitle = !string.IsNullOrWhiteSpace(post.MetaTitle) ? post.MetaTitle : post.Title,
                    MetaDescription = finalMetaDesc,
                    Image = !string.IsNullOrWhiteSpace(post.OgImage) ? post.OgImage : (post.FeatureImage ?? ""),
                    OgTitle = !string.IsNullOrWhiteSpace(post.OgTitle) ? post.OgTitle : post.Title,
                    OgDescription = finalOgDesc,
                    Layout = isPage ? "page" : "post",
                    IsPost = !isPage,
                    IsPage = isPage,
                    IsDraft = isDraft,
                    IsScheduled = isScheduled,
                    FeatureImage = !string.IsNullOrWhiteSpace(post.FeatureImage) ? post.FeatureImage : (!string.IsNullOrWhiteSpace(post.OgImage) ? post.OgImage : ""),
                    Featured = post.Featured,
                    PublishedAt = post.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssK") ?? "",
                    UpdatedAt = post.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssK") ?? post.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssK") ?? "",
                    Excerpt = !string.IsNullOrWhiteSpace(cleanCustomExcerpt) ? cleanCustomExcerpt : fallbackExcerpt,
                    TwitterTitle = !string.IsNullOrWhiteSpace(post.TwitterTitle) ? post.TwitterTitle : post.Title,
                    TwitterDescription = finalTwitterDesc,
                    TwitterImage = post.TwitterImage,
                    FacebookTitle = !string.IsNullOrWhiteSpace(post.FacebookTitle) ? post.FacebookTitle : post.Title,
                    FacebookDescription = finalFacebookDesc,
                    FacebookImage = post.FacebookImage,
                    CodeinjectionHead = post.CodeinjectionHead,
                    CodeinjectionFoot = post.CodeinjectionFoot
                };

                string htmlContent = !string.IsNullOrWhiteSpace(post.Html)
                    ? post.Html
                    : (!string.IsNullOrWhiteSpace(post.CustomExcerpt) ? $"<p>{post.CustomExcerpt}</p>" : "");
                string fullDoc = _converter.BuildFullMarkdownDocument(frontMatter, htmlContent);

                string subDirName = isPage ? "pages" : (isDraft ? "draft" : (isScheduled ? "scheduled" : "published"));
                string targetSubDir = config.CleanUrls 
                    ? Path.Combine(config.OutputDir, subDirName, post.Slug) 
                    : Path.Combine(config.OutputDir, subDirName);
                
                Directory.CreateDirectory(targetSubDir);

                string fileName = config.CleanUrls ? "index.md" : $"{post.Slug}.md";
                string filePath = Path.Combine(targetSubDir, fileName);

                await File.WriteAllTextAsync(filePath, fullDoc);
                result.GeneratedFiles.Add(filePath);

                string relativePathInToc = config.CleanUrls 
                    ? $"{subDirName}/{post.Slug}/{fileName}" 
                    : $"{subDirName}/{fileName}";

                var meta = new BlogPostMetadata
                {
                    Title = post.Title,
                    Slug = post.Slug,
                    Date = postDate,
                    FileName = relativePathInToc,
                    Tags = tagNames,
                    IsDraft = isDraft,
                    IsScheduled = isScheduled,
                    Type = post.Type ?? (isPage ? "page" : "post"),
                    FeatureImage = post.FeatureImage ?? string.Empty,
                    Excerpt = !string.IsNullOrWhiteSpace(post.CustomExcerpt) ? post.CustomExcerpt : (frontMatter.Description ?? string.Empty),
                    AuthorName = authorName ?? string.Empty,
                    AuthorSlug = authorSlug ?? string.Empty,
                    AuthorImage = authorImage ?? string.Empty
                };

                if (isPage)
                {
                    pageMetaList.Add(meta);
                    result.ProcessedPages++;
                }
                else if (isScheduled)
                {
                    scheduledMetaList.Add(meta);
                    result.ProcessedScheduled++;
                }
                else if (isDraft)
                {
                    draftMetaList.Add(meta);
                    result.ProcessedDrafts++;
                }
                else
                {
                    publishedMetaList.Add(meta);
                    result.ProcessedPosts++;
                }
            }

            // Generate Front Page (index.md) inside outputDir
            string indexFileName = Path.GetFileName(config.IndexFile);
            string indexPath = Path.Combine(config.OutputDir, indexFileName);
            GenerateFrontPage(indexPath, publishedMetaList, config.SiteTitle, siteDescription, siteCover, config.IndexPostCount);
            result.GeneratedFiles.Add(indexPath);

            // Generate subfolder Table of Contents files
            if (publishedMetaList.Count > 0)
            {
                string pubTocPath = Path.Combine(config.OutputDir, "published", "toc.yml");
                GenerateToc(pubTocPath, publishedMetaList);
                result.GeneratedFiles.Add(pubTocPath);
            }
            if (pageMetaList.Count > 0)
            {
                string pageTocPath = Path.Combine(config.OutputDir, "pages", "toc.yml");
                GenerateToc(pageTocPath, pageMetaList);
                result.GeneratedFiles.Add(pageTocPath);
            }
            if (draftMetaList.Count > 0)
            {
                string draftTocPath = Path.Combine(config.OutputDir, "draft", "toc.yml");
                GenerateToc(draftTocPath, draftMetaList);
                result.GeneratedFiles.Add(draftTocPath);
            }
            if (scheduledMetaList.Count > 0)
            {
                string scheduledTocPath = Path.Combine(config.OutputDir, "scheduled", "toc.yml");
                GenerateToc(scheduledTocPath, scheduledMetaList);
                result.GeneratedFiles.Add(scheduledTocPath);
            }

            // Generate Root Table of Contents (toc.yml for top navbar) inside outputDir
            string rootTocPath = Path.Combine(config.OutputDir, "toc.yml");
            if (navItems.Count > 0 || pageMetaList.Count > 0)
            {
                GenerateRootToc(rootTocPath, navItems, pageMetaList, publishedMetaList);
            }
            else
            {
                GenerateMainOutputDirToc(rootTocPath, publishedMetaList, pageMetaList, draftMetaList, scheduledMetaList);
            }
            if (!result.GeneratedFiles.Contains(rootTocPath)) result.GeneratedFiles.Add(rootTocPath);

            // Generate Tag Index Pages inside outputDir
            string tagsOutputDir = Path.Combine(config.OutputDir, "tags");
            GenerateTagPages(tagsOutputDir, config.OutputDir, publishedMetaList, allTags);
            result.ProcessedTags = allTags.Count;
            string mainTagsPath = Path.Combine(config.OutputDir, "tags.md");
            if (!result.GeneratedFiles.Contains(mainTagsPath)) result.GeneratedFiles.Add(mainTagsPath);

            // Generate Author Index Pages inside outputDir/author/
            if (allUsers.Count == 0)
            {
                allUsers = allPosts
                    .SelectMany(p => p.Authors ?? [])
                    .GroupBy(u => u.Id)
                    .Select(g => g.First())
                    .ToList();
            }

            if (allUsers.Count > 0)
            {
                string authorsOutputDir = Path.Combine(config.OutputDir, "author");
                Directory.CreateDirectory(authorsOutputDir);

                foreach (var author in allUsers)
                {
                    if (string.IsNullOrWhiteSpace(author.Slug)) continue;

                    string authorFilePath = Path.Combine(authorsOutputDir, $"{author.Slug}.md");

                    var authorFrontMatter = new FrontMatter
                    {
                        Uid = author.Slug,
                        Title = author.Name,
                        Slug = author.Slug,
                        Layout = "author",
                        BodyClass = $"author-template author-{author.Slug}",
                        IsPost = false,
                        IsPage = false,
                        IsTagPage = false,
                        IsTagsIndexPage = false,
                        IsAuthorPage = true,
                        MetaTitle = !string.IsNullOrWhiteSpace(author.MetaTitle) ? author.MetaTitle : author.Name,
                        MetaDescription = author.MetaDescription,
                        Description = author.Bio,
                        Image = author.CoverImage,
                        FeatureImage = author.ProfileImage
                    };

                    var authorPosts = publishedMetaList
                        .Where(p => string.Equals(p.AuthorSlug, author.Slug, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(p.AuthorName, author.Name, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(p => p.Date)
                        .ToList();

                    string yaml = _converter.GenerateYamlFrontmatter(authorFrontMatter);
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine(yaml);
                    sb.AppendLine($"# {author.Name}");
                    sb.AppendLine();
                    if (!string.IsNullOrWhiteSpace(author.Bio))
                    {
                        sb.AppendLine(author.Bio);
                        sb.AppendLine();
                    }

                    if (authorPosts.Count > 0)
                    {
                        sb.AppendLine("## Articles");
                        sb.AppendLine();
                        foreach (var post in authorPosts)
                        {
                            sb.AppendLine($"- [{post.Title}](xref:{post.Slug}) - *{post.Date:yyyy-MM-dd}*");
                        }
                    }

                    await File.WriteAllTextAsync(authorFilePath, sb.ToString());
                    result.GeneratedFiles.Add(authorFilePath);
                }
            }

            // Generate Docfx configuration file inside outputDir
            string customTemplatePath = "ghostfx";
            List<IconLink> iconLinks = [
                new IconLink { Icon = "github", Href = "https://github.com/jochenkirstaetter/ghostfx", Title = "GitHub" }
            ];

            if (!string.IsNullOrWhiteSpace(twitter))
            {
                string handle = twitter.TrimStart('@').Trim();
                string href = handle.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? handle : $"https://x.com/{handle}";
                iconLinks.Add(new IconLink { Icon = "twitter", Href = href, Title = "Twitter / X" });
            }

            if (!string.IsNullOrWhiteSpace(facebook))
            {
                string handle = facebook.Trim();
                string href = handle.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? handle : $"https://facebook.com/{handle}";
                iconLinks.Add(new IconLink { Icon = "facebook", Href = href, Title = "Facebook" });
            }

            foreach (var nav in navItems)
            {
                if (string.IsNullOrWhiteSpace(nav.Url)) continue;
                string url = nav.Url.Trim();
                if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    string iconName = DocfxGenerator.GetSocialIconFromUrl(url);
                    if (!string.IsNullOrEmpty(iconName))
                    {
                        if (!iconLinks.Any(l => string.Equals(l.Icon, iconName, StringComparison.OrdinalIgnoreCase)))
                        {
                            iconLinks.Add(new IconLink { Icon = iconName, Href = url, Title = nav.Label ?? iconName });
                        }
                    }
                }
            }

            string docfxPath = await DocfxGenerator.GenerateDocfxJsonIfNotExistsAsync(
                config.OutputDir,
                config,
                customTemplatePath,
                siteLocale ?? "en",
                iconLinks,
                result.HeaderCodeInjection,
                result.FooterCodeInjection,
                twitter,
                facebook,
                navItems,
                pageMetaList,
                publishedMetaList);
            if (!result.GeneratedFiles.Contains(docfxPath))
            {
                result.GeneratedFiles.Add(docfxPath);
            }

            // Convert Active Ghost theme to Docfx template override if downloaded/available
            string themePath = config.ThemePath;
            if (config.MigrateTheme && (File.Exists(themePath) || Directory.Exists(themePath)))
            {
                onProgress?.Invoke(totalPostsCount + 1, totalPostsCount + 1, "Converting active Ghost theme to DocFx template override");
                string templateDir = Path.Combine(config.OutputDir, customTemplatePath);
                await DocfxGenerator.ConvertGhostThemeToDocfxTemplateAsync(themePath, templateDir, result.HeaderCodeInjection, result.FooterCodeInjection, onConfirmTemplatePurge, iconLinks, navItems, pageMetaList, publishedMetaList);
            }

            stopwatch.Stop();
            result.ElapsedDuration = stopwatch.Elapsed;
            result.Success = true;
            result.Message = $"Migration completed successfully for {result.ProcessedPosts} posts, {result.ProcessedPages} pages, {result.ProcessedDrafts} drafts, and {result.ProcessedScheduled} scheduled items.";
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ElapsedDuration = stopwatch.Elapsed;
            result.Success = false;
            result.Message = $"Migration failed: {ex.Message}";
            return result;
        }
    }

    private static void GenerateFrontPage(string indexPath, List<BlogPostMetadata> posts, string siteTitle, string? siteDescription, string? siteCoverImage, int indexPostCount = 12)
    {
        var cardItems = new List<PostCardItem>();
        var recentPosts = posts.OrderByDescending(p => p.Date).Take(indexPostCount).ToList();
        foreach (var post in recentPosts)
        {
            string primaryTag = post.Tags.FirstOrDefault() ?? "";
            string tagClass = !string.IsNullOrEmpty(primaryTag) ? $"tag-{primaryTag.ToLowerInvariant().Replace(" ", "-").Replace("_", "-")}" : "";
            string imageClass = !string.IsNullOrWhiteSpace(post.FeatureImage) ? "with-image" : "no-image";

            cardItems.Add(new PostCardItem
            {
                Title = post.Title,
                Slug = post.Slug,
                Date = post.Date.ToString("yyyy-MM-dd"),
                FormattedDate = post.Date.ToString("MMM d, yyyy"),
                FeatureImage = post.FeatureImage,
                Excerpt = post.Excerpt,
                AuthorName = post.AuthorName,
                AuthorSlug = post.AuthorSlug,
                AuthorImage = post.AuthorImage,
                PrimaryTag = primaryTag,
                TagClass = tagClass,
                ImageClass = imageClass
            });
        }

        string coverRel = siteCoverImage ?? "";
        if (!string.IsNullOrWhiteSpace(coverRel))
        {
            coverRel = coverRel.Replace('\\', '/');
            if (coverRel.StartsWith("posts/")) coverRel = coverRel.Substring(6);
        }

        var indexFm = new IndexFrontMatter
        {
            Title = siteTitle,
            Description = siteDescription ?? "",
            CoverImage = coverRel,
            IsHome = true,
            BodyClass = "home-template",
            Posts = cardItems
        };

        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .ConfigureDefaultValuesHandling(YamlDotNet.Serialization.DefaultValuesHandling.OmitEmptyCollections | YamlDotNet.Serialization.DefaultValuesHandling.OmitNull)
            .Build();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine(serializer.Serialize(indexFm).Trim());
        sb.AppendLine("---");
        sb.AppendLine();

        File.WriteAllText(indexPath, sb.ToString());
    }

    private static void GenerateToc(string tocPath, List<BlogPostMetadata> posts)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var post in posts.OrderByDescending(p => p.Date))
        {
            string href = post.FileName;
            int slashIndex = href.IndexOf('/');
            if (slashIndex >= 0)
            {
                href = href.Substring(slashIndex + 1);
            }
            sb.AppendLine($"- name: \"{post.Title.Replace("\"", "\\\"")}\"");
            sb.AppendLine($"  href: {href}");
        }

        File.WriteAllText(tocPath, sb.ToString());
    }

    private static void GenerateMainOutputDirToc(
        string tocPath,
        List<BlogPostMetadata> published,
        List<BlogPostMetadata> pages,
        List<BlogPostMetadata> drafts,
        List<BlogPostMetadata> scheduled)
    {
        var sb = new System.Text.StringBuilder();
        if (published.Count > 0)
        {
            sb.AppendLine("- name: Published");
            sb.AppendLine("  href: published/toc.yml");
        }
        if (pages.Count > 0)
        {
            sb.AppendLine("- name: Pages");
            sb.AppendLine("  href: pages/toc.yml");
        }
        if (drafts.Count > 0)
        {
            sb.AppendLine("- name: Drafts");
            sb.AppendLine("  href: draft/toc.yml");
        }
        if (scheduled.Count > 0)
        {
            sb.AppendLine("- name: Scheduled");
            sb.AppendLine("  href: scheduled/toc.yml");
        }

        if (sb.Length == 0)
        {
            sb.AppendLine("- name: Published");
            sb.AppendLine("  href: published/toc.yml");
        }

        File.WriteAllText(tocPath, sb.ToString());
    }

    private static void GenerateRootToc(string rootTocPath, List<GhostNavItem> navItems, List<BlogPostMetadata> pages, List<BlogPostMetadata> posts)
    {
        var sb = new System.Text.StringBuilder();

        if (navItems != null && navItems.Count > 0)
        {
            foreach (var nav in navItems)
            {
                if (string.IsNullOrWhiteSpace(nav.Label)) continue;

                string label = nav.Label;
                string url = nav.Url?.Trim() ?? "";

                var (href, uid) = ResolveNavHrefOrUid(url, pages, posts);
                string safeLabel = label.Contains(':') || label.Contains('#') ? $"\"{label.Replace("\"", "\\\"")}\"" : label;
                sb.AppendLine($"- name: {safeLabel}");
                if (!string.IsNullOrEmpty(uid))
                {
                    sb.AppendLine($"  uid: {uid}");
                }
                else
                {
                    sb.AppendLine($"  href: {href}");
                }
            }
        }
        else if (pages != null && pages.Count > 0)
        {
            foreach (var page in pages)
            {
                string cleanLabel = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(page.Slug.Replace("-", " ").Replace("_", " "));
                string safeLabel = cleanLabel.Contains(':') || cleanLabel.Contains('#') ? $"\"{cleanLabel.Replace("\"", "\\\"")}\"" : cleanLabel;
                sb.AppendLine($"- name: {safeLabel}");
                sb.AppendLine($"  uid: {page.Slug}");
            }
        }
        else
        {
            sb.AppendLine("- name: Home");
            sb.AppendLine("  href: index.md");
            sb.AppendLine("- name: Articles");
            sb.AppendLine("  href: published/toc.yml");
        }

        File.WriteAllText(rootTocPath, sb.ToString());
    }

    private static (string? href, string? uid) ResolveNavHrefOrUid(string url, List<BlogPostMetadata> pages, List<BlogPostMetadata> posts)
    {
        if (string.IsNullOrWhiteSpace(url) || url == "/" || url.Equals("home", StringComparison.OrdinalIgnoreCase))
        {
            return ("index.md", null);
        }

        string path = url;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(url);
                path = uri.AbsolutePath;
            }
            catch
            {
                return (url, null);
            }
        }

        string slug = path.Trim('/').Split('/').LastOrDefault() ?? "";
        if (string.IsNullOrEmpty(slug)) return ("index.md", null);

        var matchingPage = pages.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (matchingPage != null)
        {
            return (null, matchingPage.Slug);
        }

        var matchingPost = posts.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (matchingPost != null)
        {
            return (null, matchingPost.Slug);
        }

        if (slug.Equals("blog", StringComparison.OrdinalIgnoreCase) || slug.Equals("articles", StringComparison.OrdinalIgnoreCase))
        {
            return ("published/toc.yml", null);
        }

        return (null, slug);
    }

    private static void GenerateTagPages(string tagsDir, string outputDir, List<BlogPostMetadata> posts, List<GhostTag> allTags)
    {
        Directory.CreateDirectory(tagsDir);

        var postsByTag = posts
            .SelectMany(p => p.Tags.Select(t => (Tag: t, Post: p)))
            .ToLookup(x => x.Tag, x => x.Post, StringComparer.OrdinalIgnoreCase);

        foreach (var tag in allTags)
        {
            var tagPosts = postsByTag[tag.Name].ToList();
            if (tagPosts.Count == 0) continue;

            string tagFilePath = Path.Combine(tagsDir, $"{tag.Slug}.md");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine($"uid: tag-{tag.Slug}");
            sb.AppendLine($"title: \"Tag: {tag.Name.Replace("\"", "\\\"")}\"");
            sb.AppendLine("layout: tag");
            sb.AppendLine("isTagPage: true");
            sb.AppendLine($"bodyClass: \"tag-template tag-{tag.Slug}\"");
            sb.AppendLine($"tagName: \"{tag.Name.Replace("\"", "\\\"")}\"");
            if (!string.IsNullOrWhiteSpace(tag.Description))
            {
                sb.AppendLine($"tagDescription: \"{tag.Description.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "")}\"");
            }
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine($"# Tag: {tag.Name}");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(tag.Description))
            {
                sb.AppendLine(tag.Description);
                sb.AppendLine();
            }

            sb.AppendLine("## Articles");
            sb.AppendLine();
            foreach (var post in tagPosts.OrderByDescending(p => p.Date))
            {
                sb.AppendLine($"- [{post.Title}](xref:{post.Slug}) - *{post.Date:yyyy-MM-dd}*");
            }

            File.WriteAllText(tagFilePath, sb.ToString());
        }

        // Generate tags/toc.yml
        string tagsTocPath = Path.Combine(tagsDir, "toc.yml");
        var tocSb = new System.Text.StringBuilder();
        foreach (var tag in allTags.OrderBy(t => t.Name))
        {
            string tagFilePath = Path.Combine(tagsDir, $"{tag.Slug}.md");
            if (File.Exists(tagFilePath))
            {
                tocSb.AppendLine($"- name: \"{tag.Name.Replace("\"", "\\\"")}\"");
                tocSb.AppendLine($"  href: {tag.Slug}.md");
            }
        }
        File.WriteAllText(tagsTocPath, tocSb.ToString());

        // Generate root tags.md index page in outputDir with .md relative links
        string mainTagsPath = Path.Combine(outputDir, "tags.md");

        var mainTagsSb = new System.Text.StringBuilder();
        mainTagsSb.AppendLine("---");
        mainTagsSb.AppendLine("uid: tags-index");
        mainTagsSb.AppendLine("title: \"Browse by Tag\"");
        mainTagsSb.AppendLine("layout: tags");
        mainTagsSb.AppendLine("isTagsIndexPage: true");
        mainTagsSb.AppendLine("bodyClass: \"tag-template tag-index-template\"");
        mainTagsSb.AppendLine("---");
        mainTagsSb.AppendLine();
        mainTagsSb.AppendLine("# Browse Content by Tag");
        mainTagsSb.AppendLine();

        foreach (var tag in allTags.OrderBy(t => t.Name))
        {
            var tagPosts = postsByTag[tag.Name].ToList();
            if (tagPosts.Count == 0) continue;

            string relPath = $"tags/{tag.Slug}.md";
            mainTagsSb.AppendLine($"- [{tag.Name} ({tagPosts.Count})]({relPath})");
        }

        File.WriteAllText(mainTagsPath, mainTagsSb.ToString());
    }

    private static string CleanMetadataText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Replace newlines/carriage returns and multiple spaces with a single space
        string clean = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        
        // Replace double quotes with single quotes to be safe in HTML attributes and JSON-LD
        clean = clean.Replace("\"", "'");

        return clean;
    }

    private static string ExtractExcerpt(string html, int softLimit = 200)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        string targetHtml = html;

        // Extract first paragraph <p>...</p> if present
        var match = System.Text.RegularExpressions.Regex.Match(html, @"<p\b[^>]*>(.*?)</p>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
        {
            targetHtml = match.Groups[1].Value;
        }
        else
        {
            // Truncate at double line break or multiple <br> breaks
            string normalizedBreak = System.Text.RegularExpressions.Regex.Replace(html, @"(<br\s*/?>\s*){2,}", "\n\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            int breakIdx = normalizedBreak.IndexOf("\n\n", StringComparison.Ordinal);
            if (breakIdx > 0)
            {
                targetHtml = normalizedBreak.Substring(0, breakIdx);
            }
        }

        // Strip HTML tags using regex
        string clean = System.Text.RegularExpressions.Regex.Replace(targetHtml, "<.*?>", string.Empty);
        
        // Replace multiple spaces/newlines with single space
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", " ").Trim();

        // Decode HTML entities
        clean = System.Net.WebUtility.HtmlDecode(clean);

        if (clean.Length <= softLimit)
            return clean;

        // Truncate at last complete word boundary on or before softLimit
        int lastSpace = clean.LastIndexOf(' ', softLimit);
        int truncateIndex = lastSpace > 0 ? lastSpace : softLimit;

        return clean.Substring(0, truncateIndex).TrimEnd('.', ',', ';', ':', '!', '?', ' ') + "...";
    }
}
