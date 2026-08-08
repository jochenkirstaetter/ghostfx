using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace GhostFx.Core;

public class FrontMatter
{
    [YamlMember(Alias = "uid")]
    public string Uid { get; set; } = string.Empty;

    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "slug")]
    public string Slug { get; set; } = string.Empty;

    [YamlMember(Alias = "date")]
    public string Date { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = [];

    [YamlMember(Alias = "metaTitle")]
    public string MetaTitle { get; set; } = string.Empty;

    [YamlMember(Alias = "metaDescription")]
    public string MetaDescription { get; set; } = string.Empty;

    [YamlMember(Alias = "image")]
    public string Image { get; set; } = string.Empty;

    [YamlMember(Alias = "og_title")]
    public string OgTitle { get; set; } = string.Empty;

    [YamlMember(Alias = "og_description")]
    public string OgDescription { get; set; } = string.Empty;
}

public class BlogPostMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FileName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public bool IsDraft { get; set; }
}

public class GhostExport
{
    [JsonPropertyName("db")]
    public List<GhostDb> Db { get; set; } = [];
}

public class GhostDb
{
    [JsonPropertyName("data")]
    public GhostData Data { get; set; } = new();
}

public class GhostData
{
    [JsonPropertyName("posts")]
    public List<GhostPost> Posts { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<GhostTag> Tags { get; set; } = [];

    [JsonPropertyName("posts_tags")]
    public List<GhostPostTagMap> PostsTags { get; set; } = [];
}

public class GhostPost
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    [JsonPropertyName("mobiledoc")]
    public string Mobiledoc { get; set; } = string.Empty;

    [JsonPropertyName("lexical")]
    public string Lexical { get; set; } = string.Empty;

    [JsonPropertyName("custom_excerpt")]
    public string CustomExcerpt { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "published";

    [JsonPropertyName("meta_title")]
    public string MetaTitle { get; set; } = string.Empty;

    [JsonPropertyName("meta_description")]
    public string MetaDescription { get; set; } = string.Empty;

    [JsonPropertyName("feature_image")]
    public string FeatureImage { get; set; } = string.Empty;

    [JsonPropertyName("og_title")]
    public string OgTitle { get; set; } = string.Empty;

    [JsonPropertyName("og_description")]
    public string OgDescription { get; set; } = string.Empty;

    [JsonPropertyName("og_image")]
    public string OgImage { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<GhostTag> Tags { get; set; } = [];
}

public class GhostTag
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class GhostPostTagMap
{
    [JsonPropertyName("post_id")]
    public string PostId { get; set; } = string.Empty;

    [JsonPropertyName("tag_id")]
    public string TagId { get; set; } = string.Empty;
}

public class GhostApiPostsResponse
{
    [JsonPropertyName("posts")]
    public List<GhostPost> Posts { get; set; } = [];
}

public class MigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProcessedPosts { get; set; }
    public int ProcessedDrafts { get; set; }
    public int ProcessedTags { get; set; }
    public List<string> GeneratedFiles { get; set; } = [];
    public string HeaderCodeInjection { get; set; } = string.Empty;
    public string FooterCodeInjection { get; set; } = string.Empty;
    public string ThemeDownloadWarning { get; set; } = string.Empty;
    public string DetectedGhostVersion { get; set; } = string.Empty;
}
