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
            List<GhostNavItem> navItems = [];

            if (!string.IsNullOrWhiteSpace(jsonContentOverride))
            {
                var (posts, tags, jsonTitle, jsonDesc, jsonNav) = _jsonParser.ParseJsonExport(jsonContentOverride);
                allPosts = posts;
                allTags = tags;
                if (jsonNav.Count > 0) navItems = jsonNav;
                if (!string.IsNullOrWhiteSpace(jsonTitle)) config.SiteTitle = jsonTitle;
                if (!string.IsNullOrWhiteSpace(jsonDesc)) siteDescription = jsonDesc;
            }
            else if (!string.IsNullOrWhiteSpace(config.InputJsonPath) && File.Exists(config.InputJsonPath))
            {
                string json = await File.ReadAllTextAsync(config.InputJsonPath);
                var (posts, tags, jsonTitle, jsonDesc, jsonNav) = _jsonParser.ParseJsonExport(json);
                allPosts = posts;
                allTags = tags;
                if (jsonNav.Count > 0) navItems = jsonNav;
                if (!string.IsNullOrWhiteSpace(jsonTitle)) config.SiteTitle = jsonTitle;
                if (!string.IsNullOrWhiteSpace(jsonDesc)) siteDescription = jsonDesc;
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

                var (apiTitle, apiDesc, _, _, _, apiNav) = await GhostAdminClient.FetchSiteBrandInfoAsync(config.GhostUrl, config.AdminApiKey);
                if (apiNav.Count > 0) navItems = apiNav;
                if (!string.IsNullOrWhiteSpace(apiTitle)) config.SiteTitle = apiTitle;
                if (!string.IsNullOrWhiteSpace(apiDesc)) siteDescription = apiDesc;
            }
            else
            {
                result.Success = false;
                result.Message = "Missing credentials or input file. Provide inputJsonPath or GhostUrl + AdminApiKey.";
                return result;
            }

            if (!string.IsNullOrWhiteSpace(config.GhostUrl))
            {
                string siteRootDir = Path.GetDirectoryName(Path.GetFullPath(config.OutputDir)) ?? ".";
                var (favFile, logoFile, coverFile) = await GhostAdminClient.DownloadSiteBrandAssetsAsync(config.GhostUrl, config.AdminApiKey ?? "", siteRootDir);
                if (!string.IsNullOrEmpty(favFile) && !result.GeneratedFiles.Contains(favFile)) result.GeneratedFiles.Add(favFile);
                if (!string.IsNullOrEmpty(logoFile) && !result.GeneratedFiles.Contains(logoFile)) result.GeneratedFiles.Add(logoFile);
                if (!string.IsNullOrEmpty(coverFile) && !result.GeneratedFiles.Contains(coverFile)) result.GeneratedFiles.Add(coverFile);
            }

            if (allPosts.Count > 0)
            {
                string ghostBaseUrl = !string.IsNullOrWhiteSpace(config.GhostUrl) ? config.GhostUrl : "https://localhost";
                var mediaFiles = await MediaDownloader.ProcessAndDownloadMediaAsync(allPosts, ghostBaseUrl, config.OutputDir, onProgress);
                result.GeneratedFiles.AddRange(mediaFiles);
            }

            if (config.DownloadTheme)
            {
                if (File.Exists(config.ThemeOutputPath))
                {
                    if (!result.GeneratedFiles.Contains(config.ThemeOutputPath))
                    {
                        result.GeneratedFiles.Add(config.ThemeOutputPath);
                    }
                }
                else
                {
                    try
                    {
                        await GhostAdminClient.DownloadActiveThemeAsync(config.GhostUrl, config.AdminApiKey ?? "", config.ThemeOutputPath);
                        result.GeneratedFiles.Add(config.ThemeOutputPath);
                    }
                    catch (Exception ex)
                    {
                        bool manualProvided = false;
                        if (onManualThemeRequested != null)
                        {
                            manualProvided = await onManualThemeRequested(config.ThemeOutputPath, result.DetectedGhostVersion);
                        }

                        if (manualProvided && File.Exists(config.ThemeOutputPath))
                        {
                            if (!result.GeneratedFiles.Contains(config.ThemeOutputPath))
                            {
                                result.GeneratedFiles.Add(config.ThemeOutputPath);
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
                    OgDescription = !string.IsNullOrWhiteSpace(post.OgDescription) ? post.OgDescription : post.CustomExcerpt
                };

                string htmlContent = !string.IsNullOrWhiteSpace(post.Html)
                    ? post.Html
                    : (!string.IsNullOrWhiteSpace(post.CustomExcerpt) ? $"<p>{post.CustomExcerpt}</p>" : "");
                string fullDoc = _converter.BuildFullMarkdownDocument(frontMatter, htmlContent);

                string subDirName = (isDraft || isScheduled) ? "drafts" : "";
                string targetSubDir = !string.IsNullOrEmpty(subDirName) ? Path.Combine(config.OutputDir, subDirName) : config.OutputDir;
                Directory.CreateDirectory(targetSubDir);

                string fileName = $"{post.Slug}.md";
                string filePath = Path.Combine(targetSubDir, fileName);

                await File.WriteAllTextAsync(filePath, fullDoc);
                result.GeneratedFiles.Add(filePath);

                string relativePathInToc = !string.IsNullOrEmpty(subDirName) ? $"{subDirName}/{fileName}" : fileName;

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
                    draftMetaList.Add(meta);
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

            // Generate Root Table of Contents (toc.yml for top navbar)
            string rootDirForToc = Path.GetDirectoryName(Path.GetFullPath(config.OutputDir)) ?? ".";
            string rootTocPath = Path.Combine(rootDirForToc, "toc.yml");
            GenerateRootToc(rootTocPath, navItems, pageMetaList, publishedMetaList, config.OutputDir);
            if (!result.GeneratedFiles.Contains(rootTocPath)) result.GeneratedFiles.Add(rootTocPath);

            // Generate Front Page (index.md)
            string indexPath = Path.Combine(Path.GetDirectoryName(config.OutputDir) ?? ".", config.IndexFile);
            GenerateFrontPage(indexPath, publishedMetaList, config.SiteTitle, siteDescription, config.OutputDir);
            result.GeneratedFiles.Add(indexPath);

            // Generate main Table of Contents (toc.yml)
            string tocPath = Path.Combine(config.OutputDir, "toc.yml");
            GenerateToc(tocPath, publishedMetaList);
            result.GeneratedFiles.Add(tocPath);

            // Handle drafts & scheduled TOC
            if (draftMetaList.Count > 0)
            {
                string draftTocPath = Path.Combine(config.OutputDir, "drafts", "toc.yml");
                GenerateToc(draftTocPath, draftMetaList);
                result.GeneratedFiles.Add(draftTocPath);
            }

            // Generate Tag Index Pages
            string tagsOutputDir = Path.Combine(config.OutputDir, "tags");
            GenerateTagPages(tagsOutputDir, publishedMetaList, allTags);
            result.ProcessedTags = allTags.Count;

            // Generate Docfx configuration file if omitted/not existing
            string rootDir = Path.GetDirectoryName(Path.GetFullPath(config.OutputDir)) ?? ".";
            string customTemplatePath = "template/ghostfx";
            string docfxPath = await DocfxGenerator.GenerateDocfxJsonIfNotExistsAsync(rootDir, config, customTemplatePath);
            if (!result.GeneratedFiles.Contains(docfxPath))
            {
                result.GeneratedFiles.Add(docfxPath);
            }

            // Convert Active Ghost theme to Docfx template override if downloaded/available
            string themeZipPath = config.ThemeOutputPath;
            if (File.Exists(themeZipPath))
            {
                onProgress?.Invoke(totalPostsCount + 1, totalPostsCount + 1, "Converting active Ghost theme to DocFx template override");
                string templateDir = Path.Combine(rootDir, customTemplatePath);
                ConvertGhostThemeToDocfxTemplate(themeZipPath, templateDir);
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

    private static void ConvertGhostThemeToDocfxTemplate(string zipPath, string targetTemplateDir)
    {
        try
        {
            Directory.CreateDirectory(targetTemplateDir);
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith(".hbs", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                {
                    string destinationPath = Path.Combine(targetTemplateDir, entry.Name);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }
        catch
        {
            // Ignore theme extraction failure if zip is corrupted
        }
    }

    private static void GenerateFrontPage(string indexPath, List<BlogPostMetadata> posts, string siteTitle, string? siteDescription, string outputDir)
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
            string link = Path.Combine(outputDir, post.FileName).Replace('\\', '/');
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

    private static void GenerateRootToc(string rootTocPath, List<GhostNavItem> navItems, List<BlogPostMetadata> pages, List<BlogPostMetadata> posts, string outputDir)
    {
        var sb = new System.Text.StringBuilder();

        if (navItems != null && navItems.Count > 0)
        {
            foreach (var nav in navItems)
            {
                if (string.IsNullOrWhiteSpace(nav.Label)) continue;

                string label = nav.Label;
                string url = nav.Url?.Trim() ?? "";

                string href = ResolveNavHref(url, pages, posts, outputDir);
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
                sb.AppendLine($"  href: {outputDir}/{page.FileName}");
            }
        }
        else
        {
            sb.AppendLine("- name: Home");
            sb.AppendLine("  href: index.md");
            sb.AppendLine("- name: Articles");
            sb.AppendLine($"  href: {outputDir}/toc.yml");
        }

        File.WriteAllText(rootTocPath, sb.ToString());
    }

    private static string ResolveNavHref(string url, List<BlogPostMetadata> pages, List<BlogPostMetadata> posts, string outputDir)
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

        if (slug.Equals("blog", StringComparison.OrdinalIgnoreCase) || slug.Equals("articles", StringComparison.OrdinalIgnoreCase))
        {
            return $"{outputDir}/toc.yml";
        }

        var matchingPage = pages.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (matchingPage != null)
        {
            return $"{outputDir}/{matchingPage.FileName}";
        }

        var matchingPost = posts.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (matchingPost != null)
        {
            return $"{outputDir}/{matchingPost.FileName}";
        }

        return $"{outputDir}/{slug}.md";
    }

    private static void GenerateTagPages(string tagsDir, List<BlogPostMetadata> posts, List<GhostTag> allTags)
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

        // Generate root tags.md index page with .md relative links
        string fullTagsDir = Path.GetFullPath(tagsDir);
        string fullOutputDir = Path.GetDirectoryName(fullTagsDir) ?? tagsDir;
        string outputDirName = Path.GetFileName(fullOutputDir);
        string rootDir = Path.GetDirectoryName(fullOutputDir) ?? ".";
        string mainTagsPath = Path.Combine(rootDir, "tags.md");

        var mainTagsSb = new System.Text.StringBuilder();
        mainTagsSb.AppendLine("---");
        mainTagsSb.AppendLine("uid: tags-index");
        mainTagsSb.AppendLine("title: \"Browse by Tag\"");
        mainTagsSb.AppendLine("---");
        mainTagsSb.AppendLine();
        mainTagsSb.AppendLine("# Browse Content by Tag");
        mainTagsSb.AppendLine();

        var postsByTag = posts
            .SelectMany(p => p.Tags.Select(t => (Tag: t, Post: p)))
            .ToLookup(x => x.Tag, x => x.Post, StringComparer.OrdinalIgnoreCase);

        foreach (var tag in allTags.OrderBy(t => t.Name))
        {
            var tagPosts = postsByTag[tag.Name].ToList();
            if (tagPosts.Count == 0) continue;

            string relPath = $"{outputDirName}/tags/{tag.Slug}.md";
            mainTagsSb.AppendLine($"- [{tag.Name} ({tagPosts.Count})]({relPath})");
        }

        File.WriteAllText(mainTagsPath, mainTagsSb.ToString());
    }
}
