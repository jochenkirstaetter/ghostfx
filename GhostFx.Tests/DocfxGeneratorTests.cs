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
        string jsonContent = await File.ReadAllTextAsync(docfxPath);
        Assert.Equal("{\n  \"existing\": true\n}", jsonContent);
    }

    [Fact]
    public void ConvertHandlebarsToDocfx_ConvertsGhostTagsToDocfxSyntax()
    {
        string hbsInput = """
        {{!< default}}
        <!DOCTYPE html>
        <html>
        <head>
            <title>{{title}} - {{@site.title}}</title>
            {{ghost_head}}
            {{!-- Theme comments should translate --}}
        </head>
        <body>
            {{#post}}
            <h1>{{title}}</h1>
            <div class="content">{{content}}</div>
            <img src="{{img_url feature_image size="xl"}}" />
            {{/post}}
            
            {{#foreach posts}}
                <h2>{{title}}</h2>
                {{#foreach tags}}
                    <span>{{name}}</span>
                {{/foreach}}
            {{/foreach}}
            
            {{ghost_foot}}
        </body>
        </html>
        """;

        string converted = DocfxGenerator.ConvertHandlebarsToDocfx(hbsInput);

        Assert.Contains("{{!master(layout/_master.tmpl)}}", converted);
        Assert.Contains("{{_appTitle}}", converted);
        Assert.Contains("{{>partials/ghost_head}}", converted);
        Assert.Contains("{{>partials/ghost_foot}}", converted);
        Assert.Contains("{{{conceptual}}}", converted);
        Assert.DoesNotContain("{{#post}}", converted);
        Assert.Contains("{{! Theme comments should translate }}", converted);
        Assert.Contains("{{#posts}}", converted);
        Assert.Contains("{{/posts}}", converted);
        Assert.Contains("{{#tags}}", converted);
        Assert.Contains("{{/tags}}", converted);
        Assert.Contains("{{featureImage}}", converted);
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

        string partialsDir = Path.Combine(extractSourceDir, "partials");
        Directory.CreateDirectory(partialsDir);
        await File.WriteAllTextAsync(Path.Combine(partialsDir, "site-nav.hbs"), "<nav>{{@site.title}}</nav>");

        ZipFile.CreateFromDirectory(extractSourceDir, themeZipPath);

        string targetTemplateDir = Path.Combine(_tempDirectory, "ghostfx");
        await DocfxGenerator.ConvertGhostThemeToDocfxTemplateAsync(themeZipPath, targetTemplateDir, "<script>header</script>", "<script>footer</script>");

        // Verify modern template public/ directory structure
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "main.css")));
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "main.js")));

        // Assets should be copied recursively as-is
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "css", "screen.css")));
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "js", "app.js")));

        // Verify layout/ master template is generated
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "layout", "_master.tmpl")));

        // Verify partials are generated under partials/ folder
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "partials", "site-nav.tmpl.partial")));

        string screenCss = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "public", "css", "screen.css"));
        Assert.Contains("background: #fff", screenCss);

        string mainJs = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "public", "main.js"));
        Assert.Contains("export default", mainJs);

        string masterContent = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "layout", "_master.tmpl"));
        Assert.Contains("{{>partials/code_header}}", masterContent);
        Assert.Contains("{{>partials/code_footer}}", masterContent);

        string codeHeader = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "partials", "code_header.tmpl.partial"));
        Assert.Contains("<script>header</script>", codeHeader);

        string codeFooter = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "partials", "code_footer.tmpl.partial"));
        Assert.Contains("<script>footer</script>", codeFooter);

        string partialContent = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "partials", "site-nav.tmpl.partial"));
        Assert.Contains("{{_appTitle}}", partialContent);
    }

    [Fact]
    public async Task ConvertGhostThemeToDocfxTemplateAsync_WorksWithDirectorySource()
    {
        string themeDir = Path.Combine(_tempDirectory, "unzipped_theme");
        Directory.CreateDirectory(themeDir);
        await File.WriteAllTextAsync(Path.Combine(themeDir, "style.css"), "h1 { color: red; }");
        await File.WriteAllTextAsync(Path.Combine(themeDir, "custom.js"), "console.log('dir theme');");
        await File.WriteAllTextAsync(Path.Combine(themeDir, "index.hbs"), "<div>{{title}}</div>");

        string targetTemplateDir = Path.Combine(_tempDirectory, "ghostfx_dir");
        await DocfxGenerator.ConvertGhostThemeToDocfxTemplateAsync(themeDir, targetTemplateDir);

        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "main.css")));
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "style.css")));
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "public", "custom.js")));
        Assert.True(File.Exists(Path.Combine(targetTemplateDir, "index.html.primary.tmpl")));

        string styleCss = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "public", "style.css"));
        Assert.Contains("color: red", styleCss);

        string mainJs = await File.ReadAllTextAsync(Path.Combine(targetTemplateDir, "public", "main.js"));
        Assert.Contains("export default", mainJs);
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

    [Fact]
    public async Task GenerateDocfxJsonIfNotExistsAsync_OmitsCustomTemplate_WhenMigrateThemeIsFalse()
    {
        string subDir = Path.Combine(_tempDirectory, "nomigrate");
        Directory.CreateDirectory(subDir);

        var config = new GhostFxConfig
        {
            OutputDir = subDir,
            SiteTitle = "No Migrate Site",
            MigrateTheme = false
        };

        string docfxPath = await DocfxGenerator.GenerateDocfxJsonIfNotExistsAsync(subDir, config, "ghostfx");

        Assert.True(File.Exists(docfxPath));
        string jsonContent = await File.ReadAllTextAsync(docfxPath);

        Assert.Contains("No Migrate Site", jsonContent);
        Assert.Contains("\"modern\"", jsonContent);
        
        var node = System.Text.Json.Nodes.JsonNode.Parse(jsonContent);
        var templates = node["build"]["template"].AsArray();
        bool hasGhostfx = false;
        foreach (var t in templates)
        {
            if (t.ToString() == "ghostfx") hasGhostfx = true;
        }
        Assert.False(hasGhostfx);
    }

    [Fact]
    public async Task EnsureDocfxTemplateOverridesExistAsync_ScaffoldsMasterTemplate()
    {
        string targetTemplateDir = Path.Combine(_tempDirectory, "scaffold_master");
        Directory.CreateDirectory(targetTemplateDir);

        await DocfxGenerator.EnsureDocfxTemplateOverridesExistAsync(targetTemplateDir, "ghostfx");

        string masterPath = Path.Combine(targetTemplateDir, "ghostfx", "layout", "_master.tmpl");
        Assert.True(File.Exists(masterPath));

        string content = await File.ReadAllTextAsync(masterPath);
        Assert.Contains("{{!GhostFx - Ghost to DocFx template conversion engine", content);
        Assert.Contains("{{#_googleAnalyticsTagId}}", content);
    }

    [Fact]
    public async Task ConvertGhostThemeToDocfxTemplateAsync_PreservesCustomMasterTemplate()
    {
        string themeDir = Path.Combine(_tempDirectory, "custom_theme_src");
        Directory.CreateDirectory(themeDir);
        await File.WriteAllTextAsync(Path.Combine(themeDir, "default.hbs"), "Theme Default Layout");

        string targetTemplateDir = Path.Combine(_tempDirectory, "custom_theme_dest");
        Directory.CreateDirectory(targetTemplateDir);

        // Pre-create a customized master layout
        string layoutDir = Path.Combine(targetTemplateDir, "layout");
        Directory.CreateDirectory(layoutDir);
        string masterPath = Path.Combine(layoutDir, "_master.tmpl");
        await File.WriteAllTextAsync(masterPath, "My Custom Master Template Layout Content");

        // Run conversion
        await DocfxGenerator.ConvertGhostThemeToDocfxTemplateAsync(themeDir, targetTemplateDir);

        // Check that _master.tmpl is preserved and NOT overwritten by the raw theme's default.hbs conversion
        Assert.True(File.Exists(masterPath));
        string content = await File.ReadAllTextAsync(masterPath);
        Assert.Equal("My Custom Master Template Layout Content", content);
    }

    [Fact]
    public async Task EnsureDocfxTemplateOverridesExistAsync_GeneratesCorrectIconLinks()
    {
        string targetTemplateDir = Path.Combine(_tempDirectory, "icon_links");
        Directory.CreateDirectory(targetTemplateDir);

        var iconLinks = new List<IconLink>
        {
            new IconLink { Icon = "github", Href = "https://github.com/jochenkirstaetter/ghostfx", Title = "GitHub" },
            new IconLink { Icon = "twitter", Href = "https://x.com/jkirstaetter", Title = "Twitter / X" },
            new IconLink { Icon = "facebook", Href = "https://facebook.com/jochen.kirstaetter", Title = "Facebook" }
        };

        await DocfxGenerator.EnsureDocfxTemplateOverridesExistAsync(targetTemplateDir, "ghostfx", iconLinks);

        string jsPath = Path.Combine(targetTemplateDir, "ghostfx", "public", "main.js");
        Assert.True(File.Exists(jsPath));

        string content = await File.ReadAllTextAsync(jsPath);
        Assert.Contains("https://x.com/jkirstaetter", content);
        Assert.Contains("https://facebook.com/jochen.kirstaetter", content);
        Assert.Contains("https://github.com/jochenkirstaetter/ghostfx", content);
    }

    [Fact]
    public async Task EnsureDocfxTemplateOverridesExistAsync_MergesExistingCustomLinks()
    {
        string targetTemplateDir = Path.Combine(_tempDirectory, "icon_links_merge");
        string publicDir = Path.Combine(targetTemplateDir, "ghostfx", "public");
        Directory.CreateDirectory(publicDir);

        string existingJs = """
        export default {
          iconLinks: [
            {
              "icon": "github",
              "href": "https://github.com/old/ghostfx",
              "title": "GitHub Old"
            },
            {
              "icon": "rss",
              "href": "https://jochen.kirstaetter.name/rss/",
              "title": "RSS Feed"
            }
          ]
        }
        """;
        string jsPath = Path.Combine(publicDir, "main.js");
        await File.WriteAllTextAsync(jsPath, existingJs, System.Text.Encoding.UTF8);

        var newLinks = new List<IconLink>
        {
            new IconLink { Icon = "github", Href = "https://github.com/new/ghostfx", Title = "GitHub New" },
            new IconLink { Icon = "twitter", Href = "https://x.com/newhandle", Title = "Twitter" }
        };

        await DocfxGenerator.EnsureDocfxTemplateOverridesExistAsync(targetTemplateDir, "ghostfx", newLinks);

        string content = await File.ReadAllTextAsync(jsPath);
        Assert.Contains("https://github.com/new/ghostfx", content);
        Assert.Contains("https://x.com/newhandle", content);
        Assert.DoesNotContain("https://github.com/old/ghostfx", content);
        Assert.Contains("https://jochen.kirstaetter.name/rss/", content);
    }

    [Fact]
    public void ParseTocYml_ParsesYamlItemsCorrectly()
    {
        string tocYaml = """
        - name: About
          uid: about
        - name: Blog
          uid: blog
        - name: Community
          uid: community
        - name: Speaking
          uid: speaking
        """;
        File.WriteAllText(Path.Combine(_tempDirectory, "toc.yml"), tocYaml);

        var items = DocfxGenerator.ParseTocYml(_tempDirectory);

        Assert.Equal(4, items.Count);
        Assert.Equal("About", items[0].Label);
        Assert.Equal("about.html", items[0].Url);
        Assert.Equal("Blog", items[1].Label);
        Assert.Equal("blog.html", items[1].Url);
        Assert.Equal("Community", items[2].Label);
        Assert.Equal("community.html", items[2].Url);
        Assert.Equal("Speaking", items[3].Label);
        Assert.Equal("speaking.html", items[3].Url);
    }

    [Fact]
    public async Task EnsureDocfxTemplateOverridesExistAsync_PopulatesSiteNavFromTocYmlWhenNavItemsEmpty()
    {
        string tocYaml = """
        - name: About
          uid: about
        - name: Blog
          uid: blog
        - name: Community
          uid: community
        - name: Speaking
          uid: speaking
        """;
        File.WriteAllText(Path.Combine(_tempDirectory, "toc.yml"), tocYaml);

        await DocfxGenerator.EnsureDocfxTemplateOverridesExistAsync(_tempDirectory, "ghostfx");

        string navPartialPath = Path.Combine(_tempDirectory, "ghostfx", "partials", "site-nav.tmpl.partial");
        Assert.True(File.Exists(navPartialPath));

        string navContent = await File.ReadAllTextAsync(navPartialPath);
        Assert.Contains("<li class=\"nav-about\" role=\"menuitem\"><a href=\"{{_rel}}about.html\">About</a></li>", navContent);
        Assert.Contains("<li class=\"nav-blog\" role=\"menuitem\"><a href=\"{{_rel}}blog.html\">Blog</a></li>", navContent);
        Assert.Contains("<li class=\"nav-community\" role=\"menuitem\"><a href=\"{{_rel}}community.html\">Community</a></li>", navContent);
        Assert.Contains("<li class=\"nav-speaking\" role=\"menuitem\"><a href=\"{{_rel}}speaking.html\">Speaking</a></li>", navContent);
    }
}
