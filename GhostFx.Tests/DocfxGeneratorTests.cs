using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using GhostFx.Core;
using Xunit;

namespace GhostFx.Tests;

public class DocfxGeneratorTests : IDisposable
{
    private readonly string _tempDirectory;

    public DocfxGeneratorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GhostFx_DocfxTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try { Directory.Delete(_tempDirectory, true); } catch { }
        }
    }

    [Fact]
    public async Task GenerateDocfxJsonIfNotExistsAsync_CreatesDocfxJsonWithModernTheme()
    {
        var config = new GhostFxConfig
        {
            OutputDir = "articles",
            IndexFile = "index.md",
            SiteTitle = "Test Blog"
        };

        string docfxPath = await DocfxGenerator.GenerateDocfxJsonIfNotExistsAsync(_tempDirectory, config);

        Assert.True(File.Exists(docfxPath));
        string jsonContent = await File.ReadAllTextAsync(docfxPath);

        Assert.Contains("\"modern\"", jsonContent);
        Assert.Contains("Test Blog", jsonContent);
        Assert.Contains("\"published\"", jsonContent);
        Assert.Contains("\"pages\"", jsonContent);
        Assert.Contains("**/content/images/**", jsonContent);
        Assert.Contains("*.png", jsonContent);
        Assert.Contains("_site/**", jsonContent);
        Assert.Contains("\"_disableContribution\": true", jsonContent);
        Assert.Contains("\"_disableBreadcrumb\": true", jsonContent);
        Assert.Contains("\"_lang\": \"en\"", jsonContent);
    }

    [Fact]
    public async Task GenerateDocfxJsonIfNotExistsAsync_OmitsIfAlreadyExists()
    {
        var config = new GhostFxConfig
        {
            OutputDir = "articles",
            IndexFile = "index.md",
            SiteTitle = "New Blog Title"
        };

        string docfxPath = Path.Combine(_tempDirectory, "docfx.json");
        string existingContent = "{\"existing\": true}";
        await File.WriteAllTextAsync(docfxPath, existingContent);

        string resultPath = await DocfxGenerator.GenerateDocfxJsonIfNotExistsAsync(_tempDirectory, config);

        Assert.Equal(docfxPath, resultPath);
        string fileContentAfter = await File.ReadAllTextAsync(docfxPath);
        Assert.Equal(existingContent, fileContentAfter);
    }

    [Fact]
    public void ConvertHandlebarsToDocfx_ConvertsGhostTagsToDocfxSyntax()
    {
        string hbsInput = """
        <!DOCTYPE html>
        <html>
        <head>
            <title>{{title}} - {{@site.title}}</title>
            {{ghost_head}}
        </head>
        <body>
            {{#post}}
            <h1>{{title}}</h1>
            <div class="content">{{content}}</div>
            {{/post}}
            {{ghost_foot}}
        </body>
        </html>
        """;

        string converted = DocfxGenerator.ConvertHandlebarsToDocfx(hbsInput);

        Assert.Contains("{{_appTitle}}", converted);
        Assert.Contains("<!-- DocFx Head Injection -->", converted);
        Assert.Contains("<!-- DocFx Foot Injection -->", converted);
        Assert.Contains("<article class=\"ghost-post-container\">", converted);
        Assert.Contains("</article>", converted);
        Assert.Contains("{{{conceptual}}}", converted);
    }

    [Fact]
    public async Task ConvertGhostThemeToDocfxTemplateAsync_ExtractsAndConvertsAssetsForModernTemplate()
    {
        string themeZipPath = Path.Combine(_tempDirectory, "theme.zip");
        string extractSourceDir = Path.Combine(_tempDirectory, "raw_theme");
        Directory.CreateDirectory(extractSourceDir);

        string cssDir = Path.Combine(extractSourceDir, "assets", "css");
        Directory.CreateDirectory(cssDir);
        await File.WriteAllTextAsync(Path.Combine(cssDir, "screen.css"), "body { background: #fff; }");

        string jsDir = Path.Combine(extractSourceDir, "assets", "js");
        Directory.CreateDirectory(jsDir);
        await File.WriteAllTextAsync(Path.Combine(jsDir, "app.js"), "console.log('theme loaded');");

        await File.WriteAllTextAsync(Path.Combine(extractSourceDir, "default.hbs"), "<html><head>{{ghost_head}}</head><body>{{@site.title}} {{ghost_foot}}</body></html>");

        ZipFile.CreateFromDirectory(extractSourceDir, themeZipPath);

        string targetTemplateDir = Path.Combine(_tempDirectory, "template", "ghostfx");
        await DocfxGenerator.ConvertGhostThemeToDocfxTemplateAsync(themeZipPath, targetTemplateDir, "<script>header</script>", "<script>footer</script>");

        // Verify modern template public/ directory structure
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "main.css")));
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "main.js")));

        // Verify NO partials directory or .partial files are generated for DocFx modern template
        Assert.False(Directory.Exists(Path.Combine(targetTemplateDir, "partials")));

        string mainCss = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "public", "main.css"));
        Assert.Contains("background: #fff", mainCss);

        string mainJs = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "public", "main.js"));
        Assert.Contains("Ghost Header Code Injection", mainJs);
        Assert.Contains("Ghost Footer Code Injection", mainJs);
    }

    [Fact]
    public async Task ConvertGhostThemeToDocfxTemplateAsync_WorksWithDirectorySource()
    {
        string themeDir = Path.Combine(_tempDirectory, "unzipped_theme");
        Directory.CreateDirectory(themeDir);
        await File.WriteAllTextAsync(Path.Combine(themeDir, "style.css"), "h1 { color: red; }");
        await File.WriteAllTextAsync(Path.Combine(themeDir, "custom.js"), "console.log('dir theme');");
        await File.WriteAllTextAsync(Path.Combine(themeDir, "index.hbs"), "<div>{{title}}</div>");

        string targetTemplateDir = Path.Combine(_tempDirectory, "template_dir", "ghostfx");
        await DocfxGenerator.ConvertGhostThemeToDocfxTemplateAsync(themeDir, targetTemplateDir);

        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "main.css")));
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "main.js")));

        string mainCss = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "public", "main.css"));
        Assert.Contains("color: red", mainCss);

        string mainJs = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "public", "main.js"));
        Assert.Contains("dir theme", mainJs);
    }

    [Fact]
    public async Task GenerateDocfxJsonIfNotExistsAsync_OmitLogoPath_WhenConfigured()
    {
        string subDir = Path.Combine(_tempDirectory, "nologo");
        Directory.CreateDirectory(subDir);

        var config = new GhostFxConfig
        {
            OutputDir = subDir,
            SiteTitle = "No Logo Site",
            LogoPath = false
        };

        string docfxPath = await DocfxGenerator.GenerateDocfxJsonIfNotExistsAsync(subDir, config);

        Assert.True(File.Exists(docfxPath));
        string jsonContent = await File.ReadAllTextAsync(docfxPath);

        Assert.Contains("No Logo Site", jsonContent);
        Assert.DoesNotContain("_appLogoPath", jsonContent);
    }
}
