using System.Text.Json;
using GhostFx.Core;
using Xunit;

namespace GhostFx.Tests;

public class ConfigTests
{
    [Fact]
    public void GhostFxConfig_DefaultValues_AreCorrect()
    {
        var config = new GhostFxConfig();

        Assert.Equal(string.Empty, config.GhostUrl);
        Assert.Equal(string.Empty, config.AdminApiKey);
        Assert.Equal("articles", config.OutputDir);
        Assert.Equal("index.md", config.IndexFile);
        Assert.Equal("My Static Blog", config.SiteTitle);
        Assert.False(config.IncludeDrafts);
        Assert.False(config.DownloadTheme);
    }

    [Fact]
    public void GhostFxConfig_JsonDeserialization_WorksAsExpected()
    {
        string json = """
        {
            "ghostUrl": "https://example-ghost-blog.com",
            "adminApiKey": "640a1b2c3d4e5f6a7b8c9d0e:1234567890abcdef1234567890abcdef",
            "outputDir": "custom-articles",
            "indexFile": "home.md",
            "siteTitle": "My Custom Title",
            "includeDrafts": true,
            "downloadTheme": true,
            "themeOutputPath": "custom-theme.zip"
        }
        """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var config = JsonSerializer.Deserialize<GhostFxConfig>(json, options);

        Assert.NotNull(config);
        Assert.Equal("https://example-ghost-blog.com", config.GhostUrl);
        Assert.Equal("640a1b2c3d4e5f6a7b8c9d0e:1234567890abcdef1234567890abcdef", config.AdminApiKey);
        Assert.Equal("custom-articles", config.OutputDir);
        Assert.Equal("home.md", config.IndexFile);
        Assert.Equal("My Custom Title", config.SiteTitle);
        Assert.True(config.IncludeDrafts);
        Assert.True(config.DownloadTheme);
        Assert.Equal("custom-theme.zip", config.ThemePath);
    }

    [Fact]
    public void GhostFxConfig_JsonDeserialization_SupportsThemePathAndThemeOutputPath()
    {
        string jsonNew = """{ "themePath": "my-theme-folder" }""";
        string jsonOld = """{ "themeOutputPath": "my-theme-legacy.zip" }""";

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var configNew = JsonSerializer.Deserialize<GhostFxConfig>(jsonNew, options);
        var configOld = JsonSerializer.Deserialize<GhostFxConfig>(jsonOld, options);

        Assert.NotNull(configNew);
        Assert.Equal("my-theme-folder", configNew.ThemePath);

        Assert.NotNull(configOld);
        Assert.Equal("my-theme-legacy.zip", configOld.ThemePath);
    }

    [Fact]
    public void GhostFxConfig_JsonDeserialization_SupportsGhostExportJsonAndInputJsonPath()
    {
        string jsonNew = """{ "ghostExportJson": "export-new.json" }""";
        string jsonOld = """{ "inputJsonPath": "export-old.json" }""";

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var configNew = JsonSerializer.Deserialize<GhostFxConfig>(jsonNew, options);
        var configOld = JsonSerializer.Deserialize<GhostFxConfig>(jsonOld, options);

        Assert.NotNull(configNew);
        Assert.Equal("export-new.json", configNew.GhostExportJson);

        Assert.NotNull(configOld);
        Assert.Equal("export-old.json", configOld.GhostExportJson);
    }
}
