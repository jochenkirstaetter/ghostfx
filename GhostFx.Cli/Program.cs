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
            downloadThemeOption
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
                Console.WriteLine($"Loading configuration from: {configFile.FullName}");
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
                    Console.WriteLine($"Error reading config file: {ex.Message}");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(url)) config.GhostUrl = url;
            if (!string.IsNullOrWhiteSpace(apiKey)) config.AdminApiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(input)) config.InputJsonPath = input;
            if (!string.IsNullOrWhiteSpace(output)) config.OutputDir = output;
            if (!string.IsNullOrWhiteSpace(indexFile)) config.IndexFile = indexFile;
            if (!string.IsNullOrWhiteSpace(siteTitle)) config.SiteTitle = siteTitle;
            if (includeDrafts.HasValue) config.IncludeDrafts = includeDrafts.Value;
            if (downloadTheme.HasValue) config.DownloadTheme = downloadTheme.Value;

            bool hasApiCreds = !string.IsNullOrWhiteSpace(config.GhostUrl) && !string.IsNullOrWhiteSpace(config.AdminApiKey);
            bool hasInputFile = !string.IsNullOrWhiteSpace(config.InputJsonPath) && File.Exists(config.InputJsonPath);

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

            Console.WriteLine($"Starting migration for: {config.SiteTitle}");
            var engine = new MigrationEngine();
            var result = await engine.ExecuteAsync(config, onProgress: (current, total, item) =>
            {
                AsciiProgressBar.Draw(current, total, item);
            });

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[SUCCESS] {result.Message}");
                Console.ResetColor();
                Console.WriteLine($"Processed Posts: {result.ProcessedPosts}");
                Console.WriteLine($"Processed Drafts: {result.ProcessedDrafts}");
                Console.WriteLine($"Processed Tags: {result.ProcessedTags}");
                Console.WriteLine($"Generated Files: {result.GeneratedFiles.Count}");
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
