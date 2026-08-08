using GhostFx.Core;
using Xunit;

namespace GhostFx.Tests;

public class MarkdownConverterTests
{
    [Fact]
    public void ConvertHtmlToMarkdown_ConvertsHeadingAndParagraph()
    {
        var converter = new MarkdownConverter();
        string html = "<h1>Heading 1</h1><p>This is <strong>bold</strong> and <em>italic</em>.</p>";
        string markdown = converter.ConvertHtmlToMarkdown(html);

        Assert.Contains("# Heading 1", markdown);
        Assert.Contains("**bold**", markdown);
        Assert.Contains("*italic*", markdown);
    }

    [Fact]
    public void GenerateYamlFrontmatter_ProducesValidYamlBlock()
    {
        var converter = new MarkdownConverter();
        var fm = new FrontMatter
        {
            Uid = "test-post",
            Title = "Test Post Title",
            Slug = "test-post-title",
            Date = "2026-08-08",
            Tags = ["csharp", "dotnet"],
            Description = "A test post description"
        };

        string yamlBlock = converter.GenerateYamlFrontmatter(fm);

        Assert.StartsWith("---\n", yamlBlock);
        Assert.EndsWith("---\n", yamlBlock);
        Assert.Contains("uid: test-post", yamlBlock);
        Assert.Contains("title: Test Post Title", yamlBlock);
        Assert.Contains("csharp", yamlBlock);
    }

    [Fact]
    public void BuildFullMarkdownDocument_CombinesFrontmatterAndBody()
    {
        var converter = new MarkdownConverter();
        var fm = new FrontMatter
        {
            Uid = "hello-world",
            Title = "Hello World",
            Slug = "hello-world",
            Date = "2026-08-08"
        };
        string html = "<h2>Welcome to GhostFx</h2><p>Testing doc conversion.</p>";

        string fullDoc = converter.BuildFullMarkdownDocument(fm, html);

        Assert.Contains("uid: hello-world", fullDoc);
        Assert.Contains("## Welcome to GhostFx", fullDoc);
    }
}
