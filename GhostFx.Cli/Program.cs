using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GhostFx.Core;

namespace GhostFx.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configOption = new Option<FileInfo?>(
            name: "--config",
            description: "Path to a ghostfx.json configuration file. (Defaults to 'ghostfx.json' in current directory if omitted).");

        var urlOption = new Option<string?>(
            name: "--url",
            description: "The live Ghost blog base URL.");

        var adminApiKeyOption = new Option<string?>(
            aliases: ["--admin-api-key", "--api-key"],
            description: "The Ghost Admin API key (Format: ID:SECRET).");

        var inputOption = new Option<string?>(
            name: "--input",
            description: "Path to a local Ghost JSON export file.");

        var outputOption = new Option<string?>(
            name: "--output",
            description: "The target directory to save Markdown posts. (Defaults to 'articles').");

        var indexOption = new Option<string?>(
            name: "--index-file",
            description: "The file path to output your dynamic front page. (Defaults to 'index.md').");

        var siteTitleOption = new Option<string?>(
            name: "--site-title",
            description: "The title of your static site.");

        var includeDraftsOption = new Option<bool?>(
            name: "--include-drafts",
            description: "If true, retrieves and processes draft status files.");

        var downloadThemeOption = new Option<bool?>(
            name: "--download-theme",
            description: "If true, downloads and extracts the active theme zip.");

        var themePathOption = new Option<string?>(
            name: "--theme-path",
            description: "Path to a theme zip archive or extracted theme folder.");

        var quietOption = new Option<bool>(
            aliases: ["--quiet", "-q"],
            description: "Operate in quiet mode without printing banners or progress animations.");

        var logoPathOption = new Option<bool?>(
            name: "--logo-path",
            description: "If true, generates _appLogoPath in docfx.json. (Defaults to true).");

        var yesOption = new Option<bool>(
            aliases: ["--yes", "-y"],
            description: "Automatically confirm and proceed with migration without interactive prompt.");

        var gaTagOption = new Option<string?>(
            name: "--ga-tag",
            description: "Google Analytics Tag ID to inject into docfx.json.");

        var cleanUrlsOption = new Option<bool?>(
            name: "--clean-urls",
            description: "If true, generates {slug}/index.md for extension-less URLs.");


        var migrateThemeOption = new Option<bool?>(
            name: "--migrate-theme",
            description: "If true, migrates and converts the Ghost theme/template. (Defaults to true).");

        var contentApiKeyOption = new Option<string?>(
            name: "--content-api-key",
            description: "The Ghost Content API key.");

        var purgeTemplateOption = new Option<bool?>(
            name: "--purge-template",
            description: "If true, purges the template folder. If false, skips purging.");

        var rootCommand = new RootCommand("GhostFx: Live-migrate from Ghost to DocFx.")
        {
            configOption,
            urlOption,
            adminApiKeyOption,
            inputOption,
            outputOption,
            indexOption,
            siteTitleOption,
            includeDraftsOption,
            downloadThemeOption,
            themePathOption,
            quietOption,
            logoPathOption,
            yesOption,
            gaTagOption,
            cleanUrlsOption,
            migrateThemeOption,
            contentApiKeyOption,
            purgeTemplateOption
        };

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            var parseResult = context.ParseResult;
            var configFile = parseResult.GetValueForOption(configOption);
            var url = parseResult.GetValueForOption(urlOption);
            var adminApiKey = parseResult.GetValueForOption(adminApiKeyOption);
            var input = parseResult.GetValueForOption(inputOption);
            var output = parseResult.GetValueForOption(outputOption);
            var indexFile = parseResult.GetValueForOption(indexOption);
            var siteTitle = parseResult.GetValueForOption(siteTitleOption);
            var includeDrafts = parseResult.GetValueForOption(includeDraftsOption);
            var downloadTheme = parseResult.GetValueForOption(downloadThemeOption);
            var themePath = parseResult.GetValueForOption(themePathOption);
            var quietCli = parseResult.GetValueForOption(quietOption);
            var logoPathCli = parseResult.GetValueForOption(logoPathOption);
            var autoConfirm = parseResult.GetValueForOption(yesOption);
            var gaTag = parseResult.GetValueForOption(gaTagOption);
            var cleanUrlsCli = parseResult.GetValueForOption(cleanUrlsOption);
            var migrateThemeCli = parseResult.GetValueForOption(migrateThemeOption);
            var contentApiKeyCli = parseResult.GetValueForOption(contentApiKeyOption);
            var purgeTemplateCli = parseResult.GetValueForOption(purgeTemplateOption);

            string? tempPipedFile = null;

            try
            {
                if (configFile == null)
                {
                    var defaultFile = new FileInfo("ghostfx.json");
                    if (defaultFile.Exists)
                    {
                        configFile = defaultFile;
                    }
                }

                var config = new GhostFxConfig();

                if (configFile != null && configFile.Exists)
                {
                    try
                    {
                        string jsonString = await File.ReadAllTextAsync(configFile.FullName);
                        var parsedConfig = JsonSerializer.Deserialize<GhostFxConfig>(jsonString, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (parsedConfig != null)
                        {
                            config = parsedConfig;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[ERROR] Reading config file: {ex.Message}");
                        Environment.ExitCode = 1;
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(url)) config.GhostUrl = url;
                if (!string.IsNullOrWhiteSpace(adminApiKey)) config.AdminApiKey = adminApiKey;
                if (!string.IsNullOrWhiteSpace(input)) config.GhostExportJson = input;
                if (!string.IsNullOrWhiteSpace(output)) config.OutputDir = output;
                if (!string.IsNullOrWhiteSpace(indexFile)) config.IndexFile = indexFile;
                if (!string.IsNullOrWhiteSpace(siteTitle)) config.SiteTitle = siteTitle;
                if (includeDrafts.HasValue) config.IncludeDrafts = includeDrafts.Value;
                if (downloadTheme.HasValue) config.DownloadTheme = downloadTheme.Value;
                if (cleanUrlsCli.HasValue) config.CleanUrls = cleanUrlsCli.Value;
                if (!string.IsNullOrWhiteSpace(themePath)) config.ThemePath = themePath;
                if (quietCli) config.Quiet = true;
                if (logoPathCli.HasValue) config.LogoPath = logoPathCli.Value;
                if (!string.IsNullOrWhiteSpace(gaTag)) config.GoogleAnalyticsTag = gaTag;
                if (migrateThemeCli.HasValue) config.MigrateTheme = migrateThemeCli.Value;
                if (!string.IsNullOrWhiteSpace(contentApiKeyCli)) config.ContentApiKey = contentApiKeyCli;
                if (purgeTemplateCli.HasValue) config.PurgeTemplate = purgeTemplateCli.Value;

                bool isQuiet = config.Quiet || Console.IsOutputRedirected;
                if (Console.IsInputRedirected)
                {
                    autoConfirm = true;
                    string pipedInput = await Console.In.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(pipedInput))
                    {
                        try
                        {
                            var parsedConfig = JsonSerializer.Deserialize<GhostFxConfig>(pipedInput, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (parsedConfig != null && (!string.IsNullOrWhiteSpace(parsedConfig.GhostUrl) || !string.IsNullOrWhiteSpace(parsedConfig.GhostExportJson)))
                            {
                                if (!string.IsNullOrWhiteSpace(parsedConfig.GhostUrl)) config.GhostUrl = parsedConfig.GhostUrl;
                                if (!string.IsNullOrWhiteSpace(parsedConfig.AdminApiKey)) config.AdminApiKey = parsedConfig.AdminApiKey;
                                if (!string.IsNullOrWhiteSpace(parsedConfig.GhostExportJson)) config.GhostExportJson = parsedConfig.GhostExportJson;
                                if (!string.IsNullOrWhiteSpace(parsedConfig.OutputDir)) config.OutputDir = parsedConfig.OutputDir;
                                if (!string.IsNullOrWhiteSpace(parsedConfig.IndexFile)) config.IndexFile = parsedConfig.IndexFile;
                                if (!string.IsNullOrWhiteSpace(parsedConfig.SiteTitle)) config.SiteTitle = parsedConfig.SiteTitle;
                                config.IncludeDrafts = parsedConfig.IncludeDrafts;
                                config.CleanUrls = parsedConfig.CleanUrls;
                                config.DownloadTheme = parsedConfig.DownloadTheme;
                                if (!string.IsNullOrWhiteSpace(parsedConfig.ThemePath)) config.ThemePath = parsedConfig.ThemePath;
                                if (parsedConfig.Quiet) config.Quiet = true;
                                config.LogoPath = parsedConfig.LogoPath;
                                config.MigrateTheme = parsedConfig.MigrateTheme;
                                if (!string.IsNullOrWhiteSpace(parsedConfig.GoogleAnalyticsTag)) config.GoogleAnalyticsTag = parsedConfig.GoogleAnalyticsTag;
                                if (!string.IsNullOrWhiteSpace(parsedConfig.ContentApiKey)) config.ContentApiKey = parsedConfig.ContentApiKey;
                                if (parsedConfig.PurgeTemplate.HasValue) config.PurgeTemplate = parsedConfig.PurgeTemplate.Value;
                            }
                            else
                            {
                                string tempPath = Path.Combine(Path.GetTempPath(), $"ghostfx_piped_{Guid.NewGuid():N}.json");
                                await File.WriteAllTextAsync(tempPath, pipedInput);
                                config.GhostExportJson = tempPath;
                                tempPipedFile = tempPath;
                            }
                        }
                        catch
                        {
                            string tempPath = Path.Combine(Path.GetTempPath(), $"ghostfx_piped_{Guid.NewGuid():N}.json");
                            await File.WriteAllTextAsync(tempPath, pipedInput);
                            config.GhostExportJson = tempPath;
                            tempPipedFile = tempPath;
                        }
                    }
                }

                if (configFile != null && configFile.Exists && !isQuiet)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[INFO] Loading configuration from: {configFile.FullName}");
                    Console.ResetColor();
                }

                bool hasApiCreds = !string.IsNullOrWhiteSpace(config.GhostUrl) && (!string.IsNullOrWhiteSpace(config.AdminApiKey) || !string.IsNullOrWhiteSpace(config.ContentApiKey));
                bool hasInputFile = !string.IsNullOrWhiteSpace(config.GhostExportJson) && File.Exists(config.GhostExportJson);

                if (!hasApiCreds && !hasInputFile)
                {
                    Console.Error.WriteLine("==========================================================================");
                    Console.Error.WriteLine(" Welcome to GhostFx - Ghost to DocFx Migration Suite");
                    Console.Error.WriteLine("==========================================================================");
                    Console.Error.WriteLine("Missing Credentials or Input File: Your Ghost details could not be resolved.");
                    Console.Error.WriteLine("To use GhostFx, please do one of the following:");
                    Console.Error.WriteLine("  1. Create a 'ghostfx.json' configuration file in the current directory.");
                    Console.Error.WriteLine("  2. Pass a custom configuration file path using: --config <path>");
                    Console.Error.WriteLine("  3. Specify live Ghost details: --url <url> --api-key <key>");
                    Console.Error.WriteLine("  4. Specify a local JSON export file: --input <path-to-ghost-export.json>");
                    Console.Error.WriteLine("  5. Pipe a JSON export file into stdin.");
                    Environment.ExitCode = 1;
                    return;
                }

                if (!isQuiet)
                {
                    // Display Migration Plan Overview
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("==========================================================================");
                    Console.WriteLine($" GhostFx Migration Plan: {config.SiteTitle}");
                    Console.WriteLine("==========================================================================");
                    Console.ResetColor();

                    if (hasApiCreds)
                    {
                        Console.WriteLine($"  [Source]          Live Ghost API ({config.GhostUrl})");
                    }
                    else
                    {
                        Console.WriteLine($"  [Source]          Local JSON Export ({config.GhostExportJson})");
                    }

                    Console.WriteLine($"  [Site Title]      {config.SiteTitle}");
                    Console.WriteLine($"  [Output Directory]{config.OutputDir}/");
                    Console.WriteLine($"  [Index File]      {config.IndexFile}");
                    Console.WriteLine($"  [Include Drafts]  {config.IncludeDrafts}");
                    Console.WriteLine($"  [Download Theme]  {config.DownloadTheme}");
                    Console.WriteLine($"  [Purge Template]  {config.MigrateTheme}");
                    Console.WriteLine($"  [Migrate Theme]   {config.MigrateTheme}");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("==========================================================================");
                    Console.ResetColor();
                    Console.WriteLine();

                    // Check if theme zip already exists on disk
                    if (config.DownloadTheme && (File.Exists(config.ThemePath) || Directory.Exists(config.ThemePath)))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[INFO] Found existing theme at: {config.ThemePath}");
                        Console.ResetColor();
                    }

                    // Interactive Confirmation Prompt
                    if (!autoConfirm && !Console.IsInputRedirected)
                    {
                        Console.Write("Do you want to proceed with this migration? [Y/n]: ");
                        string? response = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(response) && (response.StartsWith("n", StringComparison.OrdinalIgnoreCase) || response.StartsWith("no", StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[INFO] Migration cancelled by user.");
                            Console.ResetColor();
                            return;
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[INFO] Starting migration engine for: {config.SiteTitle}...");
                    Console.ResetColor();
                }

                var engine = new MigrationEngine();
                var result = await engine.ExecuteAsync(
                    config,
                    onProgress: isQuiet ? null : (current, total, item) =>
                    {
                        AsciiProgressBar.Draw(current, total, item);
                    },
                    onManualThemeRequested: async (targetPath, version) =>
                    {
                        if (!isQuiet)
                        {
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            string verInfo = string.IsNullOrWhiteSpace(version) ? "" : $" ({version})";
                            Console.WriteLine($"[WARN] Automated theme download via Ghost API is unsupported by your Ghost host{verInfo}.");
                            Console.WriteLine($"      You can manually export/download your theme zip from Ghost Admin and save it to: {targetPath}");
                            Console.ResetColor();
                        }

                        if (Console.IsInputRedirected || autoConfirm || isQuiet)
                        {
                            return false;
                        }

                        Console.Write($"\nHave you placed the exported theme zip file at '{targetPath}'? [y/N]: ");
                        string? inputStr = Console.ReadLine()?.Trim();
                        bool confirmed = !string.IsNullOrEmpty(inputStr) && (inputStr.Equals("y", StringComparison.OrdinalIgnoreCase) || inputStr.Equals("yes", StringComparison.OrdinalIgnoreCase));

                        if (confirmed && (File.Exists(targetPath) || Directory.Exists(targetPath)))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[INFO] Custom theme detected at '{targetPath}'. Continuing migration...");
                            Console.ResetColor();
                            return true;
                        }

                        return false;
                    },
                    onConfirmTemplatePurge: async (targetPath) =>
                    {
                        if (config.PurgeTemplate.HasValue)
                        {
                            return config.PurgeTemplate.Value;
                        }

                        if (Console.IsInputRedirected || autoConfirm || isQuiet)
                        {
                            return true; // Auto-confirm in non-interactive/silent modes
                        }

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"\n[WARN] The template override directory at '{targetPath}' already exists.");
                        Console.ResetColor();
                        Console.Write("\nAre you sure you want to purge and start over with a completely empty template? [Y/n]: ");
                        string? inputStr = Console.ReadLine()?.Trim();
                        bool confirmed = string.IsNullOrEmpty(inputStr) || inputStr.Equals("y", StringComparison.OrdinalIgnoreCase) || inputStr.Equals("yes", StringComparison.OrdinalIgnoreCase);

                        if (!confirmed)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[INFO] Template directory purge declined. Skipping theme migration.");
                            Console.ResetColor();
                        }
                        return confirmed;
                    });

                if (!string.IsNullOrEmpty(result.ThemeDownloadWarning) && !isQuiet)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[WARN] {result.ThemeDownloadWarning}");
                    Console.ResetColor();
                }

                if (result.Success)
                {
                    Environment.ExitCode = 0;
                    if (!isQuiet)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n[SUCCESS] {result.Message}");
                        Console.ResetColor();
                        Console.WriteLine($"  Processed Posts:     {result.ProcessedPosts}");
                        Console.WriteLine($"  Processed Pages:     {result.ProcessedPages}");
                        Console.WriteLine($"  Processed Drafts:    {result.ProcessedDrafts}");
                        if (result.ProcessedScheduled > 0)
                        {
                            Console.WriteLine($"  Processed Scheduled: {result.ProcessedScheduled}");
                        }
                        Console.WriteLine($"  Processed Tags:      {result.ProcessedTags}");
                        Console.WriteLine($"  Generated Files:     {result.GeneratedFiles.Count}");
                        Console.WriteLine($"  Duration:            {result.ElapsedDuration.TotalSeconds:F2}s");
                    }
                }
                else
                {
                    Environment.ExitCode = 1;
                    Console.Error.WriteLine($"[ERROR] {result.Message}");
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPipedFile) && File.Exists(tempPipedFile))
                {
                    try { File.Delete(tempPipedFile); } catch { }
                }
            }
        });

        return await rootCommand.InvokeAsync(args);
    }
}
