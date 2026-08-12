using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

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
                                                          (function() {
                                                            var theme = localStorage.getItem('theme');
                                                            if (!theme) {
                                                              theme = 'auto';
                                                              try { localStorage.setItem('theme', 'auto'); } catch (e) {}
                                                            }
                                                            var eff = theme === 'auto' ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : theme;
                                                            document.documentElement.setAttribute('data-bs-theme', eff);
                                                          })();
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
        string? siteFacebook = null,
        List<GhostNavItem>? navItems = null,
        List<BlogPostMetadata>? pages = null,
        List<BlogPostMetadata>? posts = null)
    {
        string templatePath = customTemplatePath ?? "ghostfx";
        var links = await EnsureDocfxTemplateOverridesExistAsync(rootDir, templatePath, iconLinks, navItems, pages,
            posts);

        string partialsDir = Path.Combine(rootDir, templatePath, "partials");
        Directory.CreateDirectory(partialsDir);
        await File.WriteAllTextAsync(Path.Combine(partialsDir, "code_header.tmpl.partial"), headerCodeInjection ?? "",
            System.Text.Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(partialsDir, "code_footer.tmpl.partial"), footerCodeInjection ?? "",
            System.Text.Encoding.UTF8);

        string fullOutputDir = Path.GetFullPath(config.OutputDir);
        string fullRootDir = Path.GetFullPath(rootDir);

        string relOutputDir = Path.GetRelativePath(fullRootDir, fullOutputDir).Replace('\\', '/');
        if (relOutputDir == ".") relOutputDir = "";

        string articlesPattern = string.IsNullOrEmpty(relOutputDir) ? "**.md" : $"{relOutputDir}/**.md";
        string tocPattern = string.IsNullOrEmpty(relOutputDir) ? "**/toc.yml" : $"{relOutputDir}/**/toc.yml";
        string mediaPattern = string.IsNullOrEmpty(relOutputDir)
            ? "**/content/images/**"
            : $"{relOutputDir}/**/content/images/**";

        string? faviconPath = null;
        if (File.Exists(Path.Combine(fullRootDir, "favicon.png"))) faviconPath = "favicon.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "favicon.png")))
            faviconPath = string.IsNullOrEmpty(relOutputDir)
                ? "content/images/favicon.png"
                : $"{relOutputDir}/content/images/favicon.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.png")))
            faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.png" : $"{relOutputDir}/favicon.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "favicon.svg")))
            faviconPath = string.IsNullOrEmpty(relOutputDir)
                ? "content/images/favicon.svg"
                : $"{relOutputDir}/content/images/favicon.svg";
        else if (File.Exists(Path.Combine(fullRootDir, "favicon.svg"))) faviconPath = "favicon.svg";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.svg")))
            faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.svg" : $"{relOutputDir}/favicon.svg";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "favicon.ico")))
            faviconPath = string.IsNullOrEmpty(relOutputDir)
                ? "content/images/favicon.ico"
                : $"{relOutputDir}/content/images/favicon.ico";
        else if (File.Exists(Path.Combine(fullRootDir, "favicon.ico"))) faviconPath = "favicon.ico";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.ico")))
            faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.ico" : $"{relOutputDir}/favicon.ico";

        string? logoPath = null;
        if (File.Exists(Path.Combine(fullRootDir, "logo.png"))) logoPath = "logo.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "logo.png")))
            logoPath = string.IsNullOrEmpty(relOutputDir)
                ? "content/images/logo.png"
                : $"{relOutputDir}/content/images/logo.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "logo.png")))
            logoPath = string.IsNullOrEmpty(relOutputDir) ? "logo.png" : $"{relOutputDir}/logo.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "content", "images", "logo.svg")))
            logoPath = string.IsNullOrEmpty(relOutputDir)
                ? "content/images/logo.svg"
                : $"{relOutputDir}/content/images/logo.svg";
        else if (File.Exists(Path.Combine(fullRootDir, "logo.svg"))) logoPath = "logo.svg";
        else if (File.Exists(Path.Combine(fullOutputDir, "logo.svg")))
            logoPath = string.IsNullOrEmpty(relOutputDir) ? "logo.svg" : $"{relOutputDir}/logo.svg";
        else if (!string.IsNullOrEmpty(faviconPath)) logoPath = faviconPath;

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
                                globalMetadataNode["_siteTwitter"] =
                                    siteTwitter.StartsWith("@") ? siteTwitter : "@" + siteTwitter;
                            }

                            if (!string.IsNullOrWhiteSpace(siteFacebook))
                            {
                                globalMetadataNode["_siteFacebook"] = siteFacebook.StartsWith("http")
                                    ? siteFacebook
                                    : "https://facebook.com/" + siteFacebook;
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
            catch
            {
            }

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
                exclude = new string[]
                    { "draft/**", "published/**", "pages/**", templateExclude, "_site/**", "**/_site/**" }
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
            ["_appFooter"] =
                $"<span>Generated by <a href='https://github.com/jochenkirstaetter/ghostfx'>GhostFx</a> for DocFx</span>"
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
            globalMetadata["_siteFacebook"] =
                siteFacebook.StartsWith("http") ? siteFacebook : "https://facebook.com/" + siteFacebook;
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
                template = new string[]
                {
                    "default",
                    "modern",
                    Path.GetFileName(templatePath)
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

        bool isZip = File.Exists(themePath) && (themePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                                                !Directory.Exists(themePath));
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

        await ConvertGhostThemeFolderAsync(themePath, targetTemplateDir, headerInjection, footerInjection, iconLinks,
            navItems, pages, posts);
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

    private static async Task ConvertGhostThemeFolderAsync(string themePath, string targetTemplateDir,
        string headerInjection = "", string footerInjection = "", List<IconLink>? iconLinks = null,
        List<GhostNavItem>? navItems = null, List<BlogPostMetadata>? pages = null, List<BlogPostMetadata>? posts = null)
    {
        string publicDir = Path.Combine(targetTemplateDir, "public");
        Directory.CreateDirectory(publicDir);

        bool isZip = File.Exists(themePath) && (themePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                                                !Directory.Exists(themePath));
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
                string fileName = Path.GetFileName(file);
                if (fileName.Equals("main.js", StringComparison.OrdinalIgnoreCase)) continue;
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".css" || ext == ".js" || ext == ".png" || ext == ".jpg" || ext == ".svg" || ext == ".ico")
                {
                    try
                    {
                        File.Copy(file, Path.Combine(publicDir, fileName), true);
                    }
                    catch
                    {
                    }
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
                catch
                {
                }
            }

            // 3. Ensure public/main.css exists
            string mainCssPath = Path.Combine(publicDir, "main.css");
            if (!File.Exists(mainCssPath))
            {
                await File.WriteAllTextAsync(mainCssPath, "");
            }

            // 4. Ensure public/main.js exists & strictly adheres to specifications
            await EnsureDocfxTemplateOverridesExistAsync(rootDir, Path.GetFileName(targetTemplateDir), iconLinks,
                navItems, pages, posts);

            // Overwrite ghost_head.tmpl and ghost_foot.tmpl
            string destPartialsDir = Path.Combine(targetTemplateDir, "partials");
            Directory.CreateDirectory(destPartialsDir);

            string ghostHeadContent =
                "{{>partials/meta}}\n{{>partials/opengraph}}\n{{>partials/twitter}}\n{{>partials/schema}}\n" +
                "{{#codeinjectionHead}}{{{codeinjectionHead}}}{{/codeinjectionHead}}\n";
            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "ghost_head.tmpl.partial"), ghostHeadContent,
                System.Text.Encoding.UTF8);

            string ghostFootContent = "{{#codeinjectionFoot}}{{{codeinjectionFoot}}}{{/codeinjectionFoot}}\n";
            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "ghost_foot.tmpl.partial"), ghostFootContent,
                System.Text.Encoding.UTF8);

            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "code_header.tmpl.partial"),
                headerInjection ?? "", System.Text.Encoding.UTF8);
            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "code_footer.tmpl.partial"),
                footerInjection ?? "", System.Text.Encoding.UTF8);

            if (navItems == null || navItems.Count == 0)
            {
                navItems = ParseTocYml(rootDir);
            }

            string siteNavContent = GenerateSiteNavPartialContent(navItems, pages, posts, iconLinks, rootDir);
            await File.WriteAllTextAsync(Path.Combine(destPartialsDir, "site-nav.tmpl.partial"), siteNavContent,
                System.Text.Encoding.UTF8);

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
                                 Path.GetDirectoryName(hbsFile)
                                     ?.EndsWith("partials", StringComparison.OrdinalIgnoreCase) == true;

                bool isLayout = hbsNameLower == "default";
                string converted =
                    ConvertHandlebarsToDocfx(hbsContent, isLayout, headerInjection, footerInjection, iconLinks);

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

                    if (hbsNameLower == "post-card")
                    {
                        converted = """
                                    <article class="post-card post {{tagClass}} {{imageClass}}">

                                        {{#image}}
                                        <a class="post-card-image-link" href="{{_rel}}{{slug}}.html" aria-label="{{title}}">
                                            <img class="post-card-image"
                                                srcset="{{_rel}}{{.}} 300w,
                                                        {{_rel}}{{.}} 600w,
                                                        {{_rel}}{{.}} 1000w,
                                                        {{_rel}}{{.}} 2000w"
                                                sizes="(max-width: 1000px) 400px, 700px"
                                                src="{{_rel}}{{.}}"
                                                alt="{{title}}"
                                            />
                                        </a>
                                        {{/image}}

                                        <div class="post-card-content">
                                            <a class="post-card-content-link" href="{{_rel}}{{slug}}.html">
                                                <header class="post-card-header">
                                                    {{#primaryTag}}
                                                        <span class="post-card-tags">{{.}}</span>
                                                    {{/primaryTag}}
                                                    <h2 class="post-card-title">{{title}}</h2>
                                                </header>
                                                {{#excerpt}}
                                                <section class="post-card-excerpt">
                                                    <p>{{.}}</p>
                                                </section>
                                                {{/excerpt}}
                                            </a>
                                            <footer class="post-card-meta">
                                                <div class="post-card-byline-wrapper">
                                                    <ul class="author-list">
                                                    {{#authorName}}
                                                        <li class="author-list-item">
                                                            <div class="author-name-tooltip">
                                                                {{.}}
                                                            </div>

                                                            {{#authorImage}}
                                                                <a href="{{_rel}}author/{{authorSlug}}.html" class="static-avatar">
                                                                    <img class="author-profile-image" src="{{_rel}}{{.}}" alt="{{authorName}}" />
                                                                </a>
                                                            {{/authorImage}}
                                                            {{^authorImage}}
                                                                <a href="{{_rel}}author/{{authorSlug}}.html" class="static-avatar author-profile-image"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg></a>
                                                            {{/authorImage}}
                                                        </li>
                                                    {{/authorName}}
                                                    </ul>

                                                    <div class="post-card-byline-content">
                                                        {{#authorName}}<span><a href="{{_rel}}author/{{authorSlug}}.html">{{.}}</a></span>{{/authorName}}
                                                        <span class="post-card-byline-date"><time datetime="{{date}}">{{formattedDate}}</time></span>
                                                    </div>
                                                </div>
                                            </footer>

                                        </div>

                                    </article>
                                    """;
                    }
                    else if (hbsNameLower == "floating-header")
                    {
                        converted = """
                                    <div class="floating-header">
                                        <div class="floating-header-logo">
                                            <a href="{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}">
                                                {{#_appFaviconPath}}
                                                    <img src="{{_rel}}{{.}}" alt="{{_appTitle}} icon" />
                                                {{/_appFaviconPath}}
                                                <span>{{_appTitle}}</span>
                                            </a>
                                        </div>
                                        <span class="floating-header-divider">&mdash;</span>
                                        <div class="floating-header-title">{{#title}}{{title}}{{/title}}{{^title}}{{_appTitle}}{{/title}}</div>
                                        <div class="floating-header-share">
                                            <div class="floating-header-share-label">Share this {{>partials/icons/point}}</div>
                                            <a class="floating-header-share-x" href="https://x.com/intent/post?text={{encode title}}&amp;url={{_rel}}{{slug}}.html"
                                                target="_blank" rel="noopener noreferrer" onclick="window.open(this.href, 'share-x', 'width=550,height=450');return false;" title="Share on X">
                                                {{>partials/icons/x}}
                                            </a>
                                            <a class="floating-header-share-fb" href="https://www.facebook.com/sharer/sharer.php?u={{_rel}}{{slug}}.html"
                                                target="_blank" rel="noopener noreferrer" onclick="window.open(this.href, 'share-facebook','width=580,height=400');return false;" title="Share on Facebook">
                                                {{>partials/icons/facebook}}
                                            </a>
                                            <a class="floating-header-share-bsky" href="https://bsky.app/intent/compose?text={{encode title}}%20{{_rel}}{{slug}}.html"
                                                target="_blank" rel="noopener noreferrer" onclick="window.open(this.href, 'share-bluesky','width=580,height=420');return false;" title="Share on BlueSky">
                                                {{>partials/icons/bluesky}}
                                            </a>
                                            <a class="floating-header-share-masto" href="https://mastodon.social/share?text={{encode title}}%20{{_rel}}{{slug}}.html"
                                                target="_blank" rel="noopener noreferrer" onclick="window.open(this.href, 'share-mastodon','width=600,height=600');return false;" title="Share on Mastodon">
                                                {{>partials/icons/mastodon}}
                                            </a>
                                            <a class="floating-header-share-li" href="https://www.linkedin.com/feed/?shareActive=true&amp;text={{encode title}}%20{{_rel}}{{slug}}.html"
                                                target="_blank" rel="noopener noreferrer" onclick="window.open(this.href, 'share-linkedin','width=650,height=650');return false;" title="Share on LinkedIn">
                                                {{>partials/icons/linkedin}}
                                            </a>
                                            <a class="floating-header-share-re" href="https://reddit.com/submit?url={{_rel}}{{slug}}.html&title={{encode title}}"
                                                target="_blank" rel="noopener noreferrer" onclick="window.open(this.href, 'share-reddit','width=580,height=500');return false;" title="Share on Reddit">
                                                {{>partials/icons/reddit}}
                                            </a>
                                            <a class="floating-header-share-email" href="mailto:?subject={{encode title}}&body=Check out this site: {{_rel}}{{slug}}.html" title="Share via Email">
                                                {{>partials/icons/email}}
                                            </a>
                                        </div>
                                        <progress id="reading-progress" class="progress" value="0">
                                            <div class="progress-container">
                                                <span class="progress-bar"></span>
                                            </div>
                                        </progress>
                                    </div>

                                    <script>
                                    (function () {
                                        function initFloatingHeader() {
                                            var header = document.querySelector('.floating-header');
                                            if (!header) return;

                                            var progressBar = document.querySelector('#reading-progress');
                                            var title = document.querySelector('.post-full-title') || document.querySelector('.site-title') || document.querySelector('h1');

                                            try {
                                                var rawUrl = window.location.href;
                                                if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                                                    rawUrl = 'https://jochen.kirstaetter.name' + window.location.pathname;
                                                }
                                                var currentUrl = encodeURIComponent(rawUrl);
                                                var currentTitle = encodeURIComponent(document.title || (title ? title.innerText : ''));

                                                var x = header.querySelector('.floating-header-share-x, .floating-header-share-tw');
                                                if (x) x.href = 'https://x.com/intent/post?text=' + currentTitle + '&url=' + currentUrl;

                                                var fb = header.querySelector('.floating-header-share-fb');
                                                if (fb) fb.href = 'https://www.facebook.com/sharer/sharer.php?u=' + currentUrl;

                                                var bsky = header.querySelector('.floating-header-share-bsky');
                                                if (bsky) bsky.href = 'https://bsky.app/intent/compose?text=' + currentTitle + '%20' + currentUrl;

                                                var masto = header.querySelector('.floating-header-share-masto');
                                                if (masto) masto.href = 'https://mastodon.social/share?text=' + currentTitle + '%20' + currentUrl;

                                                var li = header.querySelector('.floating-header-share-li');
                                                if (li) li.href = 'https://www.linkedin.com/feed/?shareActive=true&text=' + currentTitle + '%20' + currentUrl;

                                                var re = header.querySelector('.floating-header-share-re');
                                                if (re) re.href = 'https://reddit.com/submit?url=' + currentUrl + '&title=' + currentTitle;

                                                var em = header.querySelector('.floating-header-share-email');
                                                if (em) em.href = 'mailto:?subject=' + currentTitle + '&body=Check out this site: ' + currentUrl;
                                            } catch (e) {}

                                            var ticking = false;

                                            function update() {
                                                var lastScrollY = window.scrollY || window.pageYOffset;
                                                var lastWindowHeight = window.innerHeight;
                                                var lastDocumentHeight = Math.max(
                                                    document.body.scrollHeight, document.documentElement.scrollHeight,
                                                    document.body.offsetHeight, document.documentElement.offsetHeight
                                                );

                                                var trigger = 150;
                                                if (title) {
                                                    var rect = title.getBoundingClientRect();
                                                    trigger = rect.top + lastScrollY + (title.offsetHeight || 40);
                                                }
                                                var progressMax = lastDocumentHeight - lastWindowHeight;

                                                if (lastScrollY >= trigger) {
                                                    header.classList.add('floating-active');
                                                } else {
                                                    header.classList.remove('floating-active');
                                                }

                                                if (progressBar && progressMax > 0) {
                                                    progressBar.setAttribute('max', progressMax);
                                                    progressBar.setAttribute('value', lastScrollY);
                                                }

                                                ticking = false;
                                            }

                                            function requestTick() {
                                                if (!ticking) {
                                                    requestAnimationFrame(update);
                                                }
                                                ticking = true;
                                            }

                                            window.addEventListener('scroll', requestTick, { passive: true });
                                            window.addEventListener('resize', requestTick, false);

                                            update();
                                        }

                                        if (document.readyState === 'loading') {
                                            document.addEventListener('DOMContentLoaded', initFloatingHeader);
                                        } else {
                                            initFloatingHeader();
                                        }
                                    })();
                                    </script>
                                    """;
                    }
                }
                else
                {
                    if (isLayout)
                    {
                        targetPath = Path.Combine(targetTemplateDir, "layout", "_master.tmpl");
                    }
                    else if (hbsNameLower == "post" || hbsNameLower == "page" || hbsNameLower == "tag" ||
                             hbsNameLower == "author" || hbsNameLower == "index")
                    {
                        string partialLayoutDir = Path.Combine(targetTemplateDir, "partials");
                        Directory.CreateDirectory(partialLayoutDir);
                        string partialLayoutPath =
                            Path.Combine(partialLayoutDir, $"{hbsNameLower}_layout.tmpl.partial");

                        string partialContent;
                        if (hbsNameLower == "index")
                        {
                            partialContent = """
                                             <header class="site-home-header">
                                                 <div class="outer site-header-background {{#coverImage}}responsive-header-img{{/coverImage}}{{^coverImage}}{{#_appCoverImage}}responsive-header-img{{/_appCoverImage}}{{/coverImage}}" style="{{#coverImage}}background-image: url('{{_rel}}{{.}}');{{/coverImage}}{{^coverImage}}{{#_appCoverImage}}background-image: url('{{_rel}}{{.}}');{{/_appCoverImage}}{{/coverImage}}">
                                                     <div class="inner">
                                                         {{>partials/site-nav}}
                                                         <div class="site-header-content">
                                                             <h1 class="site-title">
                                                                 {{#_appLogoPath}}
                                                                     <img class="site-logo" src="{{_rel}}{{.}}" alt="{{#title}}{{title}}{{/title}}{{^title}}{{_appTitle}}{{/title}}" />
                                                                 {{/_appLogoPath}}
                                                                 {{^_appLogoPath}}
                                                                     {{#title}}{{title}}{{/title}}{{^title}}{{_appTitle}}{{/title}}
                                                                 {{/_appLogoPath}}
                                                             </h1>
                                                             <h2 class="site-description">{{#description}}{{description}}{{/description}}{{^description}}{{_appDescription}}{{/description}}</h2>
                                                         </div>
                                                     </div>
                                                 </div>
                                             </header>

                                             <main id="site-main" class="site-main outer">
                                                 <div class="inner">

                                                     <div class="post-feed">
                                                         {{#posts}}
                                                             {{>partials/post-card}}
                                                         {{/posts}}
                                                         {{{conceptual}}}
                                                     </div>

                                                 </div>
                                             </main>
                                             """;
                        }
                        else if (hbsNameLower == "author")
                        {
                            partialContent = """
                                             <header class="site-header outer">
                                                 <div class="inner">
                                                     {{>partials/site-nav}}
                                                 </div>
                                             </header>

                                             <main id="site-main" class="site-main outer">
                                                 <div class="inner">
                                                     <article class="post-full {{postClass}} {{^image}}no-image{{/image}}">
                                                         {{#image}}
                                                         <figure class="post-full-image">
                                                             <a target="_blank" rel="noopener noreferrer nofollow" href="{{_rel}}{{.}}"><img src="{{_rel}}{{.}}" alt="{{title}}" /></a>
                                                         </figure>
                                                         {{/image}}

                                                         <header class="post-full-header author-header">
                                                             {{#authorImage}}
                                                                 <img class="author-profile-image" src="{{_rel}}{{.}}" alt="{{title}}" />
                                                             {{/authorImage}}
                                                             <h1 class="site-title">{{title}}</h1>
                                                             {{#description}}
                                                                 <h2 class="site-description">{{.}}</h2>
                                                             {{/description}}
                                                             <div class="author-meta">
                                                                 {{#location}}
                                                                     <div class="author-location"><svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor" style="vertical-align: text-bottom; margin-right: 4px;"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/></svg>{{.}}</div>
                                                                 {{/location}}
                                                                 {{#website}}
                                                                     <span class="author-link"><a href="{{.}}" target="_blank" rel="noopener"><svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor" style="vertical-align: text-bottom; margin-right: 4px;"><path d="M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z"/></svg>{{.}}</a></span>
                                                                 {{/website}}
                                                                 {{#authorTwitter}}
                                                                     <span class="author-social-link"><a href="https://twitter.com/{{.}}" target="_blank" rel="noopener"><svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor" style="vertical-align: text-bottom; margin-right: 4px;"><path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/></svg>{{.}}</a></span>
                                                                 {{/authorTwitter}}
                                                                 {{#authorFacebook}}
                                                                     <span class="author-social-link"><a href="{{.}}" target="_blank" rel="noopener"><svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor" style="vertical-align: text-bottom; margin-right: 4px;"><path d="M22 12c0-5.523-4.477-10-10-10S2 6.477 2 12c0 4.991 3.657 9.128 8.438 9.878v-6.987h-2.54V12h2.54V9.797c0-2.506 1.492-3.89 3.777-3.89 1.094 0 2.238.195 2.238.195v2.46h-1.26c-1.243 0-1.63.771-1.63 1.562V12h2.773l-.443 2.89h-2.33v6.988C18.343 21.128 22 16.991 22 12z"/></svg>Facebook</a></span>
                                                                 {{/authorFacebook}}
                                                             </div>
                                                         </header>

                                                         <section class="post-full-content">
                                                             <div class="post-content">
                                                                 {{#posts}}
                                                                     {{>partials/post-card}}
                                                                 {{/posts}}
                                                                 {{{conceptual}}}
                                                             </div>
                                                         </section>
                                                     </article>
                                                 </div>
                                             </main>
                                             """;
                        }
                        else if (hbsNameLower == "tag")
                        {
                            partialContent = """
                                             <header class="site-header outer">
                                                 <div class="inner">
                                                     {{>partials/site-nav}}
                                                 </div>
                                             </header>

                                             <main id="site-main" class="site-main outer">
                                                 <div class="inner">
                                                     <article class="post-full {{postClass}} {{^image}}{{^featureImage}}no-image{{/featureImage}}{{/image}}">
                                                         {{#image}}
                                                         <figure class="post-full-image">
                                                             <a target="_blank" rel="noopener noreferrer nofollow" href="{{_rel}}{{.}}"><img src="{{_rel}}{{.}}" alt="{{title}}" /></a>
                                                         </figure>
                                                         {{/image}}
                                                         {{^image}}
                                                         {{#featureImage}}
                                                         <figure class="post-full-image">
                                                             <a target="_blank" rel="noopener noreferrer nofollow" href="{{_rel}}{{.}}"><img src="{{_rel}}{{.}}" alt="{{title}}" /></a>
                                                         </figure>
                                                         {{/featureImage}}
                                                         {{/image}}

                                                         <header class="post-full-header tag-header">
                                                             <h1 class="site-title">{{title}}</h1>
                                                             <h2 class="site-description">
                                                                 {{#description}}
                                                                     {{.}}
                                                                 {{/description}}
                                                                 {{^description}}
                                                                     A collection of posts tagged with {{title}}
                                                                 {{/description}}
                                                             </h2>
                                                         </header>

                                                         <section class="post-full-content">
                                                             <div class="post-content">
                                                                 <div class="post-feed">
                                                                     {{#posts}}
                                                                         {{>partials/post-card}}
                                                                     {{/posts}}
                                                                     {{{conceptual}}}
                                                                 </div>
                                                             </div>
                                                         </section>
                                                     </article>
                                                 </div>
                                             </main>
                                             """;
                        }
                        else if (hbsNameLower == "page")
                        {
                            partialContent = """
                                             <header class="site-header outer">
                                                 <div class="inner">
                                                     {{>partials/site-nav}}
                                                 </div>
                                             </header>

                                             <main id="site-main" class="site-main outer">
                                                 <div class="inner">
                                                     <article class="post-full {{postClass}} {{^image}}no-image{{/image}}">
                                                         {{#image}}
                                                         <figure class="post-full-image">
                                                             <a target="_blank" rel="noopener noreferrer nofollow" href="{{_rel}}{{.}}"><img src="{{_rel}}{{.}}" alt="{{title}}" /></a>
                                                         </figure>
                                                         {{/image}}

                                                         <header class="post-full-header">
                                                             <h1 class="post-full-title">{{title}}</h1>
                                                         </header>

                                                         <section class="post-full-content">
                                                             <div class="post-content">
                                                                 {{{conceptual}}}
                                                             </div>
                                                         </section>
                                                     </article>
                                                 </div>
                                             </main>
                                             """;
                        }
                        else
                        {
                            partialContent = Regex.Replace(converted, @"^\{\{!master\([^)]+\)\}\}\s*\r?\n?", "",
                                RegexOptions.IgnoreCase);
                        }

                        await File.WriteAllTextAsync(partialLayoutPath, partialContent, System.Text.Encoding.UTF8);

                        targetPath = Path.Combine(targetTemplateDir, $"{hbsNameLower}.html.primary.tmpl");
                        converted = $"{{!master(layout/_master.tmpl)}}\n{{{{>partials/{hbsNameLower}_layout}}}}";
                    }
                    else if (hbsNameLower == "error-404")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "error-404.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "error")
                    {
                        targetPath = Path.Combine(targetTemplateDir, "error.html.primary.tmpl");
                    }
                    else if (hbsNameLower == "archive" || hbsNameLower == "search" || hbsNameLower == "private" ||
                             hbsNameLower == "subscribe")
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
            }

            // Write conceptual.html.primary.tmpl layout router
            string conceptualPath = Path.Combine(targetTemplateDir, "conceptual.html.primary.tmpl");
            string conceptualContent = @"{{!master(layout/_master.tmpl)}}
{{#isHome}}
{{>partials/index_layout}}
{{/isHome}}
{{#isAuthorPage}}
{{>partials/author_layout}}
{{/isAuthorPage}}
{{#isTagPage}}
{{>partials/tag_layout}}
{{/isTagPage}}
{{#isTagsIndexPage}}
{{>partials/tag_layout}}
{{/isTagsIndexPage}}
{{#isPost}}
{{^isHome}}
{{^isAuthorPage}}
{{^isTagPage}}
{{^isTagsIndexPage}}
{{>partials/post_layout}}
{{/isTagsIndexPage}}
{{/isTagPage}}
{{/isAuthorPage}}
{{/isHome}}
{{/isPost}}
{{#isPage}}
{{^isHome}}
{{^isAuthorPage}}
{{^isTagPage}}
{{^isTagsIndexPage}}
{{^isPost}}
{{>partials/page_layout}}
{{/isPost}}
{{/isTagsIndexPage}}
{{/isTagPage}}
{{/isAuthorPage}}
{{/isHome}}
{{/isPage}}
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

            string opengraphContent =
                @"<meta property=""og:site_name"" content=""{{#_appTitle}}{{_appTitle}}{{/_appTitle}}{{^_appTitle}}Get Blogged by JoKi{{/_appTitle}}"">
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
                try
                {
                    Directory.Delete(tempExtractPath, true);
                }
                catch
                {
                }
            }
        }
    }

    public static string ConvertHandlebarsToDocfx(string hbsContent, bool isLayout = false,
        string? headerInjection = null, string? footerInjection = null, List<IconLink>? iconLinks = null)
    {
        if (string.IsNullOrWhiteSpace(hbsContent))
            return string.Empty;

        string result = hbsContent;

        if (isLayout)
        {
            result = "{{!include(/^public/.*/)}}\n{{!include(favicon.ico)}}\n{{!include(logo.svg)}}\n" + result;
            if (!result.Contains("{{>partials/floating-header}}", StringComparison.OrdinalIgnoreCase))
            {
                result = Regex.Replace(result, @"(<div\s+class=[""']site-wrapper[""'][^>]*>)",
                    "$1\n\n        {{>partials/floating-header}}\n", RegexOptions.IgnoreCase);
            }
        }

        // Convert Ghost Head & Foot using partial templates
        result = Regex.Replace(result, @"\{\{\s*ghost_head\s*\}\}", "{{>partials/ghost_head}}",
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*ghost_foot\s*\}\}", "{{>partials/ghost_foot}}",
            RegexOptions.IgnoreCase);

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
                var fitvidsRegex = new Regex(@"<script\s+[^>]*src=[""'][^""']*jquery\.fitvids\.js[""'][^>]*></script>",
                    RegexOptions.IgnoreCase);
                result = fitvidsRegex.Replace(result,
                    m => m.Value +
                         "\n    <script>\n        $(function() {\n            var $postContent = $(\".post-full-content\");\n            if ($postContent.length) $postContent.fitVids();\n        });\n    </script>");
            }
        }

        // Apply injections
        if (isLayout && result.Contains("</head>", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Replace("</head>", "{{>partials/code_header}}\n</head>",
                StringComparison.OrdinalIgnoreCase);
        }

        if (isLayout && result.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Replace("</body>", "{{>partials/code_footer}}\n</body>",
                StringComparison.OrdinalIgnoreCase);
        }

        // Convert Site Metadata
        result = Regex.Replace(result, @"\{\{\s*@site\.title\s*\}\}", "{{_appTitle}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.title\s*\}\}", "{{_appTitle}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.description\s*\}\}", "{{_appDescription}}",
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.description\s*\}\}", "{{_appDescription}}",
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.logo\s*\}\}", "{{_appLogoPath}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.logo\s*\}\}", "{{_appLogoPath}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.icon\s*\}\}", "{{_appFaviconPath}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.icon\s*\}\}", "{{_appFaviconPath}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.cover_image\s*\}\}", "{{_appCoverImage}}",
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.cover_image\s*\}\}", "{{_appCoverImage}}",
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.url\s*\}\}",
            "{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.url\s*\}\}",
            "{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.locale\s*\}\}", "{{_lang}}", RegexOptions.IgnoreCase);

        // Convert copyright current year date helper
        result = Regex.Replace(result, @"\{\{\s*date\s+format=[""']YYYY[""']\s*\}\}", "{{_currentYear}}",
            RegexOptions.IgnoreCase);

        // Convert asset helper: {{asset "path"}} -> {{_rel}}public/path
        result = Regex.Replace(result, @"\{\{\s*asset\s+""?([^"" }]+)""?\s*\}\}", "{{_rel}}public/$1",
            RegexOptions.IgnoreCase);

        // Convert legacy Facebook/Twitter footer links to dynamic _siteSocialLinks
        result = Regex.Replace(result,
            @"\{\{#(?:if\s+@blog\.facebook|@blog\.facebook)\}\}.*?\{\{\/(?:if|@blog\.facebook)\}\}",
            "{{#_siteSocialLinks}}<a href=\"{{href}}\" target=\"_blank\" rel=\"noreferrer noopener\">{{title}}</a>{{/_siteSocialLinks}}",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result,
            @"\{\{#(?:if\s+@blog\.twitter|@blog\.twitter)\}\}.*?\{\{\/(?:if|@blog\.twitter)\}\}", "",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Convert branding/other variables
        result = Regex.Replace(result, @"\{\{\s*body_class\s*\}\}", "tex2jax_ignore {{bodyClass}}",
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*post_class\s*\}\}", "{{postClass}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*meta_title\s*\}\}", "{{title}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*comment_id\s*\}\}", "{{uid}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*url(?:\s+[^}]+)?\s*\}\}", "{{_rel}}{{slug}}.html",
            RegexOptions.IgnoreCase);
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

        result = Regex.Replace(result,
            @"\{\{\s*(#foreach|#has|#if|\^if|#unless|\^unless|#is|\^is|\/foreach|\/has|\/if|\/unless|\/is)\s*([^}]*?)\s*\}\}",
            m =>
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
                    else if (marker == "^if")
                    {
                        tagStack.Push(propertyName);
                        return "{{" + "^" + propertyName + "}}";
                    }
                    else // #unless or ^unless
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
        result = Regex.Replace(result, @"\{\{\s*#contentFor\b(?:[^{}]|\{\{[^{}]*\}\})*\}\}", "",
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*\/contentFor\s*\}\}", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\{\s*block\s+[^}]+\}\}\}", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*block\s+[^}]+\s*\}\}", "", RegexOptions.IgnoreCase);

        // Replace Ghost {{> header ...}} partial tags (and any surrounding conditional header wrappers in author/tag templates) with <header class="site-header outer">
        result = Regex.Replace(result,
            @"\{\{#if\s+feature_image\}\}\s*\{\{>\s*header\b[^}]*\}\}\s*\{\{else\}\}\s*\{\{>\s*header\b[^}]*\}\}\s*\{\{/if\}\}",
            "<header class=\"site-header outer\">", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{>\s*header\b[^}]*\}\}", "<header class=\"site-header outer\">",
            RegexOptions.IgnoreCase);

        // Convert partials
        result = Regex.Replace(result, @"\{\{>\s*""?([^"" }]+)""?\s*\}\}", m =>
        {
            string path = m.Groups[1].Value;
            if (path.StartsWith("partials/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("partials\\", StringComparison.OrdinalIgnoreCase))
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
                result = Regex.Replace(result, @"</head\s*>",
                    "    <script type=\"module\" src=\"{{_rel}}public/docfx.min.js\"></script>\n    <link rel=\"stylesheet\" href=\"{{_rel}}public/main.css\">\n</head>",
                    RegexOptions.IgnoreCase);
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
            (function() {
                var cleanLink = function(a) {
                    if (a && a.href) {
                        a.removeAttribute('target');
                        a.href = a.href.replace(/(\?|&)q=[^&]*(&|$)/, '$1').replace(/[\?&]$/, '');
                    }
                };
                document.addEventListener('click', function(e) {
                    var a = e.target.closest('#search-results a');
                    if (a) { cleanLink(a); }
                }, true);
                var sr = document.getElementById('search-results');
                if (sr) {
                    new MutationObserver(function() {
                        var links = sr.querySelectorAll('a[href*=""?q=""], a[href*=""&q=""]');
                        for (var i = 0; i < links.length; i++) { cleanLink(links[i]); }
                    }).observe(sr, { childList: true, subtree: true });
                }
                if (window.location.search.indexOf('q=') !== -1) {
                    var cleanSearch = window.location.search.replace(/(\?|&)q=[^&]*(&|$)/, '$1').replace(/[\?&]$/, '');
                    var cleanUrl = window.location.pathname + cleanSearch + window.location.hash;
                    window.history.replaceState(null, '', cleanUrl);
                }
            })();
        </script>
        {{/_enableSearch}}");
            }
        }

        // Convert img_url custom helper references to use _rel prefix for path safety
        result = Regex.Replace(result, @"\{\{\s*img_url\s+""?([^"" }]+)""?(?:\s+[^}]+)?\s*\}\}", "{{_rel}}{{$1}}",
            RegexOptions.IgnoreCase);

        // Map snake_case variables to camelCase frontmatter names
        result = Regex.Replace(result, @"\bfeature_image\b", "image", RegexOptions.IgnoreCase);
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
        result = Regex.Replace(result, @"\{\{\s*date\s+format=[""']YYYY[""']\s*\}\}",
            "<span class=\"js-current-year\"></span>", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*date\s*\}\}", "<span class=\"js-current-year\"></span>",
            RegexOptions.IgnoreCase);

        // Replace Ghost footer social links with compiled iconLinks
        var sbFooterSocial = new System.Text.StringBuilder();
        if (iconLinks != null && iconLinks.Count > 0)
        {
            foreach (var link in iconLinks)
            {
                if (string.IsNullOrWhiteSpace(link.Href)) continue;
                string title = !string.IsNullOrWhiteSpace(link.Title) ? link.Title : link.Icon;
                sbFooterSocial.AppendLine(
                    $"                    <a href=\"{link.Href}\" target=\"_blank\" rel=\"noreferrer noopener\">{title}</a>");
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
        result = Regex.Replace(result, @"(src|srcset)\s*=\s*([""'])\{\{\s*(featureImage|image)\s*\}\}",
            "$1=$2{{_rel}}{{$3}}", RegexOptions.IgnoreCase);

        // Remove redundant fitVids inline script caller from post/page templates
        result = Regex.Replace(result,
            @"<script>\s*\$\(function\(\)\s*\{\s*var\s+\$postContent\s*=\s*\$\(\s*['""]\.post-full-content['""]\s*\);\s*\$postContent\.fitVids\(\);\s*\}\);\s*</script>",
            "", RegexOptions.IgnoreCase);

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
            else if (result.Contains("class=\"post-feed\""))
            {
                result = Regex.Replace(result, @"<div\s+class=[""']post-feed[""'][^>]*>",
                    m => m.Value + "\n{{{conceptual}}}", RegexOptions.IgnoreCase);
            }
            else if (result.Contains("<main"))
            {
                result = Regex.Replace(result, @"<main\b[^>]*>",
                    m => m.Value + "\n<div class=\"conceptual-content\" style=\"width: 100%;\">{{{conceptual}}}</div>",
                    RegexOptions.IgnoreCase);
            }
        }

        return result;
    }

    public static async Task<List<IconLink>> EnsureDocfxTemplateOverridesExistAsync(string rootDir,
        string customTemplatePath = "ghostfx", List<IconLink>? iconLinks = null, List<GhostNavItem>? navItems = null,
        List<BlogPostMetadata>? pages = null, List<BlogPostMetadata>? posts = null)
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
            string defaultHead =
                "{{>partials/meta}}\n{{>partials/opengraph}}\n{{>partials/twitter}}\n{{>partials/schema}}\n" +
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

        if (navItems == null || navItems.Count == 0)
        {
            navItems = ParseTocYml(rootDir);
        }

        string siteNavPath = Path.Combine(partialsDir, "site-nav.tmpl.partial");
        bool isNavEmpty = false;
        if (File.Exists(siteNavPath))
        {
            string currentSiteNav = await File.ReadAllTextAsync(siteNavPath);
            if (!currentSiteNav.Contains("<li class=\"nav-") && !currentSiteNav.Contains("role=\"menuitem\""))
            {
                isNavEmpty = true;
            }
        }

        if (!File.Exists(siteNavPath) || isNavEmpty || (navItems != null && navItems.Count > 0))
        {
            string defaultSiteNav = GenerateSiteNavPartialContent(navItems, pages, posts, iconLinks, rootDir);
            await File.WriteAllTextAsync(siteNavPath, defaultSiteNav, System.Text.Encoding.UTF8);
        }

        string postCardPath = Path.Combine(partialsDir, "post-card.tmpl.partial");
        string defaultPostCard = """
                                 <article class="post-card post {{tagClass}} {{imageClass}}">

                                     {{#image}}
                                     <a class="post-card-image-link" href="{{_rel}}{{slug}}.html" aria-label="{{title}}">
                                         <img class="post-card-image"
                                             src="{{_rel}}{{image}}"
                                             alt="{{title}}"
                                         />
                                     </a>
                                     {{/image}}

                                     <div class="post-card-content">
                                         <div class="post-card-content-header">
                                             {{#primaryTag}}
                                                 {{#tagSlug}}
                                                     <span class="post-card-tags"><a href="{{_rel}}tag/{{.}}.html">{{primaryTag}}</a></span>
                                                 {{/tagSlug}}
                                                 {{^tagSlug}}
                                                     <span class="post-card-tags">{{.}}</span>
                                                 {{/tagSlug}}
                                             {{/primaryTag}}
                                         </div>
                                         <a class="post-card-content-link" href="{{_rel}}{{slug}}.html">
                                             <header class="post-card-header">
                                                 <h2 class="post-card-title">{{title}}</h2>
                                             </header>
                                             {{#excerpt}}
                                             <section class="post-card-excerpt">
                                                 <p>{{.}}</p>
                                             </section>
                                             {{/excerpt}}
                                         </a>
                                         <footer class="post-card-meta">
                                             <div class="post-card-byline-wrapper">
                                                 <ul class="author-list">
                                                 {{#authorName}}
                                                     <li class="author-list-item">
                                                         <div class="author-name-tooltip">
                                                             {{.}}
                                                         </div>

                                                         {{#authorImage}}
                                                             <a href="{{_rel}}author/{{authorSlug}}.html" class="static-avatar">
                                                                 <img class="author-profile-image" src="{{_rel}}{{.}}" alt="{{authorName}}" />
                                                             </a>
                                                         {{/authorImage}}
                                                         {{^authorImage}}
                                                             <a href="{{_rel}}author/{{authorSlug}}.html" class="static-avatar author-profile-image"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg></a>
                                                         {{/authorImage}}
                                                     </li>
                                                 {{/authorName}}
                                                 </ul>

                                                 <div class="post-card-byline-content">
                                                     {{#authorName}}<span><a href="{{_rel}}author/{{authorSlug}}.html">{{.}}</a></span>{{/authorName}}
                                                     <span class="post-card-byline-date"><time datetime="{{date}}">{{formattedDate}}</time></span>
                                                 </div>
                                             </div>
                                         </footer>

                                     </div>

                                 </article>
                                 """;
        await File.WriteAllTextAsync(postCardPath, defaultPostCard, System.Text.Encoding.UTF8);

        string indexTmplPath = Path.Combine(rootDir, customTemplatePath, "index.html.primary.tmpl");
        if (!File.Exists(indexTmplPath))
        {
            string defaultIndexTmpl = """
                                      {{!master(layout/_master.tmpl)}}

                                      <header class="site-home-header">
                                          <div class="outer site-header-background {{#coverImage}}responsive-header-img{{/coverImage}}{{^coverImage}}{{#_appCoverImage}}responsive-header-img{{/_appCoverImage}}{{/coverImage}}" style="{{#coverImage}}background-image: url('{{_rel}}{{.}}');{{/coverImage}}{{^coverImage}}{{#_appCoverImage}}background-image: url('{{_rel}}{{.}}');{{/_appCoverImage}}{{/coverImage}}">
                                              <div class="inner">
                                                  {{>partials/site-nav}}
                                                  <div class="site-header-content">
                                                      <h1 class="site-title">
                                                          {{#_appLogoPath}}
                                                              <img class="site-logo" src="{{_rel}}{{.}}" alt="{{#title}}{{title}}{{/title}}{{^title}}{{_appTitle}}{{/title}}" />
                                                          {{/_appLogoPath}}
                                                          {{^_appLogoPath}}
                                                              {{#title}}{{title}}{{/title}}{{^title}}{{_appTitle}}{{/title}}
                                                          {{/_appLogoPath}}
                                                      </h1>
                                                      <h2 class="site-description">{{#description}}{{description}}{{/description}}{{^description}}{{_appDescription}}{{/description}}</h2>
                                                  </div>
                                              </div>
                                          </div>
                                      </header>

                                      <main id="site-main" class="site-main outer">
                                          <div class="inner">

                                              <div class="post-feed">
                                                  {{#posts}}
                                                      {{>partials/post-card}}
                                                  {{/posts}}
                                                  {{{conceptual}}}
                                              </div>

                                          </div>
                                      </main>
                                      """;
            await File.WriteAllTextAsync(indexTmplPath, defaultIndexTmpl, System.Text.Encoding.UTF8);
        }

        string publicDir = Path.Combine(rootDir, customTemplatePath, "public");
        Directory.CreateDirectory(publicDir);

        string cssPath = Path.Combine(publicDir, "main.css");
        string defaultCssOverrides = """
                                     /* GhostFx Custom DocFX Theme Overrides */
                                     """;

        if (!File.Exists(cssPath))
        {
            await File.WriteAllTextAsync(cssPath, defaultCssOverrides, System.Text.Encoding.UTF8);
        }
        else
        {
            string existingCss = await File.ReadAllTextAsync(cssPath);
            if (!existingCss.Contains(".responsive-header-img"))
            {
                await File.AppendAllTextAsync(cssPath, "\n\n" + defaultCssOverrides, System.Text.Encoding.UTF8);
            }
        }

        var links = iconLinks ??
        [
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
                catch
                {
                }
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
            else
            {
                mainContent = $$"""
                                export default {
                                  iconLinks: {{jsonLinks}}
                                }
                                """;
            }

            await File.WriteAllTextAsync(jsPath, mainContent, System.Text.Encoding.UTF8);
        }
        else
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonLinks = JsonSerializer.Serialize(links, options);

            string defaultJs = $$"""
                                 export default {
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

    public static List<GhostNavItem> ParseTocYml(string rootDir)
    {
        var items = new List<GhostNavItem>();
        string tocPath = Path.Combine(rootDir, "toc.yml");
        if (!File.Exists(tocPath)) return items;

        try
        {
            string yaml = File.ReadAllText(tocPath);
            string? currentName = null;
            string? currentHref = null;

            foreach (var rawLine in yaml.Replace("\r", "").Split('\n'))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                if (line.StartsWith("- name:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(currentName))
                    {
                        items.Add(new GhostNavItem
                        {
                            Label = currentName,
                            Url = currentHref ?? $"{currentName.ToLowerInvariant().Replace(" ", "-")}.html"
                        });
                        currentHref = null;
                    }

                    int idx = line.IndexOf(':');
                    currentName = idx >= 0 && idx < line.Length - 1
                        ? line.Substring(idx + 1).Trim(' ', '\t', '"', '\'')
                        : string.Empty;
                }
                else if (line.StartsWith("uid:", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = line.IndexOf(':');
                    if (idx >= 0 && idx < line.Length - 1)
                    {
                        string uid = line.Substring(idx + 1).Trim(' ', '\t', '"', '\'');
                        currentHref = uid.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ? uid : uid + ".html";
                    }
                }
                else if (line.StartsWith("href:", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = line.IndexOf(':');
                    if (idx >= 0 && idx < line.Length - 1)
                    {
                        string href = line.Substring(idx + 1).Trim(' ', '\t', '"', '\'');
                        currentHref = href.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                            ? href.Substring(0, href.Length - 3) + ".html"
                            : href;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentName))
            {
                items.Add(new GhostNavItem
                {
                    Label = currentName, Url = currentHref ?? $"{currentName.ToLowerInvariant().Replace(" ", "-")}.html"
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ParseTocYml Error] {ex.Message}");
        }

        return items;
    }

    public static string GenerateSiteNavPartialContent(List<GhostNavItem>? navItems = null,
        List<BlogPostMetadata>? pages = null, List<BlogPostMetadata>? posts = null, List<IconLink>? iconLinks = null,
        string? rootDir = null)
    {
        var sbNav = new System.Text.StringBuilder();

        if ((navItems == null || navItems.Count == 0) && !string.IsNullOrEmpty(rootDir))
        {
            navItems = ParseTocYml(rootDir);
        }

        if ((navItems == null || navItems.Count == 0) && pages != null && pages.Count > 0)
        {
            navItems = pages.Select(p => new GhostNavItem
            {
                Label = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(p.Slug.Replace("-", " ")
                    .Replace("_", " ")),
                Url = $"{p.Slug}.html"
            }).ToList();
        }

        if (navItems != null && navItems.Count > 0)
        {
            foreach (var nav in navItems)
            {
                if (string.IsNullOrWhiteSpace(nav.Label)) continue;
                string label = nav.Label;
                string url = nav.Url?.Trim() ?? "";
                string slug = label.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");

                string href;
                if (string.IsNullOrWhiteSpace(url) || url == "/" ||
                    url.Equals("home", StringComparison.OrdinalIgnoreCase))
                {
                    href = "{{_rel}}index.html";
                }
                else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    href = url;
                }
                else
                {
                    string pathSlug = url.Trim('/').Split('/').LastOrDefault() ?? "";
                    if (string.IsNullOrEmpty(pathSlug) ||
                        pathSlug.Equals("index.md", StringComparison.OrdinalIgnoreCase) ||
                        pathSlug.Equals("index.html", StringComparison.OrdinalIgnoreCase))
                    {
                        href = "{{_rel}}index.html";
                    }
                    else if (pathSlug.EndsWith("toc.yml", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] parts = url.Trim('/').Split('/');
                        string dir = parts.Length > 1 ? parts[0] : "";
                        if (dir.Equals("published", StringComparison.OrdinalIgnoreCase) ||
                            dir.Equals("posts", StringComparison.OrdinalIgnoreCase))
                        {
                            href = "{{_rel}}blog.html";
                        }
                        else if (!string.IsNullOrEmpty(dir))
                        {
                            href = "{{_rel}}" + dir + ".html";
                        }
                        else
                        {
                            href = "{{_rel}}index.html";
                        }
                    }
                    else if (pathSlug.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    {
                        href = "{{_rel}}" + pathSlug;
                    }
                    else if (pathSlug.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        href = "{{_rel}}" + pathSlug.Substring(0, pathSlug.Length - 3) + ".html";
                    }
                    else
                    {
                        href = "{{_rel}}" + pathSlug + ".html";
                    }
                }

                sbNav.AppendLine(
                    $"            <li class=\"nav-{slug}\" role=\"menuitem\"><a href=\"{href}\">{label}</a></li>");
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
                    "twitter" or "x" => "{{>partials/icons/x}}",
                    "bluesky" or "bsky" => "{{>partials/icons/bluesky}}",
                    "mastodon" or "masto" => "{{>partials/icons/mastodon}}",
                    "linkedin" => "{{>partials/icons/linkedin}}",
                    "youtube" => "{{>partials/icons/youtube}}",
                    "reddit" => "{{>partials/icons/reddit}}",
                    "rss" => "{{>partials/icons/rss}}",
                    "email" or "mail" => "{{>partials/icons/email}}",
                    "github" => "{{>partials/icons/github}}",
                    _ => link.Title ?? link.Icon ?? ""
                };
                sbSocial.AppendLine(
                    $"            <a class=\"social-link social-link-{iconLower}\" href=\"{link.Href}\" title=\"{link.Title}\" target=\"_blank\" rel=\"noreferrer noopener\">{iconPartial}</a>");
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<nav class=\"site-nav\">");
        sb.AppendLine("    <div class=\"site-nav-left\">");
        sb.AppendLine(
            "        <button class=\"site-nav-hamburger\" aria-label=\"Toggle menu\" onclick=\"toggleMobileNav();return false;\"><svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"currentColor\"><path d=\"M3 6h18v2H3V6zm0 5h18v2H3v-2zm0 5h18v2H3v-2z\"/></svg></button>");
        sb.AppendLine("        {{^isHome}}");
        sb.AppendLine("            {{#_appLogoPath}}");
        sb.AppendLine(
            "                <a class=\"site-nav-logo\" href=\"{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}\"><img src=\"{{_rel}}{{_appLogoPath}}\" alt=\"{{_appTitle}}\" /></a>");
        sb.AppendLine("            {{/_appLogoPath}}");
        sb.AppendLine("            {{^_appLogoPath}}");
        sb.AppendLine(
            "                <a class=\"site-nav-logo\" href=\"{{#_appUrl}}{{_appUrl}}{{/_appUrl}}{{^_appUrl}}{{_rel}}{{/_appUrl}}\">{{_appTitle}}</a>");
        sb.AppendLine("            {{/_appLogoPath}}");
        sb.AppendLine("        {{/isHome}}");
        sb.AppendLine("        <ul class=\"nav site-nav-menu\" role=\"menu\">");
        sb.Append(sbNav.ToString());
        sb.AppendLine("        </ul>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    {{#_enableSearch}}");
        sb.AppendLine("    <div class=\"site-nav-center\">");
        sb.AppendLine("        <form class=\"search\" role=\"search\" id=\"search\">");
        sb.AppendLine(
            "            <input class=\"form-control\" id=\"search-query\" type=\"search\" disabled placeholder=\"Search\" autocomplete=\"off\" aria-label=\"Search\">");
        sb.AppendLine("        </form>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    {{/_enableSearch}}");
        sb.AppendLine("    <div class=\"site-nav-right\">");
        sb.AppendLine("        <div class=\"social-links\">");
        sb.Append(sbSocial.ToString());
        sb.AppendLine("        </div>");
        sb.AppendLine("        {{#_appUrl}}");
        sb.AppendLine(
            "        <a class=\"rss-button\" href=\"https://feedly.com/i/subscription/feed/{{_appUrl}}/rss/\" title=\"RSS\" target=\"_blank\" rel=\"noreferrer noopener\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"currentColor\"><circle cx=\"6.18\" cy=\"17.82\" r=\"2.18\"/><path d=\"M4 4.44v2.83c7.03 0 12.73 5.7 12.73 12.73h2.83c0-8.59-6.97-15.56-15.56-15.56zm0 5.66v2.83c3.9 0 7.07 3.17 7.07 7.07h2.83c0-5.47-4.43-9.9-9.9-9.9z\"/></svg></a>");
        sb.AppendLine("        {{/_appUrl}}");
        sb.AppendLine(
            "        <div class=\"dropdown theme-dropdown\" style=\"display: inline-block; margin-left: 10px;\">");
        sb.AppendLine(
            "            <a title=\"Change theme\" class=\"btn border-0 dropdown-toggle\" data-bs-toggle=\"dropdown\" aria-expanded=\"false\" style=\"color: #fff; text-decoration: none; padding: 0 5px;\">");
        sb.AppendLine("                <i class=\"bi bi-circle-half\" style=\"font-size: 1.6rem;\"></i>");
        sb.AppendLine("            </a>");
        sb.AppendLine("            <ul class=\"dropdown-menu dropdown-menu-end\">");
        sb.AppendLine(
            "                <li><a class=\"dropdown-item\" href=\"#\" onclick=\"setTheme('light');return false;\"><i class=\"bi bi-sun\"></i> Light</a></li>");
        sb.AppendLine(
            "                <li><a class=\"dropdown-item\" href=\"#\" onclick=\"setTheme('dark');return false;\"><i class=\"bi bi-moon\"></i> Dark</a></li>");
        sb.AppendLine(
            "                <li><a class=\"dropdown-item\" href=\"#\" onclick=\"setTheme('auto');return false;\"><i class=\"bi bi-circle-half\"></i> Auto</a></li>");
        sb.AppendLine("            </ul>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <script>");
        sb.AppendLine("          window.toggleMobileNav = function() {");
        sb.AppendLine("            var menu = document.querySelector('.site-nav-menu');");
        sb.AppendLine("            if (menu) { menu.classList.toggle('mobile-menu-active'); }");
        sb.AppendLine("          };");
        sb.AppendLine("          document.addEventListener('click', function(e) {");
        sb.AppendLine("            var menu = document.querySelector('.site-nav-menu');");
        sb.AppendLine("            var btn = document.querySelector('.site-nav-hamburger');");
        sb.AppendLine(
            "            if (menu && menu.classList.contains('mobile-menu-active') && !menu.contains(e.target) && (!btn || !btn.contains(e.target))) {");
        sb.AppendLine("              menu.classList.remove('mobile-menu-active');");
        sb.AppendLine("            }");
        sb.AppendLine("          });");
        sb.AppendLine("          function setTheme(t) {");
        sb.AppendLine("            localStorage.setItem('theme', t);");
        sb.AppendLine(
            "            var eff = t === 'auto' ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : t;");
        sb.AppendLine("            document.documentElement.setAttribute('data-bs-theme', eff);");
        sb.AppendLine("            var icon = document.querySelector('.dropdown-toggle i.bi');");
        sb.AppendLine(
            "            if (icon) { icon.className = 'bi ' + (t === 'light' ? 'bi-sun' : t === 'dark' ? 'bi-moon' : 'bi-circle-half'); }");
        sb.AppendLine("          }");
        sb.AppendLine("          (function() {");
        sb.AppendLine("            var t = localStorage.getItem('theme');");
        sb.AppendLine(
            "            if (!t) { t = 'auto'; try { localStorage.setItem('theme', 'auto'); } catch (e) {} }");
        sb.AppendLine(
            "            var eff = t === 'auto' ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : t;");
        sb.AppendLine("            document.documentElement.setAttribute('data-bs-theme', eff);");
        sb.AppendLine("            var icon = document.querySelector('.dropdown-toggle i.bi');");
        sb.AppendLine(
            "            if (icon) { icon.className = 'bi ' + (t === 'light' ? 'bi-sun' : t === 'dark' ? 'bi-moon' : 'bi-circle-half'); }");
        sb.AppendLine("          })();");
        sb.AppendLine("        </script>");
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
            catch
            {
            }
        }

        foreach (var dir in Directory.GetDirectories(path))
        {
            PurgeDirectory(dir);
            try
            {
                Directory.Delete(dir);
            }
            catch
            {
            }
        }
    }
}
