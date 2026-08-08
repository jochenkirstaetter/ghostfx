using System.Text.Json.Serialization;

namespace GhostFx.Core;

public class GhostFxConfig
{
    [JsonPropertyName("ghostUrl")]
    public string GhostUrl { get; set; } = string.Empty;

    [JsonPropertyName("adminApiKey")]
    public string AdminApiKey { get; set; } = string.Empty;

    [JsonPropertyName("inputJsonPath")]
    public string InputJsonPath { get; set; } = string.Empty;

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

    [JsonPropertyName("themeOutputPath")]
    public string ThemeOutputPath { get; set; } = "ghostfx.zip";
}
