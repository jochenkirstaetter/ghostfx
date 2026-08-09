using System.Text.Json.Serialization;

namespace GhostFx.Core;

public class GhostFxConfig
{
    [JsonPropertyName("ghostUrl")]
    public string GhostUrl { get; set; } = string.Empty;

    [JsonPropertyName("adminApiKey")]
    public string AdminApiKey { get; set; } = string.Empty;

    [JsonPropertyName("contentApiKey")]
    public string ContentApiKey { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey
    {
        get => AdminApiKey;
        set => AdminApiKey = value;
    }

    private string _ghostExportJson = string.Empty;

    [JsonPropertyName("ghostExportJson")]
    public string GhostExportJson
    {
        get => _ghostExportJson;
        set => _ghostExportJson = value;
    }

    [JsonPropertyName("inputJsonPath")]
    public string InputJsonPath
    {
        get => _ghostExportJson;
        set => _ghostExportJson = value;
    }

    [JsonPropertyName("outputDir")]
    public string OutputDir { get; set; } = "articles";

    [JsonPropertyName("indexFile")]
    public string IndexFile { get; set; } = "index.md";

    [JsonPropertyName("siteTitle")]
    public string SiteTitle { get; set; } = "My Static Blog";

    [JsonPropertyName("includeDrafts")]
    public bool IncludeDrafts { get; set; } = false;

    [JsonPropertyName("downloadTheme")]
    public bool DownloadTheme { get; set; } = false;

    [JsonPropertyName("quiet")]
    public bool Quiet { get; set; } = false;

    [JsonPropertyName("logoPath")]
    public bool LogoPath { get; set; } = true;

    [JsonPropertyName("googleAnalyticsTag")]
    public string GoogleAnalyticsTag { get; set; } = string.Empty;

    private string _themePath = "ghostfx.zip";

    [JsonPropertyName("themePath")]
    public string ThemePath
    {
        get => _themePath;
        set => _themePath = value;
    }

    [JsonPropertyName("themeOutputPath")]
    public string ThemeOutputPath
    {
        get => _themePath;
        set => _themePath = value;
    }

    [JsonPropertyName("migrateTheme")]
    public bool MigrateTheme { get; set; } = true;
}
