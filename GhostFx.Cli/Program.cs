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

        var apiKeyOption = new Option<string?>(
            name: "--api-key",
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

        var yesOption = new Option<bool>(
            aliases: ["--yes", "-y"],
            description: "Automatically confirm and proceed with migration without interactive prompt.");

        var rootCommand = new RootCommand("GhostFx: Live-migrate from Ghost to DocFx.")
        {
            configOption,
            urlOption,
            apiKeyOption,
            inputOption,
            outputOption,
            indexOption,
            siteTitleOption,
            includeDraftsOption,
            downloadThemeOption,
            themePathOption,
            yesOption
        };

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            var parseResult = context.ParseResult;
            var configFile = parseResult.GetValueForOption(configOption);
            var url = parseResult.GetValueForOption(urlOption);
            var apiKey = parseResult.GetValueForOption(apiKeyOption);
            var input = parseResult.GetValueForOption(inputOption);
            var output = parseResult.GetValueForOption(outputOption);
            var indexFile = parseResult.GetValueForOption(indexOption);
            var siteTitle = parseResult.GetValueForOption(siteTitleOption);
            var includeDrafts = parseResult.GetValueForOption(includeDraftsOption);
            var downloadTheme = parseResult.GetValueForOption(downloadThemeOption);
            var themePath = parseResult.GetValueForOption(themePathOption);
            var autoConfirm = parseResult.GetValueForOption(yesOption);

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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[INFO] Loading configuration from: {configFile.FullName}");
                Console.ResetColor();
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Reading config file: {ex.Message}");
                    Console.ResetColor();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(url)) config.GhostUrl = url;
            if (!string.IsNullOrWhiteSpace(apiKey)) config.AdminApiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(input)) config.GhostExportJson = input;
            if (!string.IsNullOrWhiteSpace(output)) config.OutputDir = output;
            if (!string.IsNullOrWhiteSpace(indexFile)) config.IndexFile = indexFile;
            if (!string.IsNullOrWhiteSpace(siteTitle)) config.SiteTitle = siteTitle;
            if (includeDrafts.HasValue) config.IncludeDrafts = includeDrafts.Value;
            if (downloadTheme.HasValue) config.DownloadTheme = downloadTheme.Value;
            if (!string.IsNullOrWhiteSpace(themePath)) config.ThemePath = themePath;

            bool hasApiCreds = !string.IsNullOrWhiteSpace(config.GhostUrl) && !string.IsNullOrWhiteSpace(config.AdminApiKey);
            bool hasInputFile = !string.IsNullOrWhiteSpace(config.GhostExportJson) && File.Exists(config.GhostExportJson);

            if (!hasApiCreds && !hasInputFile)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("==========================================================================");
                Console.WriteLine(" Welcome to GhostFx - Ghost to DocFx Migration Suite");
                Console.WriteLine("==========================================================================");
                Console.WriteLine("Missing Credentials or Input File: Your Ghost details could not be resolved.");
                Console.WriteLine("To use GhostFx, please do one of the following:");
                Console.WriteLine("  1. Create a 'ghostfx.json' configuration file in the current directory.");
                Console.WriteLine("  2. Pass a custom configuration file path using: --config <path>");
                Console.WriteLine("  3. Specify live Ghost details: --url <url> --api-key <key>");
                Console.WriteLine("  4. Specify a local JSON export file: --input <path-to-ghost-export.json>");
                Console.ResetColor();
                Console.WriteLine();
                return;
            }

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
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("==========================================================================");
            Console.ResetColor();
            Console.WriteLine();

            // Check if theme zip already exists on disk
            if (config.DownloadTheme && File.Exists(config.ThemeOutputPath))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[INFO] Found existing theme zip archive at: {config.ThemeOutputPath}");
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

            var engine = new MigrationEngine();
            var result = await engine.ExecuteAsync(
                config,
                onProgress: (current, total, item) =>
                {
                    AsciiProgressBar.Draw(current, total, item);
                },
                onManualThemeRequested: async (targetPath, version) =>
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    string verInfo = string.IsNullOrWhiteSpace(version) ? "" : $" ({version})";
                    Console.WriteLine($"[WARN] Automated theme download via Ghost API is unsupported by your Ghost host{verInfo}.");
                    Console.WriteLine($"      You can manually export/download your theme zip from Ghost Admin:");

                    if (string.Equals(version, "v3", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"      Settings > Design > Installed Themes > Download");
                    }
                    else if (string.Equals(version, "v4", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(version, "v5", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(version, "v6", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"      Settings > Theme > Change theme > Advanced > Export theme");
                    }
                    else
                    {
                        Console.WriteLine($"      - Ghost v3:       Settings > Design > Installed Themes > Download");
                        Console.WriteLine($"      - Ghost v4/v5/v6: Settings > Theme > Change theme > Advanced > Export theme");
                    }

                    Console.WriteLine($"      and save the zip file to: {targetPath}");
                    Console.ResetColor();

                    if (Console.IsInputRedirected || autoConfirm)
                    {
                        return false;
                    }

                    Console.Write($"\nHave you placed the exported theme zip file at '{targetPath}'? [y/N]: ");
                    string? input = Console.ReadLine()?.Trim();
                    bool confirmed = !string.IsNullOrEmpty(input) && (input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase));

                    if (confirmed && File.Exists(targetPath))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[INFO] Theme zip detected at '{targetPath}'. Continuing migration with custom theme...");
                        Console.ResetColor();
                        return true;
                    }

                    return false;
                });

            if (!string.IsNullOrEmpty(result.ThemeDownloadWarning))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[WARN] {result.ThemeDownloadWarning}");
                Console.ResetColor();
            }

            if (result.Success)
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
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] {result.Message}");
                Console.ResetColor();
            }
        });

        return await rootCommand.InvokeAsync(args);
    }
}
