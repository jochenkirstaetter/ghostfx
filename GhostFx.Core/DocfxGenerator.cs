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
        List<IconLink>? iconLinks = null)
    {
        await EnsureDocfxTemplateOverridesExistAsync(rootDir, customTemplatePath, iconLinks);

        string docfxPath = Path.Combine(rootDir, "docfx.json");

        string fullOutputDir = Path.GetFullPath(config.OutputDir);
        string fullRootDir = Path.GetFullPath(rootDir);

        string relOutputDir = Path.GetRelativePath(fullRootDir, fullOutputDir).Replace('\\', '/');
        if (relOutputDir == ".") relOutputDir = "";

        string articlesPattern = string.IsNullOrEmpty(relOutputDir) ? "**.md" : $"{relOutputDir}/**.md";
        string tocPattern = string.IsNullOrEmpty(relOutputDir) ? "**/toc.yml" : $"{relOutputDir}/**/toc.yml";
        string draftsPattern = string.IsNullOrEmpty(relOutputDir) ? "drafts/**.md" : $"{relOutputDir}/drafts/**.md";
        string tagsPattern = string.IsNullOrEmpty(relOutputDir) ? "tags/**.md" : $"{relOutputDir}/tags/**.md";

        string indexPath = config.IndexFile;
        if (!string.IsNullOrEmpty(relOutputDir) && !File.Exists(Path.Combine(fullRootDir, config.IndexFile)) && File.Exists(Path.Combine(fullOutputDir, config.IndexFile)))
        {
            indexPath = $"{relOutputDir}/{config.IndexFile}";
        }

        string? faviconPath = null;
        if (File.Exists(Path.Combine(fullRootDir, "favicon.png"))) faviconPath = "favicon.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.png"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.png" : $"{relOutputDir}/favicon.png";
        else if (File.Exists(Path.Combine(fullRootDir, "favicon.svg"))) faviconPath = "favicon.svg";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.svg"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.svg" : $"{relOutputDir}/favicon.svg";
        else if (File.Exists(Path.Combine(fullRootDir, "favicon.ico"))) faviconPath = "favicon.ico";
        else if (File.Exists(Path.Combine(fullOutputDir, "favicon.ico"))) faviconPath = string.IsNullOrEmpty(relOutputDir) ? "favicon.ico" : $"{relOutputDir}/favicon.ico";

        string? logoPath = null;
        if (File.Exists(Path.Combine(fullRootDir, "logo.png"))) logoPath = "logo.png";
        else if (File.Exists(Path.Combine(fullOutputDir, "logo.png"))) logoPath = string.IsNullOrEmpty(relOutputDir) ? "logo.png" : $"{relOutputDir}/logo.png";
        else if (File.Exists(Path.Combine(fullRootDir, "logo.svg"))) logoPath = "logo.svg";
        else if (File.Exists(Path.Combine(fullOutputDir, "logo.svg"))) logoPath = string.IsNullOrEmpty(relOutputDir) ? "logo.svg" : $"{relOutputDir}/logo.svg";
        else if (!string.IsNullOrEmpty(faviconPath)) logoPath = faviconPath;

        var docfxConfig = new
        {
            build = new
            {
                content = new object[]
                {
                    new
                    {
                        files = new string[]
                        {
                            articlesPattern,
                            draftsPattern,
                            tocPattern,
                            "toc.yml",
                            indexPath,
                            "tags.md",
                            tagsPattern
                        }
                    }
                },
                resource = new object[]
                {
                    new
                    {
                        files = new string[]
                        {
                            "images/**",
                            "media/**",
                            "**/images/**",
                            "**/media/**",
                            string.IsNullOrEmpty(relOutputDir) ? "images/**" : $"{relOutputDir}/images/**",
                            string.IsNullOrEmpty(relOutputDir) ? "media/**" : $"{relOutputDir}/media/**",
                            string.IsNullOrEmpty(relOutputDir) ? "**/images/**" : $"{relOutputDir}/**/images/**",
                            string.IsNullOrEmpty(relOutputDir) ? "**/media/**" : $"{relOutputDir}/**/media/**"
                        }
                    }
                },
                output = "_site",
                template = new string[]
                {
                    "default",
                    "modern",
                    Path.GetFileName(customTemplatePath)
                },
                globalMetadata = new
                {
                    _appTitle = config.SiteTitle,
                    _appName = config.SiteTitle,
                    _appFaviconPath = faviconPath ?? "favicon.png",
                    _appLogoPath = logoPath ?? faviconPath ?? "favicon.png",
                    _enableSearch = true,
                    _appFooter = $"<span>Generated by <a href='https://github.com/jochenkirstaetter/ghostfx'>GhostFx</a> for DocFx</span>"
                }
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

    public static async Task ConvertGhostThemeToDocfxTemplateAsync(string zipFilePath, string targetTemplateDir, string headerInjection = "", string footerInjection = "")
    {
        if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
            return;

        // DocFx modern template uses public/ directory (main.css and main.js) for layout overrides and styling
        string publicDir = Path.Combine(targetTemplateDir, "public");
        Directory.CreateDirectory(publicDir);

        // Remove legacy partials directory if existing
        string legacyPartialsDir = Path.Combine(targetTemplateDir, "partials");
        if (Directory.Exists(legacyPartialsDir))
        {
            try { Directory.Delete(legacyPartialsDir, true); } catch { }
        }

        string tempExtractPath = Path.Combine(Path.GetTempPath(), "GhostFx_Theme_" + Guid.NewGuid().ToString("N"));
        try
        {
            ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath, overwriteFiles: true);

            // 1. Process CSS assets and compile into public/main.css for DocFx modern template
            string mainCssPath = Path.Combine(publicDir, "main.css");
            using (var cssWriter = new StreamWriter(mainCssPath, append: false))
            {
                await cssWriter.WriteLineAsync("/* GhostFx Auto-Converted Ghost Theme CSS Override for DocFx Modern Template */");

                foreach (var cssFile in Directory.GetFiles(tempExtractPath, "*.css", SearchOption.AllDirectories))
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

                foreach (var jsFile in Directory.GetFiles(tempExtractPath, "*.js", SearchOption.AllDirectories))
                {
                    if (jsFile.Contains("node_modules")) continue;
                    string jsContent = await File.ReadAllTextAsync(jsFile);
                    await jsWriter.WriteLineAsync($"// Source: {Path.GetFileName(jsFile)}");
                    await jsWriter.WriteLineAsync(jsContent);
                }
            }

            // 3. Process Handlebars (.hbs) template files into converted styling / DOM wrappers
            foreach (var hbsFile in Directory.GetFiles(tempExtractPath, "*.hbs", SearchOption.AllDirectories))
            {
                string hbsContent = await File.ReadAllTextAsync(hbsFile);
                string converted = ConvertHandlebarsToDocfx(hbsContent);
                // Extracted template rules can be appended to main.js as layout helpers
                string hbsName = Path.GetFileNameWithoutExtension(hbsFile);
                string templateJsPath = Path.Combine(publicDir, $"{hbsName}.js");
                await File.WriteAllTextAsync(templateJsPath, $"// Converted Ghost Template ({hbsName})\n/*\n{converted}\n*/");
            }
        }
        finally
        {
            if (Directory.Exists(tempExtractPath))
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
        result = Regex.Replace(result, @"\{\{\s*@site\.description\s*\}\}", "{{_appDescription}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*@site\.url\s*\}\}", "{{_rel}}", RegexOptions.IgnoreCase);

        // Convert Post tags
        result = Regex.Replace(result, @"\{\{\s*title\s*\}\}", "{{title}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*content\s*\}\}", "{{{conceptual}}}", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*excerpt\s*\}\}", "{{summary}}", RegexOptions.IgnoreCase);

        // Convert Block Helpers {{#post}} ... {{/post}}
        result = Regex.Replace(result, @"\{\{\s*#post\s*\}\}", "<article class=\"ghost-post-container\">", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*/post\s*\}\}", "</article>", RegexOptions.IgnoreCase);

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
