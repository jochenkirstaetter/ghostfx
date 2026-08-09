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
        Func<string, string, Task<bool>>? onManualThemeRequested = null)
    {
        var result = new MigrationResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            List<GhostPost> allPosts = [];
            List<GhostTag> allTags = [];
            string? siteDescription = null;
            string? siteIcon = null;
            string? siteLogo = null;
            string? siteCover = null;
            List<GhostNavItem> navItems = [];
            string? siteLocale = null;

            if (!string.IsNullOrWhiteSpace(jsonContentOverride))
            {
                var (posts, tags, jsonTitle, jsonDesc, jsonIcon, jsonLogo, jsonCover, jsonNav, jsonLocale) = _jsonParser.ParseJsonExport(jsonContentOverride);
                allPosts = posts;
                allTags = tags;
                if (jsonNav.Count > 0) navItems = jsonNav;
                if (!string.IsNullOrWhiteSpace(jsonTitle)) config.SiteTitle = jsonTitle;
                if (!string.IsNullOrWhiteSpace(jsonDesc)) siteDescription = jsonDesc;
                if (!string.IsNullOrWhiteSpace(jsonIcon)) siteIcon = jsonIcon;
                if (!string.IsNullOrWhiteSpace(jsonLogo)) siteLogo = jsonLogo;
                if (!string.IsNullOrWhiteSpace(jsonCover)) siteCover = jsonCover;
                if (!string.IsNullOrWhiteSpace(jsonLocale)) siteLocale = jsonLocale;
            }
            else if (!string.IsNullOrWhiteSpace(config.GhostExportJson) && File.Exists(config.GhostExportJson))
            {
                string json = await File.ReadAllTextAsync(config.GhostExportJson);
                var (posts, tags, jsonTitle, jsonDesc, jsonIcon, jsonLogo, jsonCover, jsonNav, jsonLocale) = _jsonParser.ParseJsonExport(json);
                allPosts = posts;
                allTags = tags;
                if (jsonNav.Count > 0) navItems = jsonNav;
                if (!string.IsNullOrWhiteSpace(jsonTitle)) config.SiteTitle = jsonTitle;
                if (!string.IsNullOrWhiteSpace(jsonDesc)) siteDescription = jsonDesc;
                if (!string.IsNullOrWhiteSpace(jsonIcon)) siteIcon = jsonIcon;
                if (!string.IsNullOrWhiteSpace(jsonLogo)) siteLogo = jsonLogo;
                if (!string.IsNullOrWhiteSpace(jsonCover)) siteCover = jsonCover;
                if (!string.IsNullOrWhiteSpace(jsonLocale)) siteLocale = jsonLocale;

                if (!string.IsNullOrWhiteSpace(config.GhostUrl) && (navItems.Count == 0 || string.IsNullOrWhiteSpace(config.SiteTitle) || string.IsNullOrWhiteSpace(siteIcon) || string.IsNullOrWhiteSpace(siteCover)))
                {
                    try
                    {
                        var (apiTitle, apiDesc, apiIcon, apiLogo, apiCover, apiNav, apiLocale) = await GhostAdminClient.FetchSiteBrandInfoAsync(config.GhostUrl, config.AdminApiKey ?? "");
                        if (navItems.Count == 0 && apiNav.Count > 0) navItems = apiNav;
                        if (string.IsNullOrWhiteSpace(config.SiteTitle) && !string.IsNullOrWhiteSpace(apiTitle)) config.SiteTitle = apiTitle;
                        if (string.IsNullOrWhiteSpace(siteDescription) && !string.IsNullOrWhiteSpace(apiDesc)) siteDescription = apiDesc;
                        if (string.IsNullOrWhiteSpace(siteIcon) && !string.IsNullOrWhiteSpace(apiIcon)) siteIcon = apiIcon;
                        if (string.IsNullOrWhiteSpace(siteLogo) && !string.IsNullOrWhiteSpace(apiLogo)) siteLogo = apiLogo;
                        if (string.IsNullOrWhiteSpace(siteCover) && !string.IsNullOrWhiteSpace(apiCover)) siteCover = apiCover;
                        if (string.IsNullOrWhiteSpace(siteLocale) && !string.IsNullOrWhiteSpace(apiLocale)) siteLocale = apiLocale;
                    }
                    catch { }
                }
            }
            else if (!string.IsNullOrWhiteSpace(config.GhostUrl) && !string.IsNullOrWhiteSpace(config.AdminApiKey))
            {
                var (head, foot) = await GhostAdminClient.GetCodeInjectionsAsync(config.GhostUrl, config.AdminApiKey);
                result.HeaderCodeInjection = head;
                result.FooterCodeInjection = foot;

                var (posts, version) = await GhostAdminClient.FetchPostsFromApiAsync(config.GhostUrl, config.AdminApiKey, config.IncludeDrafts);
                allPosts = posts;
                result.DetectedGhostVersion = version;
                allTags = allPosts.SelectMany(p => p.Tags).GroupBy(t => t.Id).Select(g => g.First()).ToList();

                var (apiTitle, apiDesc, apiIcon, apiLogo, apiCover, apiNav, apiLocale) = await GhostAdminClient.FetchSiteBrandInfoAsync(config.GhostUrl, config.AdminApiKey);
                if (apiNav.Count > 0) navItems = apiNav;
                if (!string.IsNullOrWhiteSpace(apiTitle)) config.SiteTitle = apiTitle;
                if (!string.IsNullOrWhiteSpace(apiDesc)) siteDescription = apiDesc;
                if (!string.IsNullOrWhiteSpace(apiIcon)) siteIcon = apiIcon;
                if (!string.IsNullOrWhiteSpace(apiLogo)) siteLogo = apiLogo;
                if (!string.IsNullOrWhiteSpace(apiCover)) siteCover = apiCover;
                if (!string.IsNullOrWhiteSpace(apiLocale)) siteLocale = apiLocale;
            }
            else
            {
                result.Success = false;
                result.Message = "Missing credentials or input file. Provide ghostExportJson or GhostUrl + AdminApiKey.";
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

                var frontMatter = new FrontMatter
                {
                    Uid = post.Slug,
                    Title = post.Title + titleSuffix,
                    Slug = post.Slug,
                    Date = dateStr,
                    Status = post.Status ?? (isDraft ? "draft" : (isScheduled ? "scheduled" : "published")),
                    Type = post.Type ?? (isPage ? "page" : "post"),
                    Tags = tagNames,
                    Description = !string.IsNullOrWhiteSpace(post.CustomExcerpt) ? post.CustomExcerpt : post.MetaDescription,
                    MetaTitle = !string.IsNullOrWhiteSpace(post.MetaTitle) ? post.MetaTitle : post.Title,
                    MetaDescription = !string.IsNullOrWhiteSpace(post.MetaDescription) ? post.MetaDescription : post.CustomExcerpt,
                    Image = !string.IsNullOrWhiteSpace(post.OgImage) ? post.OgImage : post.FeatureImage,
                    OgTitle = !string.IsNullOrWhiteSpace(post.OgTitle) ? post.OgTitle : post.Title,
                    OgDescription = !string.IsNullOrWhiteSpace(post.OgDescription) ? post.OgDescription : post.CustomExcerpt,
                    Layout = isPage ? "page" : "post",
                    IsPost = !isPage,
                    IsPage = isPage,
                    IsDraft = isDraft,
                    IsScheduled = isScheduled,
                    FeatureImage = !string.IsNullOrWhiteSpace(post.FeatureImage) ? post.FeatureImage : (!string.IsNullOrWhiteSpace(post.OgImage) ? post.OgImage : ""),
                    Featured = post.Featured,
                    PublishedAt = post.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssK") ?? "",
                    Excerpt = post.CustomExcerpt,
                    TwitterTitle = !string.IsNullOrWhiteSpace(post.TwitterTitle) ? post.TwitterTitle : post.Title,
                    TwitterDescription = !string.IsNullOrWhiteSpace(post.TwitterDescription) ? post.TwitterDescription : post.CustomExcerpt,
                    TwitterImage = post.TwitterImage,
                    FacebookTitle = !string.IsNullOrWhiteSpace(post.FacebookTitle) ? post.FacebookTitle : post.Title,
                    FacebookDescription = !string.IsNullOrWhiteSpace(post.FacebookDescription) ? post.FacebookDescription : post.CustomExcerpt,
                    FacebookImage = post.FacebookImage,
                    CodeinjectionHead = post.CodeinjectionHead,
                    CodeinjectionFoot = post.CodeinjectionFoot
                };

                string htmlContent = !string.IsNullOrWhiteSpace(post.Html)
                    ? post.Html
                    : (!string.IsNullOrWhiteSpace(post.CustomExcerpt) ? $"<p>{post.CustomExcerpt}</p>" : "");
                string fullDoc = _converter.BuildFullMarkdownDocument(frontMatter, htmlContent);

                string subDirName = isPage ? "pages" : (isDraft ? "draft" : (isScheduled ? "scheduled" : "published"));
                string targetSubDir = Path.Combine(config.OutputDir, subDirName);
                Directory.CreateDirectory(targetSubDir);

                string fileName = $"{post.Slug}.md";
                string filePath = Path.Combine(targetSubDir, fileName);

                await File.WriteAllTextAsync(filePath, fullDoc);
                result.GeneratedFiles.Add(filePath);

                string relativePathInToc = $"{subDirName}/{fileName}";

                var meta = new BlogPostMetadata
                {
                    Title = post.Title,
                    Slug = post.Slug,
                    Date = postDate,
                    FileName = relativePathInToc,
                    Tags = tagNames,
                    IsDraft = isDraft,
                    IsScheduled = isScheduled,
                    Type = post.Type ?? (isPage ? "page" : "post")
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
            GenerateFrontPage(indexPath, publishedMetaList, config.SiteTitle, siteDescription);
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

            // Generate Docfx configuration file inside outputDir
            string customTemplatePath = "ghostfx";
            string docfxPath = await DocfxGenerator.GenerateDocfxJsonIfNotExistsAsync(config.OutputDir, config, customTemplatePath, siteLocale ?? "en");
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
                await DocfxGenerator.ConvertGhostThemeToDocfxTemplateAsync(themePath, templateDir, result.HeaderCodeInjection, result.FooterCodeInjection);
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

    private static void GenerateFrontPage(string indexPath, List<BlogPostMetadata> posts, string siteTitle, string? siteDescription)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {siteTitle}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(siteDescription))
        {
            sb.AppendLine(siteDescription);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("Welcome to our documentation and articles blog repository.");
            sb.AppendLine();
        }
        sb.AppendLine("## Recent Articles");
        sb.AppendLine();

        foreach (var post in posts.OrderByDescending(p => p.Date).Take(10))
        {
            string link = post.FileName.Replace('\\', '/');
            sb.AppendLine($"- [{post.Title}]({link}) - *{post.Date:yyyy-MM-dd}*");
        }

        File.WriteAllText(indexPath, sb.ToString());
    }

    private static void GenerateToc(string tocPath, List<BlogPostMetadata> posts)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var post in posts.OrderByDescending(p => p.Date))
        {
            string fileName = Path.GetFileName(post.FileName);
            sb.AppendLine($"- name: \"{post.Title.Replace("\"", "\\\"")}\"");
            sb.AppendLine($"  href: {fileName}");
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

                string href = ResolveNavHref(url, pages, posts);
                string safeLabel = label.Contains(':') || label.Contains('#') ? $"\"{label.Replace("\"", "\\\"")}\"" : label;
                sb.AppendLine($"- name: {safeLabel}");
                sb.AppendLine($"  href: {href}");
            }
        }
        else if (pages != null && pages.Count > 0)
        {
            foreach (var page in pages)
            {
                string cleanLabel = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(page.Slug.Replace("-", " ").Replace("_", " "));
                string safeLabel = cleanLabel.Contains(':') || cleanLabel.Contains('#') ? $"\"{cleanLabel.Replace("\"", "\\\"")}\"" : cleanLabel;
                sb.AppendLine($"- name: {safeLabel}");
                sb.AppendLine($"  href: {page.FileName}");
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

    private static string ResolveNavHref(string url, List<BlogPostMetadata> pages, List<BlogPostMetadata> posts)
    {
        if (string.IsNullOrWhiteSpace(url) || url == "/" || url.Equals("home", StringComparison.OrdinalIgnoreCase))
        {
            return "index.md";
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
                return url;
            }
        }

        string slug = path.Trim('/').Split('/').LastOrDefault() ?? "";
        if (string.IsNullOrEmpty(slug)) return "index.md";

        var matchingPage = pages.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (matchingPage != null)
        {
            return matchingPage.FileName;
        }

        var matchingPost = posts.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (matchingPost != null)
        {
            return matchingPost.FileName;
        }

        if (slug.Equals("blog", StringComparison.OrdinalIgnoreCase) || slug.Equals("articles", StringComparison.OrdinalIgnoreCase))
        {
            return "published/toc.yml";
        }

        return $"published/{slug}.md";
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
                sb.AppendLine($"- [{post.Title}](../{post.FileName}) - *{post.Date:yyyy-MM-dd}*");
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
}
