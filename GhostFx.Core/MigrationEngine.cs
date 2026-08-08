using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

    public async Task<MigrationResult> ExecuteAsync(GhostFxConfig config, string? jsonContentOverride = null, Action<int, int, string>? onProgress = null)
    {
        var result = new MigrationResult();

        try
        {
            List<GhostPost> allPosts = [];
            List<GhostTag> allTags = [];

            if (!string.IsNullOrWhiteSpace(jsonContentOverride))
            {
                var (posts, tags) = _jsonParser.ParseJsonExport(jsonContentOverride);
                allPosts = posts;
                allTags = tags;
            }
            else if (!string.IsNullOrWhiteSpace(config.InputJsonPath) && File.Exists(config.InputJsonPath))
            {
                string json = await File.ReadAllTextAsync(config.InputJsonPath);
                var (posts, tags) = _jsonParser.ParseJsonExport(json);
                allPosts = posts;
                allTags = tags;
            }
            else if (!string.IsNullOrWhiteSpace(config.GhostUrl) && !string.IsNullOrWhiteSpace(config.AdminApiKey))
            {
                var (head, foot) = await GhostAdminClient.GetCodeInjectionsAsync(config.GhostUrl, config.AdminApiKey);
                result.HeaderCodeInjection = head;
                result.FooterCodeInjection = foot;

                allPosts = await GhostAdminClient.FetchPostsFromApiAsync(config.GhostUrl, config.AdminApiKey, config.IncludeDrafts);
                allTags = allPosts.SelectMany(p => p.Tags).GroupBy(t => t.Id).Select(g => g.First()).ToList();

                if (config.DownloadTheme)
                {
                    await GhostAdminClient.DownloadActiveThemeAsync(config.GhostUrl, config.AdminApiKey, config.ThemeOutputPath);
                    result.GeneratedFiles.Add(config.ThemeOutputPath);
                }
            }
            else
            {
                result.Success = false;
                result.Message = "Missing credentials or input file. Provide inputJsonPath or GhostUrl + AdminApiKey.";
                return result;
            }

            Directory.CreateDirectory(config.OutputDir);

            var postsToProcess = allPosts
                .Where(p => p.Status == "published" || (config.IncludeDrafts && p.Status == "draft"))
                .ToList();

            List<BlogPostMetadata> publishedMetaList = [];
            List<BlogPostMetadata> draftMetaList = [];

            int totalPostsCount = postsToProcess.Count;
            for (int i = 0; i < totalPostsCount; i++)
            {
                var post = postsToProcess[i];
                onProgress?.Invoke(i + 1, totalPostsCount, string.IsNullOrWhiteSpace(post.Title) ? post.Slug : post.Title);

                bool isDraft = string.Equals(post.Status, "draft", StringComparison.OrdinalIgnoreCase);
                DateTime postDate = post.PublishedAt ?? post.CreatedAt ?? DateTime.UtcNow;
                string dateStr = postDate.ToString("yyyy-MM-dd");

                var tagNames = post.Tags.Select(t => t.Name).ToList();

                var frontMatter = new FrontMatter
                {
                    Uid = post.Slug,
                    Title = post.Title + (isDraft ? " (Draft)" : ""),
                    Slug = post.Slug,
                    Date = dateStr,
                    Tags = tagNames,
                    Description = !string.IsNullOrWhiteSpace(post.CustomExcerpt) ? post.CustomExcerpt : post.MetaDescription,
                    MetaTitle = !string.IsNullOrWhiteSpace(post.MetaTitle) ? post.MetaTitle : post.Title,
                    MetaDescription = !string.IsNullOrWhiteSpace(post.MetaDescription) ? post.MetaDescription : post.CustomExcerpt,
                    Image = !string.IsNullOrWhiteSpace(post.OgImage) ? post.OgImage : post.FeatureImage,
                    OgTitle = !string.IsNullOrWhiteSpace(post.OgTitle) ? post.OgTitle : post.Title,
                    OgDescription = !string.IsNullOrWhiteSpace(post.OgDescription) ? post.OgDescription : post.CustomExcerpt
                };

                string fullDoc = _converter.BuildFullMarkdownDocument(frontMatter, post.Html);

                string targetSubDir = isDraft ? Path.Combine(config.OutputDir, "drafts") : config.OutputDir;
                Directory.CreateDirectory(targetSubDir);

                string fileName = $"{post.Slug}.md";
                string filePath = Path.Combine(targetSubDir, fileName);

                await File.WriteAllTextAsync(filePath, fullDoc);
                result.GeneratedFiles.Add(filePath);

                var meta = new BlogPostMetadata
                {
                    Title = post.Title,
                    Slug = post.Slug,
                    Date = postDate,
                    FileName = isDraft ? $"drafts/{fileName}" : fileName,
                    Tags = tagNames,
                    IsDraft = isDraft
                };

                if (isDraft)
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

            // Generate Front Page (index.md)
            string indexPath = Path.Combine(Path.GetDirectoryName(config.OutputDir) ?? ".", config.IndexFile);
            GenerateFrontPage(indexPath, publishedMetaList, config.SiteTitle, config.OutputDir);
            result.GeneratedFiles.Add(indexPath);

            // Generate main Table of Contents (toc.yml)
            string tocPath = Path.Combine(config.OutputDir, "toc.yml");
            GenerateToc(tocPath, publishedMetaList);
            result.GeneratedFiles.Add(tocPath);

            // Handle drafts TOC
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
            string customTemplatePath = "templates/ghost-theme";
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
                await DocfxGenerator.ConvertGhostThemeToDocfxTemplateAsync(themeZipPath, templateDir, result.HeaderCodeInjection, result.FooterCodeInjection);
            }

            result.Success = true;
            result.Message = $"Migration completed successfully! Processed {result.ProcessedPosts} posts and {result.ProcessedDrafts} drafts.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Migration failed: {ex.Message}";
        }

        return result;
    }

    private void GenerateFrontPage(string indexFilePath, List<BlogPostMetadata> metaList, string siteTitle, string outputDir)
    {
        string relativeDir = Path.GetFileName(outputDir);
        var recentPosts = metaList.OrderByDescending(p => p.Date).Take(5).ToList();

        var postsListMarkdown = recentPosts.Count > 0
            ? string.Join("\n", recentPosts.Select(p => $"- [{p.Title}]({relativeDir}/{p.Slug}.html) — *{p.Date:yyyy-MM-dd}*"))
            : "_No posts available yet._";

        string indexContent = $"""
        ---
        uid: home
        title: "{siteTitle}"
        ---

        # Welcome to {siteTitle}

        This is a static blog migrated from Ghost and generated using Docfx.

        ## Latest Articles

        {postsListMarkdown}

        ---
        [Explore all articles in the archive]({relativeDir}/toc.yml) | [Browse by Tag](tags.md)
        """;

        var dir = Path.GetDirectoryName(indexFilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(indexFilePath, indexContent);
    }

    private void GenerateToc(string tocPath, List<BlogPostMetadata> metaList)
    {
        var tocEntries = metaList.OrderByDescending(p => p.Date).Select(p => new
        {
            name = p.Title,
            href = Path.GetFileName(p.FileName)
        }).ToList();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        string yaml = serializer.Serialize(tocEntries);
        var dir = Path.GetDirectoryName(tocPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(tocPath, yaml);
    }

    private void GenerateTagPages(string tagsOutputDir, List<BlogPostMetadata> metaList, List<GhostTag> allTags)
    {
        Directory.CreateDirectory(tagsOutputDir);

        var postsByTag = metaList
            .SelectMany(post => post.Tags.Select(tag => new { Tag = tag, Post = post }))
            .GroupBy(x => x.Tag)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Post).OrderByDescending(p => p.Date).ToList());

        foreach (var tagGroup in postsByTag)
        {
            string tagName = tagGroup.Key;
            var matchingPosts = tagGroup.Value;
            string tagSlug = allTags.FirstOrDefault(t => t.Name == tagName)?.Slug ?? tagName.ToLowerInvariant().Replace(" ", "-");

            var postsListMarkdown = string.Join("\n", matchingPosts.Select(p =>
                $"- [{p.Title}](../{p.Slug}.html) — *{p.Date:yyyy-MM-dd}*"
            ));

            string tagFileContent = $"""
            ---
            uid: tag-{tagSlug}
            title: "Articles tagged: {tagName}"
            ---

            # Articles Tagged with: {tagName}

            {postsListMarkdown}

            ---
            [Back to all Tags](../tags.md)
            """;

            File.WriteAllText(Path.Combine(tagsOutputDir, $"{tagSlug}.md"), tagFileContent);
        }

        var masterTagsMarkdown = string.Join("\n", postsByTag.OrderByDescending(t => t.Value.Count).Select(t =>
        {
            string tagSlug = allTags.FirstOrDefault(g => g.Name == t.Key)?.Slug ?? t.Key.ToLowerInvariant().Replace(" ", "-");
            return $"- [{t.Key} ({t.Value.Count})](articles/tags/{tagSlug}.html)";
        }));

        string masterTagsContent = $"""
        ---
        uid: tags-index
        title: "Browse by Tag"
        ---

        # Browse Content by Tag

        {(string.IsNullOrWhiteSpace(masterTagsMarkdown) ? "_No tags defined._" : masterTagsMarkdown)}
        """;

        File.WriteAllText("tags.md", masterTagsContent);
    }
}
