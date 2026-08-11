using System.Text.RegularExpressions;
using ReverseMarkdown;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GhostFx.Core;

public class MarkdownConverter
{
    private readonly Converter _htmlConverter;
    private readonly ISerializer _yamlSerializer;

    public MarkdownConverter()
    {
        _htmlConverter = new Converter(new Config
        {
            GithubFlavored = true
        });

        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public string ConvertHtmlToMarkdown(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        string processedHtml = Regex.Replace(html, @"<br\s*/?>", " GHOSTFXBRPLACEHOLDER ", RegexOptions.IgnoreCase);

        string markdown = _htmlConverter.Convert(processedHtml);
        markdown = Regex.Replace(markdown, @"\s*<figure", "\n\n<figure");
        markdown = Regex.Replace(markdown, @"</figure>\s*", "</figure>\n\n");
        markdown = Regex.Replace(markdown, @"\s*GHOSTFXBRPLACEHOLDER\s*", "  \n");

        return markdown.Trim();
    }

    public string GenerateYamlFrontmatter(FrontMatter frontMatter)
    {
        string yaml = _yamlSerializer.Serialize(frontMatter);
        return $"---\n{yaml}---\n";
    }

    public string BuildFullMarkdownDocument(FrontMatter frontMatter, string htmlContent)
    {
        string frontmatterYaml = GenerateYamlFrontmatter(frontMatter);
        string markdownBody = ConvertHtmlToMarkdown(htmlContent);
        return $"{frontmatterYaml}\n{markdownBody}\n";
    }
}
