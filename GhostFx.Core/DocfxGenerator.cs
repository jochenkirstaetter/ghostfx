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
    public static async Task<string> GenerateDocfxJsonIfNotExistsAsync(
        string rootDir,
        GhostFxConfig config,
        string customTemplatePath = "template/ghostfx",
        string siteLocale = "en",
        List<IconLink>? iconLinks = null)
    {
        await EnsureDocfxTemplateOverridesExistAsync(rootDir, customTemplatePath, iconLinks);

        string docfxPath = Path.Combine(rootDir, "docfx.json");
        if (File.Exists(docfxPath))
        {
            return docfxPath;
        }

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

        var excludePatterns = new string[] { "_site/**", "**/_site/**" };

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
                exclude = new string[] { "published/**", "pages/**", "_site/**", "**/_site/**" }
            }
        };

        var globalMetadata = new Dictionary<string, object>
        {
            ["_appTitle"] = config.SiteTitle,
            ["_appName"] = config.SiteTitle,
            ["_appFaviconPath"] = faviconPath ?? "favicon.png",
            ["_enableSearch"] = true,
            ["_disableContribution"] = true,
            ["_disableBreadcrumb"] = true,
            ["_lang"] = string.IsNullOrWhiteSpace(siteLocale) ? "en" : siteLocale,
            ["_appFooter"] = $"<span>Generated by <a href='https://github.com/jochenkirstaetter/ghostfx'>GhostFx</a> for DocFx</span>"
        };

        if (config.LogoPath)
        {
            globalMetadata["_appLogoPath"] = logoPath ?? faviconPath ?? "favicon.png";
        }

        if (!string.IsNullOrWhiteSpace(config.GoogleAnalyticsTag))
        {
            globalMetadata["_googleAnalyticsTagId"] = config.GoogleAnalyticsTag;
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
                            "_site/**",
                            "**/_site/**"
                        }
                    }
                },
                output = "_site",
                template = config.MigrateTheme
                    ? new string[]
                    {
                        "default",
                        "modern",
                        Path.GetFileName(customTemplatePath)
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

    public static async Task ConvertGhostThemeToDocfxTemplateAsync(string themePath, string targetTemplateDir, string headerInjection = "", string footerInjection = "")
    {
        if (string.IsNullOrWhiteSpace(themePath))
            return;

        bool isZip = File.Exists(themePath) && (themePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || !Directory.Exists(themePath));
        bool isDirectory = Directory.Exists(themePath);

        if (!isZip && !isDirectory)
            return;

        // DocFx modern template uses public/ directory (main.css and main.js) for layout overrides and styling
        string publicDir = Path.Combine(targetTemplateDir, "public");
        Directory.CreateDirectory(publicDir);

        // Remove legacy target layout and templates, but preserve directory structure
        string targetLayoutsDir = Path.Combine(targetTemplateDir, "layout");
        if (Directory.Exists(targetLayoutsDir))
        {
            try { Directory.Delete(targetLayoutsDir, true); } catch { }
        }

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
            // 1. Process CSS assets and compile into public/main.css for DocFx modern template
            string mainCssPath = Path.Combine(publicDir, "main.css");
            using (var cssWriter = new StreamWriter(mainCssPath, append: false))
            {
                await cssWriter.WriteLineAsync("/* GhostFx Auto-Converted Ghost Theme CSS Override for DocFx Modern Template */");

                foreach (var cssFile in Directory.GetFiles(sourceDir, "*.css", SearchOption.AllDirectories))
                {
                    string cssContent = await File.ReadAllTextAsync(cssFile);
                    await cssWriter.WriteLineAsync($"/* Source: {Path.GetFileName(cssFile)} */");
                    await cssWriter.WriteLineAsync(cssContent);
                }
            }

            // 2. Process JS assets and Code Injections into public/main.js for DocFx modern template
            string mainJsPath = Path.Combine(publicDir, "main.js");
            using (var jsWriter = new StreamWriter(mainJsPath, append: false))
            {
                await jsWriter.WriteLineAsync("// GhostFx Auto-Converted Ghost Theme JS Override for DocFx Modern Template");
                await jsWriter.WriteLineAsync("document.addEventListener('DOMContentLoaded', () => {");
                await jsWriter.WriteLineAsync("  console.log('[GhostFx] Modern theme overrides loaded.');");

                if (!string.IsNullOrWhiteSpace(headerInjection))
                {
                    await jsWriter.WriteLineAsync("  // Ghost Header Code Injection");
                    await jsWriter.WriteLineAsync($"  const headerContainer = document.createElement('div');");
                    await jsWriter.WriteLineAsync($"  headerContainer.innerHTML = {JsonSerializer.Serialize(headerInjection)};");
                    await jsWriter.WriteLineAsync("  document.head.appendChild(headerContainer);");
                }

                if (!string.IsNullOrWhiteSpace(footerInjection))
                {
                    await jsWriter.WriteLineAsync("  // Ghost Footer Code Injection");
                    await jsWriter.WriteLineAsync($"  const footerContainer = document.createElement('div');");
                    await jsWriter.WriteLineAsync($"  footerContainer.innerHTML = {JsonSerializer.Serialize(footerInjection)};");
                    await jsWriter.WriteLineAsync("  document.body.appendChild(footerContainer);");
                }

                await jsWriter.WriteLineAsync("});");

                foreach (var jsFile in Directory.GetFiles(sourceDir, "*.js", SearchOption.AllDirectories))
                {
                    if (jsFile.Contains("node_modules")) continue;
                    string jsContent = await File.ReadAllTextAsync(jsFile);
                    await jsWriter.WriteLineAsync($"// Source: {Path.GetFileName(jsFile)}");
                    await jsWriter.WriteLineAsync(jsContent);
                }
            }

            // 3. Process Handlebars (.hbs) template files into converted DocFX Mustache templates
            foreach (var hbsFile in Directory.GetFiles(sourceDir, "*.hbs", SearchOption.AllDirectories))
            {
                string hbsContent = await File.ReadAllTextAsync(hbsFile);
                string converted = ConvertHandlebarsToDocfx(hbsContent);
                string hbsName = Path.GetFileNameWithoutExtension(hbsFile);
                string hbsNameLower = hbsName.ToLowerInvariant();

                bool isPartial = hbsFile.Contains("/partials/", StringComparison.OrdinalIgnoreCase) || 
                                 hbsFile.Contains("\\partials\\", StringComparison.OrdinalIgnoreCase) ||
                                 hbsFile.Contains("/partials\\", StringComparison.OrdinalIgnoreCase) ||
                                 hbsFile.Contains("\\partials/", StringComparison.OrdinalIgnoreCase) ||
                                 Path.GetDirectoryName(hbsFile)?.EndsWith("partials", StringComparison.OrdinalIgnoreCase) == true;

                string targetPath;

                if (isPartial)
                {
                    int partialsIdx = hbsFile.IndexOf("partials", StringComparison.OrdinalIgnoreCase);
                    string relativePart = hbsFile.Substring(partialsIdx + 8).TrimStart('/', '\\');
                    string relName = Path.ChangeExtension(relativePart, ".tmpl");
                    targetPath = Path.Combine(targetTemplateDir, "partials", relName);
                }
                else
                {
                    if (hbsNameLower == "default")
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
                    else
                    {
                        targetPath = Path.Combine(targetTemplateDir, $"{hbsNameLower}.html.primary.tmpl");
                    }
                }

                string? parentDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                await File.WriteAllTextAsync(targetPath, converted);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempExtractPath) && Directory.Exists(tempExtractPath))
            {
                try { Directory.Delete(tempExtractPath, true); } catch { }
            }
        }
    }

    public static string ConvertHandlebarsToDocfx(string hbsContent)
    {
        if (string.IsNullOrWhiteSpace(hbsContent))
            return string.Empty;

        string result = hbsContent;

        // Convert Ghost Head & Foot
        result = Regex.Replace(result, @"\{\{\s*ghost_head\s*\}\}", "<!-- DocFx Head Injection -->", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*ghost_foot\s*\}\}", "<!-- DocFx Foot Injection -->", RegexOptions.IgnoreCase);

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
        result = Regex.Replace(result, @"\{\{\s*@site\.url\s*\}\}", "{{_rel}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@blog\.url\s*\}\}", "{{_rel}}", RegexOptions.IgnoreCase);

        // Convert Post tags
        result = Regex.Replace(result, @"\{\{\s*title\s*\}\}", "{{title}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*content\s*\}\}", "{{{conceptual}}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*excerpt\s*\}\}", "{{summary}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*custom_excerpt\s*\}\}", "{{summary}}", RegexOptions.IgnoreCase);

        // Convert Block Helpers {{#post}} ... {{/post}}
        result = Regex.Replace(result, @"\{\{\s*#post\s*\}\}", "<article class=\"ghost-post-container\">", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*/post\s*\}\}", "</article>", RegexOptions.IgnoreCase);

        // Convert layouts references
        result = Regex.Replace(result, @"\{\{\!<\s*([^ }]+)\s*\}\}", m =>
        {
            string layoutName = m.Groups[1].Value.Trim().ToLowerInvariant();
            if (layoutName == "default")
                return "{{!master(layout/_master.tmpl)}}";
            return $"{{!master(layout/_{layoutName}.tmpl)}}";
        }, RegexOptions.IgnoreCase);

        // Convert comments
        result = Regex.Replace(result, @"\{\{\!--", "{{!", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"--\}\}", "}}", RegexOptions.IgnoreCase);

        // Convert loops & conditional blocks using a stack for matching tag names
        var tagStack = new System.Collections.Generic.Stack<string>();

        // We combine loop blocks (#foreach), has blocks (#has), and conditionals (#if, #unless)
        // using matching regex
        result = Regex.Replace(result, @"\{\{\s*(#foreach|#has|#if|#unless|\/foreach|\/has|\/if|\/unless)\s*([^ }]*)\s*\}\}", m =>
        {
            string marker = m.Groups[1].Value.ToLowerInvariant();
            string arg = m.Groups[2].Value.Trim();

            if (marker.StartsWith('#'))
            {
                string propertyName = arg;
                if (marker == "#foreach")
                {
                    propertyName = arg;
                    tagStack.Push(propertyName);
                    return $"{{#{propertyName}}}";
                }
                else if (marker == "#has")
                {
                    propertyName = "hasMultipleAuthors";
                    if (arg.Contains("author", StringComparison.OrdinalIgnoreCase))
                    {
                        propertyName = "hasMultipleAuthors";
                    }
                    tagStack.Push(propertyName);
                    return $"{{#{propertyName}}}";
                }
                else if (marker == "#if")
                {
                    tagStack.Push(propertyName);
                    return $"{{#{propertyName}}}";
                }
                else // #unless
                {
                    tagStack.Push(propertyName);
                    return $"{{^{propertyName}}}";
                }
            }
            else // closing tag
            {
                if (tagStack.Count > 0)
                {
                    return $"{{/{tagStack.Pop()}}}";
                }
                return "{{/posts}}"; // fallback
            }
        }, RegexOptions.IgnoreCase);

        // Convert partials
        result = Regex.Replace(result, @"\{\{>\s*""?([^"" }]+)""?\s*\}\}", m =>
        {
            string path = m.Groups[1].Value;
            if (path.StartsWith("partials/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("partials\\", StringComparison.OrdinalIgnoreCase))
            {
                return $"{{{{> {path}}}}}";
            }
            return $"{{{{> partials/{path}}}}}";
        }, RegexOptions.IgnoreCase);

        // Master layout content injection spot
        result = Regex.Replace(result, @"\{\{\{\s*body\s*\}\}\}", "{{!body}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*body\s*\}\}", "{{!body}}", RegexOptions.IgnoreCase);

        // Convert img_url custom helper references
        result = Regex.Replace(result, @"\{\{\s*img_url\s+""?([^"" }]+)""?(?:\s+[^}]+)?\s*\}\}", "{{$1}}", RegexOptions.IgnoreCase);

        return result;
    }

    public static async Task EnsureDocfxTemplateOverridesExistAsync(string rootDir, string customTemplatePath = "template/ghostfx", List<IconLink>? iconLinks = null)
    {
        string publicDir = Path.Combine(rootDir, customTemplatePath, "public");
        Directory.CreateDirectory(publicDir);

        string cssPath = Path.Combine(publicDir, "main.css");
        if (!File.Exists(cssPath))
        {
            string defaultCss = """
            /* GhostFx Auto-Generated DocFx Theme Overrides */
            :root {
                --docfx-primary: #15171a;
                --docfx-accent: #30b1ff;
            }

            .ghost-post-container {
                max-width: 840px;
                margin: 0 auto;
                padding: 1.5rem 0;
            }
            """;
            await File.WriteAllTextAsync(cssPath, defaultCss);
        }

        var links = iconLinks ?? [
            new IconLink { Icon = "github", Href = "https://github.com/jochenkirstaetter/ghostfx", Title = "GitHub" }
        ];

        string jsPath = Path.Combine(publicDir, "main.js");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonLinks = JsonSerializer.Serialize(links, options);

        string defaultJs = $$"""
        export default {
          iconLinks: {{jsonLinks}}
        }
        """;
        await File.WriteAllTextAsync(jsPath, defaultJs);
    }
}
