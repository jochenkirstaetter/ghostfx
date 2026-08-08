using System;
using System.Collections.Generic;
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

        return _htmlConverter.Convert(html).Trim();
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
