using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GhostFx.Core;

public static class DocfxGenerator
{
    private const string DefaultMasterTemplate = """
{{!GhostFx - Ghost to DocFx template conversion engine. This content has been auto-generated and changes might be over-written.}}
{{!include(/^public/.*/)}}
{{!include(favicon.ico)}}
{{!include(logo.svg)}}
<!DOCTYPE html>
<html {{#_lang}}lang="{{_lang}}"{{/_lang}}>
  <head>
    <meta charset="utf-8">
    {{#redirect_url}}
      <meta http-equiv="refresh" content="0;URL='{{redirect_url}}'">
    {{/redirect_url}}
    {{^redirect_url}}
      <title>{{#title}}{{title}}{{/title}}{{^title}}{{>partials/title}}{{/title}} {{#_appTitle}}| {{_appTitle}} {{/_appTitle}}</title>
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
      <meta name="title" content="{{#title}}{{title}}{{/title}}{{^title}}{{>partials/title}}{{/title}} {{#_appTitle}}| {{_appTitle}} {{/_appTitle}}">
      {{#_description}}<meta name="description" content="{{_description}}">{{/_description}}
      {{#description}}<meta name="description" content="{{description}}">{{/description}}
      <link rel="icon" href="{{_rel}}{{{_appFaviconPath}}}{{^_appFaviconPath}}favicon.ico{{/_appFaviconPath}}">
      <link rel="stylesheet" href="{{_rel}}public/docfx.min.css">
      <meta name="docfx:navrel" content="{{_navRel}}">
      <meta name="docfx:tocrel" content="{{_tocRel}}">
      {{#_noindex}}<meta name="searchOption" content="noindex">{{/_noindex}}
      {{#_enableSearch}}<meta name="docfx:rel" content="{{_rel}}">{{/_enableSearch}}
      {{#_disableNewTab}}<meta name="docfx:disablenewtab" content="true">{{/_disableNewTab}}
      {{#_disableTocFilter}}<meta name="docfx:disabletocfilter" content="true">{{/_disableTocFilter}}
      {{#docurl}}<meta name="docfx:docurl" content="{{docurl}}">{{/docurl}}
      <meta name="loc:inThisArticle" content="{{__global.inThisArticle}}">
      <meta name="loc:searchResultsCount" content="{{__global.searchResultsCount}}">
      <meta name="loc:searchNoResults" content="{{__global.searchNoResults}}">
      <meta name="loc:tocFilter" content="{{__global.tocFilter}}">
      <meta name="loc:nextArticle" content="{{__global.nextArticle}}">
      <meta name="loc:prevArticle" content="{{__global.prevArticle}}">
      <meta name="loc:themeLight" content="{{__global.themeLight}}">
      <meta name="loc:themeDark" content="{{__global.themeDark}}">
      <meta name="loc:themeAuto" content="{{__global.themeAuto}}">
      <meta name="loc:changeTheme" content="{{__global.changeTheme}}">
      <meta name="loc:copy" content="{{__global.copy}}">
      <meta name="loc:downloadPdf" content="{{__global.downloadPdf}}">

      <script type="module" src="./{{_rel}}public/docfx.min.js"></script>

      <script>
        const theme = localStorage.getItem('theme') || 'auto'
        document.documentElement.setAttribute('data-bs-theme', theme === 'auto' ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : theme)
      </script>

      {{#_googleAnalyticsTagId}}
      <script async src="https://www.googletagmanager.com/gtag/js?id={{_googleAnalyticsTagId}}"></script>
      <script>
        window.dataLayer = window.dataLayer || [];
        function gtag() { dataLayer.push(arguments); }
        gtag('js', new Date());
        gtag('config', '{{_googleAnalyticsTagId}}');
      </script>
      {{/_googleAnalyticsTagId}}
    {{/redirect_url}}
    {{>partials/ghost_head}}
    {{>partials/code_header}}
    <link rel="stylesheet" href="{{_rel}}public/main.css">
  </head>

  {{^redirect_url}}
  <body class="tex2jax_ignore" data-layout="{{_layout}}{{layout}}" data-yaml-mime="{{yamlmime}}">
    <header class="bg-body border-bottom">
      {{^_disableNavbar}}
      <nav id="autocollapse" class="navbar navbar-expand-md" role="navigation">
        <div class="container-xxl flex-nowrap">
          <a class="navbar-brand" href="{{_appLogoUrl}}{{^_appLogoUrl}}{{_rel}}index.html{{/_appLogoUrl}}">
            {{#_appLogoPath}}
              <img id="logo" class="svg" src="{{_rel}}{{{_appLogoPath}}}{{^_appLogoPath}}logo.svg{{/_appLogoPath}}" alt="{{_appName}}" >
            {{/_appLogoPath}}
            {{_appName}}
          </a>
          <button class="btn btn-lg d-md-none border-0" type="button" data-bs-toggle="collapse" data-bs-target="#navpanel" aria-controls="navpanel" aria-expanded="false" aria-label="Toggle navigation">
            <i class="bi bi-three-dots"></i>
          </button>
          <div class="collapse navbar-collapse" id="navpanel">
            <div id="navbar">
              {{#_enableSearch}}
              <form class="search" role="search" id="search">
                <i class="bi bi-search"></i>
                <input class="form-control" id="search-query" type="search" disabled placeholder="{{__global.search}}" autocomplete="off" aria-label="Search">
              </form>
              {{/_enableSearch}}
            </div>
          </div>
        </div>
      </nav>
      {{/_disableNavbar}}
    </header>

    <main class="container-xxl">
      {{^_disableToc}}
      <div class="toc-offcanvas">
        <div class="offcanvas-md offcanvas-start" tabindex="-1" id="tocOffcanvas" aria-labelledby="tocOffcanvasLabel">
          <div class="offcanvas-header">
            <h5 class="offcanvas-title" id="tocOffcanvasLabel">Table of Contents</h5>
            <button type="button" class="btn-close" data-bs-dismiss="offcanvas" data-bs-target="#tocOffcanvas" aria-label="Close"></button>
          </div>
          <div class="offcanvas-body">
            <nav class="toc" id="toc"></nav>
          </div>
        </div>
      </div>
      {{/_disableToc}}

      <div class="content">
        <div class="actionbar">
          {{^_disableToc}}
          <button class="btn btn-lg border-0 d-md-none"
              type="button" data-bs-toggle="offcanvas" data-bs-target="#tocOffcanvas"
              aria-controls="tocOffcanvas" aria-expanded="false" aria-label="Show table of contents">
            <i class="bi bi-list"></i>
          </button>
          {{/_disableToc}}

          {{^_disableBreadcrumb}}
          <nav id="breadcrumb"></nav>
          {{/_disableBreadcrumb}}
        </div>

        <article data-uid="{{uid}}">
          {{!body}}
        </article>

        {{^_disableContribution}}
        <div class="contribution d-print-none">
          {{#sourceurl}}
          <a href="{{sourceurl}}" class="edit-link">{{__global.improveThisDoc}}</a>
          {{/sourceurl}}
          {{^sourceurl}}{{#docurl}}
          <a href="{{docurl}}" class="edit-link">{{__global.improveThisDoc}}</a>
          {{/docurl}}{{/sourceurl}}
        </div>
        {{/_disableContribution}}

        {{^_disableNextArticle}}
        <div class="next-article d-print-none border-top" id="nextArticle"></div>
        {{/_disableNextArticle}}

      </div>

      {{^_disableAffix}}
      <div class="affix">
        <nav id="affix"></nav>
      </div>
      {{/_disableAffix}}
    </main>

    {{#_enableSearch}}
    <div class="container-xxl search-results" id="search-results"></div>
    {{/_enableSearch}}

    <footer class="border-top text-secondary">
      <div class="container-xxl">
        <div class="flex-fill">
          {{{_appFooter}}}{{^_appFooter}}<span>Made with <a href="https://dotnet.github.io/docfx">docfx</a></span>{{/_appFooter}}
        </div>
      </div>
    </footer>
    {{>partials/ghost_foot}}
    {{>partials/code_footer}}
    
  </body>
  {{/redirect_url}}
</html>
""";

    public static async Task<string> GenerateDocfxJsonIfNotExistsAsync(
        string rootDir,
        GhostFxConfig config,
        string? customTemplatePath = null,
        string siteLocale = "en",
        List<IconLink>? iconLinks = null,
        string? headerCodeInjection = null,
        string? footerCodeInjection = null,
        string? siteTwitter = null,
        string? siteFacebook = null)
    {
        string templatePath = customTemplatePath ?? "ghostfx";
        var links = await EnsureDocfxTemplateOverridesExistAsync(rootDir, templatePath, iconLinks);

        string partialsDir = Path.Combine(rootDir, templatePath, "partials");
        Directory.CreateDirectory(partialsDir);
        await File.WriteAllTextAsync(Path.Combine(partialsDir, "code_header.tmpl.partial"), headerCodeInjection ?? "", System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(partialsDir, "code_footer.tmpl.partial"), footerCodeInjection ?? "", System.Text.Encoding.UTF8);

        string fullOutputDir = Path.GetFullPath(config.OutputDir);
        string fullRootDir = Path.GetFullPath(rootDir);

        string relOutputDir = Path.GetRelativePath(fullRootDir, fullOutputDir).Replace('\\', '/');
        if (relOutputDir == ".") relOutputDir = "";

        string articlesPattern = string.IsNullOrEmpty(relOutputDir) ? "**.md" : $"{relOutputDir}/**.md";
        string tocPattern = string.IsNullOrEmpty(relOutputDir) ? "**/toc.yml" : $"{relOutputDir}/**/toc.yml";
        string mediaPattern = string.IsNullOrEmpty(relOutputDir) ? "**/content/images/**" : $"{relOutputDir}/**/content/images/**";

        string? faviconPath = null;
        if (File.Exists(Path.Combine(fullRootDir, "favicon.png"))) faviconPath = "favicon.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "favicon.png"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "content/images/favicon.png" : $"{relOutputDir}/content/images/favicon.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.png"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.png" : $"{relOutputDir}/favicon.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "favicon.svg"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "content/images/favicon.svg" : $"{relOutputDir}/content/images/favicon.svg";
        else if (File.Exists(Path.Combine(fullRootDir, "favicon.svg"))) faviconPath = "favicon.svg";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.svg"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.svg" : $"{relOutputDir}/favicon.svg";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "favicon.ico"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "content/images/favicon.ico" : $"{relOutputDir}/content/images/favicon.ico";
        else if (File.Exists(Path.Combine(fullRootDir, "favicon.ico"))) faviconPath = "favicon.ico";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.ico"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.ico" : $"{relOutputDir}/favicon.ico";

        string? logoPath = null;
        if (File.Exists(Path.Combine(fullRootDir, "logo.png"))) logoPath = "logo.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "logo.png"))) logoPath = string.IsNullOrEmpty(relOutputDir) ? "content/images/logo.png" : $"{relOutputDir}/content/images/logo.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "logo.png"))) logoPath = string.IsNullOrEmpty(relOutputDir) ? "logo.png" : $"{relOutputDir}/logo.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "logo.svg"))) logoPath = string.IsNullOrEmpty(relOutputDir) ? "content/images/logo.svg" : $"{relOutputDir}/content/images/logo.svg";
        else if (File.Exists(Path.Combine(fullRootDir, "logo.svg"))) logoPath = "logo.svg";
        else if (File.Exists(Path.Combine(fullOutputDir, "logo.svg"))) logoPath = string.IsNullOrEmpty(relOutputDir) ? "logo.svg" : $"{relOutputDir}/logo.svg";
        else if (!string.IsNullOrEmpty(faviconPath)) logoPath = faviconPath;

        bool useCustomTemplate = config.MigrateTheme;
        string templateExclude = $"{Path.GetFileName(templatePath)}/**";

        string docfxPath = Path.Combine(rootDir, "docfx.json");
        if (File.Exists(docfxPath))
        {
            try
            {
                string existingJson = await File.ReadAllTextAsync(docfxPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(existingJson);
                if (node != null)
                {
                    var buildNode = node["build"];
                    if (buildNode != null)
                    {
                        // 1. Ensure template list includes custom template name
                        var templateArray = buildNode["template"] as System.Text.Json.Nodes.JsonArray;
                        if (templateArray == null)
                        {
                            buildNode["template"] = new System.Text.Json.Nodes.JsonArray("default", "modern");
                            templateArray = buildNode["template"] as System.Text.Json.Nodes.JsonArray;
                        }
                        if (templateArray != null)
                        {
                            string targetTemplateName = Path.GetFileName(templatePath);
                            if (!useCustomTemplate)
                            {
                                for (int i = templateArray.Count - 1; i >= 0; i--)
                                {
                                    if (templateArray[i]?.ToString() == targetTemplateName)
                                    {
                                        templateArray.RemoveAt(i);
                                    }
                                }
                            }
                            else
                            {
                                bool hasTemplate = false;
                                foreach (var item in templateArray)
                                {
                                    if (item?.ToString() == targetTemplateName)
                                    {
                                        hasTemplate = true;
                                        break;
                                    }
                                }
                                if (!hasTemplate)
                                {
                                    templateArray.Add(targetTemplateName);
                                }
                            }
                        }

                        // 2. Ensure content and resource exclusions include the custom template folder
                        
                        var contentNode = buildNode["content"] as System.Text.Json.Nodes.JsonArray;
                        if (contentNode != null)
                        {
                            foreach (var entry in contentNode)
                            {
                                if (entry?["exclude"] != null)
                                {
                                    var excludeArray = entry["exclude"] as System.Text.Json.Nodes.JsonArray;
                                    if (excludeArray != null)
                                    {
                                        bool hasExclude = false;
                                        foreach (var item in excludeArray)
                                        {
                                            if (item?.ToString() == templateExclude)
                                            {
                                                hasExclude = true;
                                                break;
                                            }
                                        }
                                        if (!hasExclude)
                                        {
                                            excludeArray.Add(templateExclude);
                                        }
                                    }
                                }
                            }
                        }

                        var resourceNode = buildNode["resource"] as System.Text.Json.Nodes.JsonArray;
                        if (resourceNode != null)
                        {
                            foreach (var entry in resourceNode)
                            {
                                if (entry?["exclude"] != null)
                                {
                                    var excludeArray = entry["exclude"] as System.Text.Json.Nodes.JsonArray;
                                    if (excludeArray != null)
                                    {
                                        bool hasExclude = false;
                                        foreach (var item in excludeArray)
                                        {
                                            if (item?.ToString() == templateExclude)
                                            {
                                                hasExclude = true;
                                                break;
                                            }
                                        }
                                        if (!hasExclude)
                                        {
                                            excludeArray.Add(templateExclude);
                                        }
                                    }
                                }
                            }
                        }

                        // 3. Update globalMetadata
                        var globalMetadataNode = buildNode["globalMetadata"];
                        if (globalMetadataNode == null)
                        {
                            buildNode["globalMetadata"] = new System.Text.Json.Nodes.JsonObject();
                            globalMetadataNode = buildNode["globalMetadata"];
                        }
                        
                        if (globalMetadataNode != null)
                        {
                            globalMetadataNode["_currentYear"] = DateTime.UtcNow.Year;
                            if (!string.IsNullOrWhiteSpace(config.GhostUrl))
                            {
                                globalMetadataNode["_appUrl"] = config.GhostUrl.TrimEnd('/');
                            }
                            if (!string.IsNullOrWhiteSpace(siteTwitter))
                            {
                                globalMetadataNode["_siteTwitter"] = siteTwitter.StartsWith("@") ? siteTwitter : "@" + siteTwitter;
                            }
                            if (!string.IsNullOrWhiteSpace(siteFacebook))
                            {
                                globalMetadataNode["_siteFacebook"] = siteFacebook.StartsWith("http") ? siteFacebook : "https://facebook.com/" + siteFacebook;
                            }

                            globalMetadataNode["_disableContribution"] = true;
                            globalMetadataNode["_disableBreadcrumb"] = true;
                            globalMetadataNode["_lang"] = string.IsNullOrWhiteSpace(siteLocale) ? "en" : siteLocale;
                            globalMetadataNode["_enableSearch"] = true;
                            globalMetadataNode["_appTitle"] = config.SiteTitle;
                            globalMetadataNode["_appName"] = config.SiteTitle;
                            globalMetadataNode["_appFaviconPath"] = faviconPath ?? "favicon.png";
                            if (config.LogoPath)
                            {
                                globalMetadataNode["_appLogoPath"] = logoPath ?? faviconPath ?? "favicon.png";
                            }
                            else
                            {
                                globalMetadataNode.AsObject().Remove("_appLogoPath");
                            }

                            globalMetadataNode.AsObject().Remove("_siteNavigation");
                            globalMetadataNode.AsObject().Remove("_siteSocialLinks");
                        }
                    }
                    
                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    await File.WriteAllTextAsync(docfxPath, node.ToJsonString(jsonOptions), System.Text.Encoding.UTF8);
                }
            }
            catch { }
            return docfxPath;
        }


        var contentEntries = new List<object>
        {
            new
            {
                files = new string[] { "**.md" },
                src = "published",
                dest = ""
            },
            new
            {
                files = new string[] { "**.md" },
                src = "pages",
                dest = ""
            },
            new
            {
                files = new string[] { "**.md", "**/toc.yml" },
                exclude = new string[] { "draft/**", "published/**", "pages/**", templateExclude, "_site/**", "**/_site/**" }
            }
        };

        var globalMetadata = new Dictionary<string, object>
        {
            ["_appTitle"] = config.SiteTitle,
            ["_appName"] = config.SiteTitle,
            ["_appFaviconPath"] = faviconPath ?? "favicon.ico",
            ["_enableSearch"] = true,
            ["_disableContribution"] = true,
            ["_disableBreadcrumb"] = true,
            ["_lang"] = string.IsNullOrWhiteSpace(siteLocale) ? "en" : siteLocale,
            ["_appFooter"] = $"<span>Generated by <a href='https://github.com/jochenkirstaetter/ghostfx'>GhostFx</a> for DocFx</span>"
        };

        if (config.LogoPath)
        {
            globalMetadata["_appLogoPath"] = logoPath ?? faviconPath ?? "favicon.ico";
        }

        if (!string.IsNullOrWhiteSpace(config.GoogleAnalyticsTag))
        {
            globalMetadata["_googleAnalyticsTagId"] = config.GoogleAnalyticsTag;
        }

        if (!string.IsNullOrWhiteSpace(config.GhostUrl))
        {
            globalMetadata["_appUrl"] = config.GhostUrl.TrimEnd('/');
        }

        if (!string.IsNullOrWhiteSpace(siteTwitter))
        {
            globalMetadata["_siteTwitter"] = siteTwitter.StartsWith("@") ? siteTwitter : "@" + siteTwitter;
        }

        if (!string.IsNullOrWhiteSpace(siteFacebook))
        {
            globalMetadata["_siteFacebook"] = siteFacebook.StartsWith("http") ? siteFacebook : "https://facebook.com/" + siteFacebook;
        }

        var docfxConfig = new
        {
            build = new
            {
                content = contentEntries.ToArray(),
                resource = new object[]
                {
                    new
                    {
                        files = new string[]
                        {
                            "**/content/images/**",
                            "*.png",
                            "*.jpg",
                            "*.jpeg",
                            "*.svg",
                            "*.ico"
                        },
                        exclude = new string[]
                        {
                            templateExclude,
                            "_site/**",
                            "**/_site/**"
                        }
                    }
                },
                output = "_site",
                template = useCustomTemplate
                    ? new string[]
                    {
                        "default",
                        "modern",
                        Path.GetFileName(templatePath)
                    }
                    : new string[]
                    {
                        "default",
                        "modern"
                    },
                globalMetadata
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(docfxConfig, options);
        var dir = Path.GetDirectoryName(docfxPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(docfxPath, json);
        return docfxPath;
    }

    public static async Task ConvertGhostThemeToDocfxTemplateAsync(
        string themePath, 
        string targetTemplateDir, 
        string headerInjection = "", 
        string footerInjection = "",
        Func<string, Task<bool>>? onConfirmTemplatePurge = null,
        List<IconLink>? iconLinks = null,
        List<GhostNavItem>? navItems = null,
        List<BlogPostMetadata>? pages = null,
        List<BlogPostMetadata>? posts = null)
    {
        if (string.IsNullOrWhiteSpace(themePath))
            return;

        bool isZip = File.Exists(themePath) && (themePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || !Directory.Exists(themePath));
        bool isDirectory = Directory.Exists(themePath);

        if (!isZip && !isDirectory)
            return;

        // If template directory exists, confirm purge
        if (Directory.Exists(targetTemplateDir))
        {
            if (onConfirmTemplatePurge != null)
            {
                bool confirmed = await onConfirmTemplatePurge(targetTemplateDir);
                if (!confirmed)
                {
                    return; // Skip theme migration/purge
                }
            }

            PurgeDirectory(targetTemplateDir);
        }

        Directory.CreateDirectory(targetTemplateDir);

        // DocFx modern template uses public/ directory for layout overrides and styling
        string publicDir = Path.Combine(targetTemplateDir, "public");
        Directory.CreateDirectory(publicDir);

        await ConvertGhostThemeFolderAsync(themePath, targetTemplateDir, headerInjection, footerInjection, iconLinks, navItems, pages, posts);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            string targetFile = Path.Combine(target, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }
        foreach (var subDir in Directory.GetDirectories(source))
        {
            string targetSubDir = Path.Combine(target, Path.GetFileName(subDir));
            CopyDirectory(subDir, targetSubDir);
        }
    }

    private static async Task ConvertGhostThemeFolderAsync(string themePath, string targetTemplateDir, string headerInjection = "", string footerInjection = "", List<IconLink>? iconLinks = null, List<GhostNavItem>? navItems = null, List<BlogPostMetadata>? pages = null, List<BlogPostMetadata>? posts = null)
    {
        string publicDir = Path.Combine(targetTemplateDir, "public");
        Directory.CreateDirectory(publicDir);

        bool isZip = File.Exists(themePath) && (themePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || !Directory.Exists(themePath));
        string sourceDir;
        string? tempExtractPath = null;

        if (isZip)
        {
            tempExtractPath = Path.Combine(Path.GetTempPath(), "GhostFx_Theme_" + Guid.NewGuid().ToString("N"));
            ZipFile.ExtractToDirectory(themePath, tempExtractPath, overwriteFiles: true);
            sourceDir = tempExtractPath;
        }
        else
        {
            sourceDir = themePath;
        }

        try
        {
            // 1. Recursive copy of the assets directory if it exists
            string assetsSourceDir = Path.Combine(sourceDir, "assets");
            if (Directory.Exists(assetsSourceDir))
            {
                CopyDirectory(assetsSourceDir, publicDir);
            }

            // Also copy any individual asset files located in the root of sourceDir
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".css" || ext == ".js" || ext == ".png" || ext == ".jpg" || ext == ".svg" || ext == ".ico")
                {
                    try
                     {
                        File.Copy(file, Path.Combine(publicDir, Path.GetFileName(file)), true);
                    }
                    catch { }
                }
            }

            // 2. Copy favicon.ico additionally to the output root folder if it exists
            string rootDir = Path.GetDirectoryName(targetTemplateDir) ?? targetTemplateDir;
            string faviconSourcePath = Path.Combine(sourceDir, "favicon.ico");
            if (!File.Exists(faviconSourcePath))
            {
                faviconSourcePath = Path.Combine(sourceDir, "assets", "favicon.ico");
            }

            if (File.Exists(faviconSourcePath))
            {
                try
                {
                    File.Copy(faviconSourcePath, Path.Combine(rootDir, "favicon.ico"), true);
                    File.Copy(faviconSourcePath, Path.Combine(publicDir, "favicon.ico"), true);
                }
                catch { }
            }

            // 3. Ensure public/main.css exists
            string mainCssPath = Path.Combine(publicDir, "main.css");
            if (!File.Exists(mainCssPath))
            {
                await File.WriteAllTextAsync(mainCssPath, "");
            }

            // 4. Ensure public/main.js exists & strictly adheres to specifications
            await EnsureDocfxTemplateOverridesExistAsync(rootDir, Path.GetFileName(targetTemplateDir), iconLinks, navItems, pages, posts);

            // Overwrite ghost_head.tmpl and ghost_foot.tmpl
            string destPartialsDir = Path.Combine(targetTemplateDir, "partials");
            Directory.CreateDirectory(destPartialsDir);

            string ghostHeadContent = "{{>partials/meta}}\n{{>partials/opengraph}}\n{{>partials/twitter}}\n{{>partials/schema}}\n" +
                                      "{{#codeinjectionHead}}{{{codeinjectionHead}}}{{/codeinjectionHead}}\n";
            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "ghost_head.tmpl.partial"), ghostHeadContent, System.Text.Encoding.UTF8);

            string ghostFootContent = "{{#codeinjectionFoot}}{{{codeinjectionFoot}}}{{/codeinjectionFoot}}\n";
            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "ghost_foot.tmpl.partial"), ghostFootContent, System.Text.Encoding.UTF8);

            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "code_header.tmpl.partial"), headerInjection ?? "", System.Text.Encoding.UTF8);
            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "code_footer.tmpl.partial"), footerInjection ?? "", System.Text.Encoding.UTF8);

            string siteNavContent = GenerateSiteNavPartialContent(navItems, pages, posts, iconLinks);
            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "site-nav.tmpl.partial"), siteNavContent, System.Text.Encoding.UTF8);

            // 5. Process Handlebars (.hbs) template files into converted DocFX Mustache templates
            foreach (var hbsFile in Directory.GetFiles(sourceDir, "*.hbs", SearchOption.AllDirectories))
            {
                string hbsContent = await File.ReadAllTextAsync(hbsFile);
                string hbsName = Path.GetFileNameWithoutExtension(hbsFile);
                string hbsNameLower = hbsName.ToLowerInvariant();

                if (hbsNameLower == "site-nav")
                {
                    continue;
                }

                bool isPartial = hbsFile.Contains("/partials/", StringComparison.OrdinalIgnoreCase) || 
                                 hbsFile.Contains("\\partials\\", StringComparison.OrdinalIgnoreCase) ||
                                 hbsFile.Contains("/partials\\", StringComparison.OrdinalIgnoreCase) ||
                                 hbsFile.Contains("\\partials/", StringComparison.OrdinalIgnoreCase) ||
                                 Path.GetDirectoryName(hbsFile)?.EndsWith("partials", StringComparison.OrdinalIgnoreCase) == true;

                bool isLayout = hbsNameLower == "default";
                string converted = ConvertHandlebarsToDocfx(hbsContent, isLayout, headerInjection, footerInjection, iconLinks);

                string targetPath;

                if (isPartial)
                {
                    int partialsIdx = hbsFile.IndexOf("partials", StringComparison.OrdinalIgnoreCase);
                    string relativePart = hbsFile.Substring(partialsIdx + 8).TrimStart('/', '\\');

                    if (IsDevelopmentOrTestFile(relativePart))
                    {
                        continue;
                    }

                    string relName = Path.ChangeExtension(relativePart, ".tmpl.partial");
                    targetPath = Path.Combine(targetTemplateDir, "partials", relName);
                }
                else
                {
                    if (isLayout)
                    {
                        targetPath = Path.Combine(targetTemplateDir, "layout", "_master.tmpl");
                    }
                    else if (hbsNameLower == "post")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "post.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "page")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "page.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "tag")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "tag.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "author")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "author.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "index")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "index.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "error-404")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "error-404.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "error")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "error.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "archive" || hbsNameLower == "search" || hbsNameLower == "private" || hbsNameLower == "subscribe")
                    {
                        targetPath = Path.Combine(targetTemplateDir, $"{hbsNameLower}.html.primary.tmpl");
                    }
                    else if (hbsNameLower.StartsWith("custom-"))
                    {
                        targetPath = Path.Combine(targetTemplateDir, $"{hbsNameLower}.html.primary.tmpl");
                    }
                    else
                    {
                        // Skip any development leftovers or unsupported templates in the root folder
                        continue;
                    }
                }

                string? parentDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                await File.WriteAllTextAsync(targetPath, converted, System.Text.Encoding.UTF8);

                // For primary templates (excluding layout and partials), also generate a layout partial copy
                if (!isPartial && !isLayout)
                {
                    string partialLayoutDir = Path.Combine(targetTemplateDir, "partials");
                    Directory.CreateDirectory(partialLayoutDir);
                    string partialLayoutPath = Path.Combine(partialLayoutDir, $"{hbsNameLower}_layout.tmpl.partial");
                    // Strip the master inheritance directive to prevent layout nesting issues inside partials
                    string partialContent = Regex.Replace(converted, @"^\{\{!master\([^)]+\)\}\}\s*\r?\n?", "", RegexOptions.IgnoreCase);
                    await File.WriteAllTextAsync(partialLayoutPath, partialContent);
                }
            }

            // Write conceptual.html.primary.tmpl layout router
            string conceptualPath = Path.Combine(targetTemplateDir, "conceptual.html.primary.tmpl");
            string conceptualContent = @"{{!master(layout/_master.tmpl)}}
{{#isPost}}
{{>partials/post_layout}}
{{/isPost}}
{{#isPage}}
{{>partials/page_layout}}
{{/isPage}}
{{#isTagPage}}
{{>partials/tag_layout}}
{{/isTagPage}}
{{#isTagsIndexPage}}
{{>partials/tag_layout}}
{{/isTagsIndexPage}}
{{#isAuthorPage}}
{{>partials/author_layout}}
{{/isAuthorPage}}
{{#isHome}}
{{>partials/index_layout}}
{{/isHome}}
{{^isPost}}
{{^isPage}}
{{^isTagPage}}
{{^isTagsIndexPage}}
{{^isAuthorPage}}
{{^isHome}}
{{{conceptual}}}
{{/isHome}}
{{/isAuthorPage}}
{{/isTagsIndexPage}}
{{/isTagPage}}
{{/isPage}}
{{/isPost}}";
            await File.WriteAllTextAsync(conceptualPath, conceptualContent);

            // Write metadata partial templates in partials/
            string pDir = Path.Combine(targetTemplateDir, "partials");
            Directory.CreateDirectory(pDir);

            string metaContent = @"{{#_appFaviconPath}}
<link rel=""icon"" href=""{{_rel}}{{_appFaviconPath}}"" type=""image/png"">
{{/_appFaviconPath}}
{{^_appFaviconPath}}
<link rel=""icon"" href=""{{_rel}}favicon.png"" type=""image/png"">
{{/_appFaviconPath}}
{{#canonicalUrl}}
<link rel=""canonical"" href=""{{canonicalUrl}}"">
{{/canonicalUrl}}
<meta name=""referrer"" content=""no-referrer-when-downgrade"">
{{#metaTitle}}
<meta name=""title"" content=""{{metaTitle}}"">
{{/metaTitle}}
{{^metaTitle}}
{{#title}}
<meta name=""title"" content=""{{title}}"">
{{/title}}
{{/metaTitle}}
{{#metaDescription}}
<meta name=""description"" content=""{{metaDescription}}"">
{{/metaDescription}}
{{^metaDescription}}
{{#description}}
<meta name=""description"" content=""{{description}}"">
{{/description}}
{{^description}}
{{#summary}}
<meta name=""description"" content=""{{summary}}"">
{{/summary}}
{{/description}}
{{/metaDescription}}";

            string opengraphContent = @"<meta property=""og:site_name"" content=""{{#_appTitle}}{{_appTitle}}{{/_appTitle}}{{^_appTitle}}Get Blogged by JoKi{{/_appTitle}}"">
<meta property=""og:type"" content=""{{#isPost}}article{{/isPost}}{{^isPost}}website{{/isPost}}"">
{{#ogTitle}}
<meta property=""og:title"" content=""{{ogTitle}}"">
{{/ogTitle}}
{{^ogTitle}}
{{#title}}
<meta property=""og:title"" content=""{{title}}"">
{{/title}}
{{/ogTitle}}
{{#ogDescription}}
<meta property=""og:description"" content=""{{ogDescription}}"">
{{/ogDescription}}
{{^ogDescription}}
{{#description}}
<meta property=""og:description"" content=""{{description}}"">
{{/description}}
{{^description}}
{{#summary}}
<meta property=""og:description"" content=""{{summary}}"">
{{/summary}}
{{/description}}
{{/ogDescription}}
{{#canonicalUrl}}
<meta property=""og:url"" content=""{{canonicalUrl}}"">
{{/canonicalUrl}}
{{#imageUrl}}
<meta property=""og:image"" content=""{{imageUrl}}"">
<meta property=""og:image:width"" content=""1920"">
<meta property=""og:image:height"" content=""1277"">
{{/imageUrl}}
{{#publishedAt}}
<meta property=""article:published_time"" content=""{{publishedAt}}"">
{{/publishedAt}}
{{#updatedAt}}
<meta property=""article:modified_time"" content=""{{updatedAt}}"">
{{/updatedAt}}
{{^updatedAt}}
{{#publishedAt}}
<meta property=""article:modified_time"" content=""{{publishedAt}}"">
{{/publishedAt}}
{{/updatedAt}}
{{#tags}}
<meta property=""article:tag"" content=""{{.}}"">
{{/tags}}
{{#_siteFacebook}}
<meta property=""article:publisher"" content=""{{_siteFacebook}}"">
{{/_siteFacebook}}
{{#authorFacebook}}
<meta property=""article:author"" content=""{{authorFacebook}}"">
{{/authorFacebook}}";

            string twitterContent = @"<meta name=""twitter:card"" content=""summary_large_image"">
{{#twitterTitle}}
<meta name=""twitter:title"" content=""{{twitterTitle}}"">
{{/twitterTitle}}
{{^twitterTitle}}
{{#title}}
<meta name=""twitter:title"" content=""{{title}}"">
{{/title}}
{{/twitterTitle}}
{{#twitterDescription}}
<meta name=""twitter:description"" content=""{{twitterDescription}}"">
{{/twitterDescription}}
{{^twitterDescription}}
{{#description}}
<meta name=""twitter:description"" content=""{{description}}"">
{{/description}}
{{^description}}
{{#summary}}
<meta name=""twitter:description"" content=""{{summary}}"">
{{/summary}}
{{/description}}
{{/twitterDescription}}
{{#canonicalUrl}}
<meta name=""twitter:url"" content=""{{canonicalUrl}}"">
{{/canonicalUrl}}
{{#twitterImageUrl}}
<meta name=""twitter:image"" content=""{{twitterImageUrl}}"">
{{/twitterImageUrl}}
{{#_siteTwitter}}
<meta name=""twitter:site"" content=""{{_siteTwitter}}"">
{{/_siteTwitter}}
{{#authorTwitter}}
<meta name=""twitter:creator"" content=""{{authorTwitter}}"">
{{/authorTwitter}}
{{#author}}
<meta name=""twitter:label1"" content=""Written by"">
<meta name=""twitter:data1"" content=""{{author}}"">
{{/author}}
{{#keywords}}
<meta name=""twitter:label2"" content=""Filed under"">
<meta name=""twitter:data2"" content=""{{keywords}}"">
{{/keywords}}";

            string schemaContent = @"<script type=""application/ld+json"">
{
    ""@context"": ""https://schema.org"",
    ""@type"": ""Article"",
    ""publisher"": {
        ""@type"": ""Organization"",
        ""name"": ""{{#_appTitle}}{{_appTitle}}{{/_appTitle}}{{^_appTitle}}Get Blogged by JoKi{{/_appTitle}}"",
        ""url"": ""{{#_appUrl}}{{_appUrl}}/{{/_appUrl}}"",
        ""logo"": {
            ""@type"": ""ImageObject"",
            ""url"": ""{{#_appUrl}}{{#_appLogoPath}}{{_appUrl}}/{{_appLogoPath}}{{/_appLogoPath}}{{^_appLogoPath}}{{_appUrl}}/favicon.png{{/_appLogoPath}}{{/_appUrl}}{{^_appUrl}}favicon.png{{/_appUrl}}"",
            ""width"": 60,
            ""height"": 60
        }
    },
    ""author"": {
        ""@type"": ""Person"",
        ""name"": ""{{#author}}{{author}}{{/author}}{{^author}}Jochen Kirstätter{{/author}}"",
        ""image"": {
            ""@type"": ""ImageObject"",
            ""url"": ""{{authorImageUrl}}"",
            ""width"": 100,
            ""height"": 100
        },
        ""url"": ""{{authorPageUrl}}"",
        ""sameAs"": [
            ""{{#_appUrl}}{{_appUrl}}/{{/_appUrl}}""
            {{#authorFacebook}},
            ""{{authorFacebook}}""
            {{/authorFacebook}}
            {{#authorTwitter}},
            ""https://x.com/{{authorTwitter}}""
            {{/authorTwitter}}
        ]
    },
    ""headline"": ""{{#title}}{{title}}{{/title}}"",
    ""url"": ""{{canonicalUrl}}"",
    ""datePublished"": ""{{#publishedAt}}{{publishedAt}}{{/publishedAt}}"",
    ""dateModified"": ""{{#updatedAt}}{{updatedAt}}{{/updatedAt}}{{^updatedAt}}{{#publishedAt}}{{publishedAt}}{{/publishedAt}}{{/updatedAt}}"",
    ""image"": {
        ""@type"": ""ImageObject"",
        ""url"": ""{{imageUrl}}"",
        ""width"": 1920,
        ""height"": 1277
    },
    ""keywords"": ""{{#keywords}}{{keywords}}{{/keywords}}"",
    ""description"": ""{{#description}}{{description}}{{/description}}{{^description}}{{#summary}}{{summary}}{{/summary}}{{/description}}"",
    ""mainEntityOfPage"": {
        ""@type"": ""WebPage"",
        ""@id"": ""{{#_appUrl}}{{_appUrl}}/{{/_appUrl}}""
    }
}
</script>";

            await File.WriteAllTextAsync(Path.Combine(pDir, "meta.tmpl.partial"), metaContent);
            await File.WriteAllTextAsync(Path.Combine(pDir, "opengraph.tmpl.partial"), opengraphContent);
            await File.WriteAllTextAsync(Path.Combine(pDir, "twitter.tmpl.partial"), twitterContent);
            await File.WriteAllTextAsync(Path.Combine(pDir, "schema.tmpl.partial"), schemaContent);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempExtractPath) && Directory.Exists(tempExtractPath))
            {
                try { Directory.Delete(tempExtractPath, true); } catch { }
            }
        }
    }

    public static string ConvertHandlebarsToDocfx(string hbsContent, bool isLayout = false, string? headerInjection = null, string? footerInjection = null, List<IconLink>? iconLinks = null)
    {
        if (string.IsNullOrWhiteSpace(hbsContent))
            return string.Empty;

        string result = hbsContent;

        if (isLayout)
        {
            result = "{{!include(/^public/.*/)}}\n{{!include(favicon.ico)}}\n{{!include(logo.svg)}}\n" + result;
        }

        // Convert Ghost Head & Foot using partial templates
        result = Regex.Replace(result, @"\{\{\s*ghost_head\s*\}\}", "{{>partials/ghost_head}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*ghost_foot\s*\}\}", "{{>partials/ghost_foot}}", RegexOptions.IgnoreCase);

        // Place docfx.min.css after viewport meta tag, or fall back to head if not present
        if (isLayout)
        {
            var viewportRegex = new Regex(@"<meta\s+name=[""']viewport[""'][^>]*>", RegexOptions.IgnoreCase);
            var docfxMeta = @"
    <link rel=""stylesheet"" href=""{{_rel}}public/docfx.min.css"">
    <meta name=""docfx:navrel"" content=""{{_navRel}}"">
    <meta name=""docfx:tocrel"" content=""{{_tocRel}}"">
    {{#_noindex}}<meta name=""searchOption"" content=""noindex"">{{/_noindex}}
    {{#_enableSearch}}<meta name=""docfx:rel"" content=""{{_rel}}"">{{/_enableSearch}}
    {{#_disableNewTab}}<meta name=""docfx:disablenewtab"" content=""true"">{{/_disableNewTab}}
    {{#_disableTocFilter}}<meta name=""docfx:disabletocfilter"" content=""true"">{{/_disableTocFilter}}
    {{#docurl}}<meta name=""docfx:docurl"" content=""{{docurl}}"">{{/docurl}}
    <meta name=""loc:inThisArticle"" content=""{{__global.inThisArticle}}"">
    <meta name=""loc:searchResultsCount"" content=""{{__global.searchResultsCount}}"">
    <meta name=""loc:searchNoResults"" content=""{{__global.searchNoResults}}"">
    <meta name=""loc:tocFilter"" content=""{{__global.tocFilter}}"">
    <meta name=""loc:nextArticle"" content=""{{__global.nextArticle}}"">
    <meta name=""loc:prevArticle"" content=""{{__global.prevArticle}}"">
    <meta name=""loc:themeLight"" content=""{{__global.themeLight}}"">
    <meta name=""loc:themeDark"" content=""{{__global.themeDark}}"">
    <meta name=""loc:themeAuto"" content=""{{__global.themeAuto}}"">
    <meta name=""loc:changeTheme"" content=""{{__global.changeTheme}}"">
    <meta name=""loc:copy"" content=""{{__global.copy}}"">
    <meta name=""loc:downloadPdf"" content=""{{__global.downloadPdf}}"">
    {{#_enableSearch}}
    <style>
        #search-results-container {
            display: none;
        }
        body[data-search=true] #site-main {
            display: none !important;
        }
        body[data-search=true] #search-results-container {
            display: block !important;
        }
    </style>
    {{/_enableSearch}}";

            if (viewportRegex.IsMatch(result))
            {
                result = viewportRegex.Replace(result, m => m.Value + docfxMeta, 1);
            }
            else if (result.Contains("<head>", StringComparison.OrdinalIgnoreCase))
            {
                result = Regex.Replace(result, @"<head\b[^>]*>", m => m.Value + docfxMeta, RegexOptions.IgnoreCase);
            }

            if (result.Contains("jquery.fitvids.js", StringComparison.OrdinalIgnoreCase))
            {
                var fitvidsRegex = new Regex(@"<script\s+[^>]*src=[""'][^""']*jquery\.fitvids\.js[""'][^>]*></script>", RegexOptions.IgnoreCase);
                result = fitvidsRegex.Replace(result, m => m.Value + "\n    <script>\n        $(function() {\n            var $postContent = $(\".post-full-content\");\n            if ($postContent.length) $postContent.fitVids();\n        });\n    </script>");
            }
        }

        // Apply injections
        if (isLayout && result.Contains("</head>", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Replace("</head>", "{{>partials/code_header}}\n</head>", StringComparison.OrdinalIgnoreCase);
        }
        if (isLayout && result.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Replace("</body>", "{{>partials/code_footer}}\n</body>", StringComparison.OrdinalIgnoreCase);
        }

        // Convert Site Metadata
        result = Regex.Replace(result, @"\{\{\s*@site\.title\s*\}\}", "{{_appTitle}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.title\s*\}\}", "{{_appTitle}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.description\s*\}\}", "{{_appDescription}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.description\s*\}\}", "{{_appDescription}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.logo\s*\}\}", "{{_appLogoPath}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.logo\s*\}\}", "{{_appLogoPath}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.icon\s*\}\}", "{{_appFaviconPath}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.icon\s*\}\}", "{{_appFaviconPath}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.cover_image\s*\}\}", "{{_appCoverImage}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.cover_image\s*\}\}", "{{_appCoverImage}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.url\s*\}\}", "{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.url\s*\}\}", "{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.locale\s*\}\}", "{{_lang}}", RegexOptions.IgnoreCase);

        // Convert copyright current year date helper
        result = Regex.Replace(result, @"\{\{\s*date\s+format=[""']YYYY[""']\s*\}\}", "{{_currentYear}}", RegexOptions.IgnoreCase);

        // Convert asset helper: {{asset "path"}} -> {{_rel}}public/path
        result = Regex.Replace(result, @"\{\{\s*asset\s+""?([^"" }]+)""?\s*\}\}", "{{_rel}}public/$1", RegexOptions.IgnoreCase);

        // Convert legacy Facebook/Twitter footer links to dynamic _siteSocialLinks
        result = Regex.Replace(result, @"\{\{#(?:if\s+@blog\.facebook|@blog\.facebook)\}\}.*?\{\{\/(?:if|@blog\.facebook)\}\}", "{{#_siteSocialLinks}}<a href=\"{{href}}\" target=\"_blank\" rel=\"noreferrer noopener\">{{title}}</a>{{/_siteSocialLinks}}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"\{\{#(?:if\s+@blog\.twitter|@blog\.twitter)\}\}.*?\{\{\/(?:if|@blog\.twitter)\}\}", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Convert branding/other variables
        result = Regex.Replace(result, @"\{\{\s*body_class\s*\}\}", "tex2jax_ignore {{bodyClass}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*post_class\s*\}\}", "{{postClass}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*meta_title\s*\}\}", "{{title}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*comment_id\s*\}\}", "{{uid}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*url(?:\s+[^}]+)?\s*\}\}", "{{_rel}}{{slug}}.html", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*pagination\s*\}\}", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*name\s*\}\}", "{{title}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*profile_image\s*\}\}", "{{avatar}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*cover_image\s*\}\}", "{{image}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*bio\s*\}\}", "{{bio}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*location\s*\}\}", "{{location}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*website\s*\}\}", "{{website}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*description\s*\}\}", "{{description}}", RegexOptions.IgnoreCase);

        // Convert Post tags
        result = Regex.Replace(result, @"\{\{\s*title\s*\}\}", "{{title}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*content\s*\}\}", "{{{conceptual}}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\{\s*content\s*\}\}\}", "{{{conceptual}}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*excerpt\s*\}\}", "{{summary}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*custom_excerpt\s*\}\}", "{{summary}}", RegexOptions.IgnoreCase);

        // Convert Block Helpers {{#post}} ... {{/post}}
        result = Regex.Replace(result, @"\{\{\s*#post\s*\}\}", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*/post\s*\}\}", "", RegexOptions.IgnoreCase);

        // Convert layouts references
        result = Regex.Replace(result, @"\{\{\!<\s*([^ }]+)\s*\}\}", m =>
        {
            string layoutName = m.Groups[1].Value.Trim().ToLowerInvariant();
            if (layoutName == "default")
                return "{{!master(layout/_master.tmpl)}}";
            return "{{!master(layout/_" + layoutName + ".tmpl)}}";
        }, RegexOptions.IgnoreCase);

        // Convert comments
        result = Regex.Replace(result, @"\{\{\!--", "{{!", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"--\}\}", "}}", RegexOptions.IgnoreCase);

        // Convert loops & conditional blocks using a stack for matching tag names
        var tagStack = new System.Collections.Generic.Stack<string>();

        result = Regex.Replace(result, @"\{\{\s*(#foreach|#has|#if|#unless|#is|\^is|\/foreach|\/has|\/if|\/unless|\/is)\s*([^}]*?)\s*\}\}", m =>
        {
            string marker = m.Groups[1].Value.ToLowerInvariant();
            string arg = m.Groups[2].Value.Trim();

            if (marker.StartsWith('#') || marker.StartsWith('^'))
            {
                string propertyName = arg;
                if (marker == "#foreach")
                {
                    propertyName = arg;
                    tagStack.Push(propertyName);
                    return "{{" + "#" + propertyName + "}}";
                }
                else if (marker == "#has")
                {
                    propertyName = "hasMultipleAuthors";
                    if (arg.Contains("author", StringComparison.OrdinalIgnoreCase))
                    {
                        propertyName = "hasMultipleAuthors";
                    }
                    tagStack.Push(propertyName);
                    return "{{" + "#" + propertyName + "}}";
                }
                else if (marker == "#is" || marker == "^is")
                {
                    propertyName = "isHome";
                    string cleanArg = arg.Replace("\"", "").Replace("'", "");
                    if (cleanArg.Equals("post", StringComparison.OrdinalIgnoreCase))
                        propertyName = "isPost";
                    else if (cleanArg.Equals("page", StringComparison.OrdinalIgnoreCase))
                        propertyName = "isPage";
                    else if (cleanArg.Equals("tag", StringComparison.OrdinalIgnoreCase))
                        propertyName = "isTagPage";
                    else if (cleanArg.Equals("author", StringComparison.OrdinalIgnoreCase))
                        propertyName = "isAuthorPage";
                    else if (cleanArg.Equals("home", StringComparison.OrdinalIgnoreCase))
                        propertyName = "isHome";

                    tagStack.Push(propertyName);
                    char prefix = marker[0];
                    return "{{" + prefix + propertyName + "}}";
                }
                else if (marker == "#if")
                {
                    tagStack.Push(propertyName);
                    return "{{" + "#" + propertyName + "}}";
                }
                else // #unless
                {
                    tagStack.Push(propertyName);
                    return "{{" + "^" + propertyName + "}}";
                }
            }
            else // closing tag
            {
                if (tagStack.Count > 0)
                {
                    return "{{" + "/" + tagStack.Pop() + "}}";
                }
                return "{{/posts}}"; // fallback
            }
        }, RegexOptions.IgnoreCase);

        // Remove Ghost {{#get ...}} tags along with their closing tags
        result = Regex.Replace(result, @"\{\{\s*#get\b(?:[^{}]|\{\{[^{}]*\}\})*\}\}", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*\/get\s*\}\}", "", RegexOptions.IgnoreCase);

        // Remove Ghost {{#contentFor ...}} and {{{block ...}}} tags
        result = Regex.Replace(result, @"\{\{\s*#contentFor\b(?:[^{}]|\{\{[^{}]*\}\})*\}\}", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*\/contentFor\s*\}\}", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\{\s*block\s+[^}]+\}\}\}", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*block\s+[^}]+\s*\}\}", "", RegexOptions.IgnoreCase);

        // Replace Ghost {{> header ...}} partial tags (and any surrounding conditional header wrappers in author/tag templates) with <header class="site-header outer">
        result = Regex.Replace(result, @"\{\{#if\s+feature_image\}\}\s*\{\{>\s*header\b[^}]*\}\}\s*\{\{else\}\}\s*\{\{>\s*header\b[^}]*\}\}\s*\{\{/if\}\}", "<header class=\"site-header outer\">", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{>\s*header\b[^}]*\}\}", "<header class=\"site-header outer\">", RegexOptions.IgnoreCase);

        // Convert partials
        result = Regex.Replace(result, @"\{\{>\s*""?([^"" }]+)""?\s*\}\}", m =>
        {
            string path = m.Groups[1].Value;
            if (path.StartsWith("partials/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("partials\\", StringComparison.OrdinalIgnoreCase))
            {
                return $"{{{{>{path}}}}}";
            }
            return $"{{{{>partials/{path}}}}}";
        }, RegexOptions.IgnoreCase);

        result = Regex.Replace(result, @"\{\{\{\s*body\s*\}\}\}", "{{!body}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*body\s*\}\}", "{{!body}}", RegexOptions.IgnoreCase);

        if (isLayout)
        {
            if (!result.Contains("docfx.min.js", StringComparison.OrdinalIgnoreCase))
            {
                result = Regex.Replace(result, @"</head\s*>", "    <script type=\"module\" src=\"{{_rel}}public/docfx.min.js\"></script>\n    <link rel=\"stylesheet\" href=\"{{_rel}}public/main.css\">\n</head>", RegexOptions.IgnoreCase);
            }
            if (!result.Contains("id=\"search-results\"", StringComparison.OrdinalIgnoreCase))
            {
                result = result.Replace("{{!body}}", @"{{!body}}
        {{#_enableSearch}}
        <main id=""search-results-container"" class=""site-main outer"">
            <div class=""inner"">
                <div id=""search-results"" class=""search-results""></div>
            </div>
        </main>
        <script>
            document.addEventListener('click', function(e) {
                var a = e.target.closest('#search-results .sr-item a');
                if (a) { a.removeAttribute('target'); }
            });
        </script>
        {{/_enableSearch}}");
            }
        }

        // Convert img_url custom helper references to use _rel prefix for path safety
        result = Regex.Replace(result, @"\{\{\s*img_url\s+""?([^"" }]+)""?(?:\s+[^}]+)?\s*\}\}", "{{_rel}}{{$1}}", RegexOptions.IgnoreCase);

        // Map snake_case variables to camelCase frontmatter names
        result = Regex.Replace(result, @"\bfeature_image\b", "featureImage", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bog_title\b", "ogTitle", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bog_description\b", "ogDescription", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\btwitter_title\b", "twitterTitle", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\btwitter_description\b", "twitterDescription", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\btwitter_image\b", "twitterImage", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bfacebook_title\b", "facebookTitle", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bfacebook_description\b", "facebookDescription", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bfacebook_image\b", "facebookImage", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bpublished_at\b", "publishedAt", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bcodeinjection_head\b", "codeinjectionHead", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bcodeinjection_foot\b", "codeinjectionFoot", RegexOptions.IgnoreCase);

        // Replace Ghost Handlebars date helpers with client-side dynamic year span
        result = Regex.Replace(result, @"\{\{\s*date\s+format=[""']YYYY[""']\s*\}\}", "<span class=\"js-current-year\"></span>", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*date\s*\}\}", "<span class=\"js-current-year\"></span>", RegexOptions.IgnoreCase);

        // Replace Ghost footer social links with compiled iconLinks
        var sbFooterSocial = new System.Text.StringBuilder();
        if (iconLinks != null && iconLinks.Count > 0)
        {
            foreach (var link in iconLinks)
            {
                if (string.IsNullOrWhiteSpace(link.Href)) continue;
                string title = !string.IsNullOrWhiteSpace(link.Title) ? link.Title : link.Icon;
                sbFooterSocial.AppendLine($"                    <a href=\"{link.Href}\" target=\"_blank\" rel=\"noreferrer noopener\">{title}</a>");
            }
        }
        string footerSocialMarkup = sbFooterSocial.ToString();
        if (!string.IsNullOrWhiteSpace(footerSocialMarkup))
        {
            result = Regex.Replace(result,
                @"(<nav\s+class=[""']site-footer-nav[""']\s*>[\s\S]*?<a\s+href=[""'][^""']*[""']>Latest Posts</a>)\s*[\s\S]*?(</nav>)",
                "$1\n" + footerSocialMarkup + "                $2",
                RegexOptions.IgnoreCase);
        }

        // Prepend {{_rel}} to template image variables inside html attributes to make them path-agnostic
        result = Regex.Replace(result, @"(src|srcset)\s*=\s*([""'])\{\{\s*(featureImage|image)\s*\}\}", "$1=$2{{_rel}}{{$3}}", RegexOptions.IgnoreCase);

        // Remove redundant fitVids inline script caller from post/page templates
        result = Regex.Replace(result, @"<script>\s*\$\(function\(\)\s*\{\s*var\s+\$postContent\s*=\s*\$\(\s*['""]\.post-full-content['""]\s*\);\s*\$postContent\.fitVids\(\);\s*\}\);\s*</script>", "", RegexOptions.IgnoreCase);

        if (!isLayout && !result.Contains("{{{conceptual}}}"))
        {
            if (result.Contains("{{{content}}}"))
            {
                result = result.Replace("{{{content}}}", "{{{conceptual}}}");
            }
            else if (result.Contains("{{content}}"))
            {
                result = result.Replace("{{content}}", "{{{conceptual}}}");
            }
            else if (result.Contains("{{#posts}}"))
            {
                result = Regex.Replace(result, @"\{\{#posts\}\}[\s\S]*?\{\{/posts\}\}", "<div class=\"conceptual-content\" style=\"width: 100%;\">{{{conceptual}}}</div>", RegexOptions.IgnoreCase);
            }
            else if (result.Contains("class=\"post-feed\""))
            {
                result = Regex.Replace(result, @"<div\s+class=[""']post-feed[""'][^>]*>", m => m.Value + "\n<div class=\"conceptual-content\" style=\"width: 100%;\">{{{conceptual}}}</div>", RegexOptions.IgnoreCase);
            }
            else if (result.Contains("<main"))
            {
                result = Regex.Replace(result, @"<main\b[^>]*>", m => m.Value + "\n<div class=\"conceptual-content\" style=\"width: 100%;\">{{{conceptual}}}</div>", RegexOptions.IgnoreCase);
            }
        }

        return result;
    }

    public static async Task<List<IconLink>> EnsureDocfxTemplateOverridesExistAsync(string rootDir, string customTemplatePath = "ghostfx", List<IconLink>? iconLinks = null, List<GhostNavItem>? navItems = null, List<BlogPostMetadata>? pages = null, List<BlogPostMetadata>? posts = null)
    {
        string layoutDir = Path.Combine(rootDir, customTemplatePath, "layout");
        Directory.CreateDirectory(layoutDir);

        string masterPath = Path.Combine(layoutDir, "_master.tmpl");
        if (!File.Exists(masterPath))
        {
            await File.WriteAllTextAsync(masterPath, DefaultMasterTemplate, System.Text.Encoding.UTF8);
        }

        string partialsDir = Path.Combine(rootDir, customTemplatePath, "partials");
        Directory.CreateDirectory(partialsDir);

        string ghostHeadPath = Path.Combine(partialsDir, "ghost_head.tmpl.partial");
        if (!File.Exists(ghostHeadPath))
        {
            string defaultHead = "{{>partials/meta}}\n{{>partials/opengraph}}\n{{>partials/twitter}}\n{{>partials/schema}}\n" +
                                 "{{#codeinjectionHead}}{{{codeinjectionHead}}}{{/codeinjectionHead}}\n";
            await File.WriteAllTextAsync(ghostHeadPath, defaultHead, System.Text.Encoding.UTF8);
        }

        string ghostFootPath = Path.Combine(partialsDir, "ghost_foot.tmpl.partial");
        if (!File.Exists(ghostFootPath))
        {
            string defaultFoot = "{{#codeinjectionFoot}}{{{codeinjectionFoot}}}{{/codeinjectionFoot}}\n";
            await File.WriteAllTextAsync(ghostFootPath, defaultFoot, System.Text.Encoding.UTF8);
        }

        string codeHeaderPath = Path.Combine(partialsDir, "code_header.tmpl.partial");
        if (!File.Exists(codeHeaderPath))
        {
            await File.WriteAllTextAsync(codeHeaderPath, "", System.Text.Encoding.UTF8);
        }

        string codeFooterPath = Path.Combine(partialsDir, "code_footer.tmpl.partial");
        if (!File.Exists(codeFooterPath))
        {
            await File.WriteAllTextAsync(codeFooterPath, "", System.Text.Encoding.UTF8);
        }

        string siteNavPath = Path.Combine(partialsDir, "site-nav.tmpl.partial");
        if (!File.Exists(siteNavPath) || navItems != null || iconLinks != null)
        {
            string defaultSiteNav = GenerateSiteNavPartialContent(navItems, pages, posts, iconLinks);
            await File.WriteAllTextAsync(siteNavPath, defaultSiteNav, System.Text.Encoding.UTF8);
        }

        string publicDir = Path.Combine(rootDir, customTemplatePath, "public");
        Directory.CreateDirectory(publicDir);

        string cssPath = Path.Combine(publicDir, "main.css");
        if (!File.Exists(cssPath))
        {
            await File.WriteAllTextAsync(cssPath, "");
        }

        var links = iconLinks ?? [
            new IconLink { Icon = "github", Href = "https://github.com/jochenkirstaetter/ghostfx", Title = "GitHub" }
        ];

        string jsPath = Path.Combine(publicDir, "main.js");
        if (File.Exists(jsPath))
        {
            string mainContent = await File.ReadAllTextAsync(jsPath);
            var match = Regex.Match(mainContent, @"iconLinks:\s*(\[[\s\S]*?\])", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string jsonArray = match.Groups[1].Value;
                try
                {
                    var parseOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };
                    var existingLinks = JsonSerializer.Deserialize<List<IconLink>>(jsonArray, parseOptions);
                    if (existingLinks != null)
                    {
                        links = MergeIconLinks(existingLinks, links);
                    }
                }
                catch { }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonLinks = JsonSerializer.Serialize(links, options);
            // indent the JSON array lines to look neat in JS
            jsonLinks = string.Join("\n", jsonLinks.Split('\n').Select((line, idx) => idx == 0 ? line : "  " + line));

            var regex = new Regex(@"iconLinks:\s*\[[\s\S]*?\]", RegexOptions.IgnoreCase);
            if (regex.IsMatch(mainContent))
            {
                mainContent = regex.Replace(mainContent, $"iconLinks: {jsonLinks}");
            }

            await File.WriteAllTextAsync(jsPath, mainContent, System.Text.Encoding.UTF8);
        }
        else
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonLinks = JsonSerializer.Serialize(links, options);

            bool hasGhostJs = File.Exists(Path.Combine(publicDir, "ghost.js"));
            string importGhost = hasGhostJs ? "import './ghost.js';\n\n" : "";

            string defaultJs = $$"""
            {{importGhost}}export default {
              iconLinks: {{jsonLinks}}
            }
            """;
            await File.WriteAllTextAsync(jsPath, defaultJs, System.Text.Encoding.UTF8);
        }
        return links;
    }

    private static List<IconLink> MergeIconLinks(List<IconLink> existingLinks, List<IconLink> newLinks)
    {
        var merged = new List<IconLink>(newLinks);
        var newIcons = newLinks.Select(l => l.Icon.ToLowerInvariant()).ToHashSet();

        foreach (var link in existingLinks)
        {
            if (!newIcons.Contains(link.Icon.ToLowerInvariant()))
            {
                merged.Add(link);
            }
        }

        return merged;
    }

    private static bool IsDevelopmentOrTestFile(string relativePath)
    {
        var normalized = "/" + relativePath.Replace('\\', '/').TrimStart('/', '\\').ToLowerInvariant();

        // Check common test folder patterns
        if (normalized.Contains("/abc/") || 
            normalized.Contains("/custom/") || 
            normalized.Contains("/sub/") ||
            normalized.Contains("/test/"))
        {
            return true;
        }

        // Check common test filename patterns
        var fileName = Path.GetFileNameWithoutExtension(normalized);
        if (fileName == "test" || 
            fileName.StartsWith("test-") || 
            fileName.StartsWith("test1") || 
            fileName.StartsWith("test2") || 
            fileName == "hello" || 
            fileName == "empty" || 
            fileName == "partial" || 
            fileName == "partial-error" || 
            fileName.StartsWith("rule-") ||
            fileName == "toggle")
        {
            return true;
        }

        return false;
    }

    public static string GetSocialIconFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        string lower = url.ToLowerInvariant();
        if (lower.Contains("github.com")) return "github";
        if (lower.Contains("twitter.com") || lower.Contains("x.com")) return "twitter";
        if (lower.Contains("facebook.com")) return "facebook";
        if (lower.Contains("linkedin.com")) return "linkedin";
        if (lower.Contains("youtube.com")) return "youtube";
        if (lower.Contains("instagram.com")) return "instagram";
        if (lower.Contains("mastodon")) return "mastodon";
        if (lower.Contains("threads.net")) return "threads";
        if (lower.Contains("discord")) return "discord";
        if (lower.Contains("reddit.com")) return "reddit";
        return string.Empty;
    }

    public static string GenerateSiteNavPartialContent(List<GhostNavItem>? navItems = null, List<BlogPostMetadata>? pages = null, List<BlogPostMetadata>? posts = null, List<IconLink>? iconLinks = null)
    {
        var sbNav = new System.Text.StringBuilder();
        if (navItems != null && navItems.Count > 0)
        {
            foreach (var nav in navItems)
            {
                if (string.IsNullOrWhiteSpace(nav.Label)) continue;
                string label = nav.Label;
                string url = nav.Url?.Trim() ?? "";
                string slug = label.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");

                string href;
                if (string.IsNullOrWhiteSpace(url) || url == "/" || url.Equals("home", StringComparison.OrdinalIgnoreCase))
                {
                    href = "{{_rel}}index.html";
                }
                else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    href = url;
                }
                else
                {
                    string pathSlug = url.Trim('/').Split('/').LastOrDefault() ?? "";
                    if (string.IsNullOrEmpty(pathSlug))
                    {
                        href = "{{_rel}}index.html";
                    }
                    else
                    {
                        href = "{{_rel}}" + pathSlug + ".html";
                    }
                }

                sbNav.AppendLine($"            <li class=\"nav-{slug}\" role=\"menuitem\"><a href=\"{href}\">{label}</a></li>");
            }
        }

        var sbSocial = new System.Text.StringBuilder();
        if (iconLinks != null && iconLinks.Count > 0)
        {
            foreach (var link in iconLinks)
            {
                if (string.IsNullOrWhiteSpace(link.Href)) continue;
                string iconLower = (link.Icon ?? "").ToLowerInvariant();
                string iconPartial = iconLower switch
                {
                    "facebook" => "{{>partials/icons/facebook}}",
                    "twitter" or "x" => "{{>partials/icons/twitter}}",
                    "linkedin" => "{{>partials/icons/linkedin}}",
                    "youtube" => "{{>partials/icons/youtube}}",
                    "reddit" => "{{>partials/icons/reddit}}",
                    "rss" => "{{>partials/icons/rss}}",
                    "email" or "mail" => "{{>partials/icons/email}}",
                    "github" => "<svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"currentColor\"><path d=\"M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z\"/></svg>",
                    _ => link.Title ?? link.Icon ?? ""
                };
                sbSocial.AppendLine($"            <a class=\"social-link social-link-{iconLower}\" href=\"{link.Href}\" title=\"{link.Title}\" target=\"_blank\" rel=\"noreferrer noopener\">{iconPartial}</a>");
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<nav class=\"site-nav\">");
        sb.AppendLine("    <div class=\"site-nav-left\">");
        sb.AppendLine("        {{^isHome}}");
        sb.AppendLine("            {{#_appLogoPath}}");
        sb.AppendLine("                <a class=\"site-nav-logo\" href=\"{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}\"><img src=\"{{_rel}}{{_appLogoPath}}\" alt=\"{{_appTitle}}\" /></a>");
        sb.AppendLine("            {{/_appLogoPath}}");
        sb.AppendLine("            {{^_appLogoPath}}");
        sb.AppendLine("                <a class=\"site-nav-logo\" href=\"{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}\">{{_appTitle}}</a>");
        sb.AppendLine("            {{/_appLogoPath}}");
        sb.AppendLine("        {{/isHome}}");
        sb.AppendLine("        <ul class=\"nav\" role=\"menu\">");
        sb.Append(sbNav.ToString());
        sb.AppendLine("        </ul>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"site-nav-right\">");
        sb.AppendLine("        {{#_enableSearch}}");
        sb.AppendLine("        <form class=\"search\" role=\"search\" id=\"search\" style=\"margin-right: 15px;\">");
        sb.AppendLine("            <input class=\"form-control\" id=\"search-query\" type=\"search\" disabled placeholder=\"Search\" autocomplete=\"off\" aria-label=\"Search\" style=\"border-radius: 20px; padding: 4px 12px; background: rgba(255,255,255,0.15); border: none; color: #fff;\">");
        sb.AppendLine("        </form>");
        sb.AppendLine("        {{/_enableSearch}}");
        sb.AppendLine("        <div class=\"social-links\">");
        sb.Append(sbSocial.ToString());
        sb.AppendLine("        </div>");
        sb.AppendLine("        {{#_appUrl}}");
        sb.AppendLine("        <a class=\"rss-button\" href=\"https://feedly.com/i/subscription/feed/{{_appUrl}}/rss/\" title=\"RSS\" target=\"_blank\" rel=\"noreferrer noopener\">{{>partials/icons/rss}}</a>");
        sb.AppendLine("        {{/_appUrl}}");
        sb.AppendLine("        <div class=\"dropdown\" style=\"display: inline-block; margin-left: 10px;\">");
        sb.AppendLine("            <a title=\"Change theme\" class=\"btn border-0 dropdown-toggle\" data-bs-toggle=\"dropdown\" aria-expanded=\"false\" style=\"color: #fff; text-decoration: none; padding: 0 5px;\">");
        sb.AppendLine("                <i class=\"bi bi-circle-half\" style=\"font-size: 1.6rem;\"></i>");
        sb.AppendLine("            </a>");
        sb.AppendLine("            <ul class=\"dropdown-menu dropdown-menu-end\">");
        sb.AppendLine("                <li><a class=\"dropdown-item\" href=\"#\" onclick=\"localStorage.setItem('theme','light');document.documentElement.setAttribute('data-bs-theme','light');return false;\"><i class=\"bi bi-sun\"></i> Light</a></li>");
        sb.AppendLine("                <li><a class=\"dropdown-item\" href=\"#\" onclick=\"localStorage.setItem('theme','dark');document.documentElement.setAttribute('data-bs-theme','dark');return false;\"><i class=\"bi bi-moon\"></i> Dark</a></li>");
        sb.AppendLine("                <li><a class=\"dropdown-item\" href=\"#\" onclick=\"localStorage.setItem('theme','auto');document.documentElement.setAttribute('data-bs-theme',window.matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light');return false;\"><i class=\"bi bi-circle-half\"></i> Auto</a></li>");
        sb.AppendLine("            </ul>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</nav>");
        return sb.ToString();
    }

    private static void PurgeDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.GetFiles(path))
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }

        foreach (var dir in Directory.GetDirectories(path))
        {
            PurgeDirectory(dir);
            try
            {
                Directory.Delete(dir);
            }
            catch { }
        }
    }
}
