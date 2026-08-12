using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace GhostFx.Core;

public sealed class FrontMatter
{
    [YamlMember(Alias = "uid")]
    public string Uid { get; set; } = string.Empty;

    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "slug")]
    public string Slug { get; set; } = string.Empty;

    [YamlMember(Alias = "date")]
    public string Date { get; set; } = string.Empty;

    [YamlMember(Alias = "status")]
    public string Status { get; set; } = string.Empty;

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = [];

    [YamlMember(Alias = "keywords")]
    public string Keywords { get; set; } = string.Empty;

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

    [YamlMember(Alias = "layout")]
    public string Layout { get; set; } = string.Empty;

    [YamlMember(Alias = "bodyClass")]
    public string BodyClass { get; set; } = string.Empty;

    [YamlMember(Alias = "postClass")]
    public string PostClass { get; set; } = string.Empty;

    [YamlMember(Alias = "isPost")]
    public bool IsPost { get; set; }

    [YamlMember(Alias = "isPage")]
    public bool IsPage { get; set; }

    [YamlMember(Alias = "isDraft")]
    public bool IsDraft { get; set; }

    [YamlMember(Alias = "isScheduled")]
    public bool IsScheduled { get; set; }

    [YamlMember(Alias = "isTagPage")]
    public bool IsTagPage { get; set; }

    [YamlMember(Alias = "isTagsIndexPage")]
    public bool IsTagsIndexPage { get; set; }

    [YamlMember(Alias = "isAuthorPage")]
    public bool IsAuthorPage { get; set; }

    [YamlMember(Alias = "isHome")]
    public bool IsHome { get; set; }

    [YamlMember(Alias = "author")]
    public string Author { get; set; } = string.Empty;

    [YamlMember(Alias = "authorTwitter")]
    public string AuthorTwitter { get; set; } = string.Empty;

    [YamlMember(Alias = "authorFacebook")]
    public string AuthorFacebook { get; set; } = string.Empty;

    [YamlMember(Alias = "website")]
    public string Website { get; set; } = string.Empty;

    [YamlMember(Alias = "location")]
    public string Location { get; set; } = string.Empty;

    [YamlMember(Alias = "authorImage")]
    public string AuthorImage { get; set; } = string.Empty;

    [YamlMember(Alias = "authorSlug")]
    public string AuthorSlug { get; set; } = string.Empty;

    [YamlMember(Alias = "canonicalUrl")]
    public string CanonicalUrl { get; set; } = string.Empty;

    [YamlMember(Alias = "imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [YamlMember(Alias = "twitterImageUrl")]
    public string TwitterImageUrl { get; set; } = string.Empty;

    [YamlMember(Alias = "authorImageUrl")]
    public string AuthorImageUrl { get; set; } = string.Empty;

    [YamlMember(Alias = "authorPageUrl")]
    public string AuthorPageUrl { get; set; } = string.Empty;

    [YamlMember(Alias = "tagName")]
    public string TagName { get; set; } = string.Empty;

    [YamlMember(Alias = "tagDescription")]
    public string TagDescription { get; set; } = string.Empty;

    [YamlMember(Alias = "feature_image")]
    public string FeatureImage { get; set; } = string.Empty;

    [YamlMember(Alias = "featured")]
    public bool Featured { get; set; }

    [YamlMember(Alias = "publishedAt")]
    public string PublishedAt { get; set; } = string.Empty;

    [YamlMember(Alias = "updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;

    [YamlMember(Alias = "excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [YamlMember(Alias = "twitter_title")]
    public string TwitterTitle { get; set; } = string.Empty;

    [YamlMember(Alias = "twitter_description")]
    public string TwitterDescription { get; set; } = string.Empty;

    [YamlMember(Alias = "twitter_image")]
    public string TwitterImage { get; set; } = string.Empty;

    [YamlMember(Alias = "facebook_title")]
    public string FacebookTitle { get; set; } = string.Empty;

    [YamlMember(Alias = "facebook_description")]
    public string FacebookDescription { get; set; } = string.Empty;

    [YamlMember(Alias = "facebook_image")]
    public string FacebookImage { get; set; } = string.Empty;

    [YamlMember(Alias = "codeinjection_head")]
    public string CodeinjectionHead { get; set; } = string.Empty;

    [YamlMember(Alias = "codeinjection_foot")]
    public string CodeinjectionFoot { get; set; } = string.Empty;
}

public sealed class BlogPostMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FileName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public bool IsDraft { get; set; }
    public bool IsScheduled { get; set; }
    public string Type { get; set; } = "post";
    public string FeatureImage { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorSlug { get; set; } = string.Empty;
    public string AuthorImage { get; set; } = string.Empty;
}

public sealed class PostCardItem
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "slug")]
    public string Slug { get; set; } = string.Empty;

    [YamlMember(Alias = "date")]
    public string Date { get; set; } = string.Empty;

    [YamlMember(Alias = "formattedDate")]
    public string FormattedDate { get; set; } = string.Empty;

    [YamlMember(Alias = "featureImage")]
    public string FeatureImage { get; set; } = string.Empty;

    [YamlMember(Alias = "excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [YamlMember(Alias = "authorName")]
    public string AuthorName { get; set; } = string.Empty;

    [YamlMember(Alias = "authorSlug")]
    public string AuthorSlug { get; set; } = string.Empty;

    [YamlMember(Alias = "authorImage")]
    public string AuthorImage { get; set; } = string.Empty;

    [YamlMember(Alias = "primaryTag")]
    public string PrimaryTag { get; set; } = string.Empty;

    [YamlMember(Alias = "tagClass")]
    public string TagClass { get; set; } = string.Empty;

    [YamlMember(Alias = "imageClass")]
    public string ImageClass { get; set; } = string.Empty;
}

public sealed class IndexFrontMatter
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;

    [YamlMember(Alias = "coverImage")]
    public string CoverImage { get; set; } = string.Empty;

    [YamlMember(Alias = "isHome")]
    public bool IsHome { get; set; } = true;

    [YamlMember(Alias = "bodyClass")]
    public string BodyClass { get; set; } = "home-template";

    [YamlMember(Alias = "posts")]
    public List<PostCardItem> Posts { get; set; } = [];
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

    [JsonPropertyName("settings")]
    public List<GhostSetting> Settings { get; set; } = [];

    [JsonPropertyName("users")]
    public List<GhostUser> Users { get; set; } = [];
}

public class GhostUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("profile_image")]
    public string ProfileImage { get; set; } = string.Empty;

    [JsonPropertyName("cover_image")]
    public string CoverImage { get; set; } = string.Empty;

    [JsonPropertyName("bio")]
    public string Bio { get; set; } = string.Empty;

    [JsonPropertyName("website")]
    public string Website { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("facebook")]
    public string Facebook { get; set; } = string.Empty;

    [JsonPropertyName("twitter")]
    public string Twitter { get; set; } = string.Empty;

    [JsonPropertyName("meta_title")]
    public string MetaTitle { get; set; } = string.Empty;

    [JsonPropertyName("meta_description")]
    public string MetaDescription { get; set; } = string.Empty;
}

public class GhostApiUsersResponse
{
    [JsonPropertyName("users")]
    public List<GhostUser> Users { get; set; } = [];
}

public class GhostSetting
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
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

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "published";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "post";

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

    [JsonPropertyName("authors")]
    public List<GhostUser> Authors { get; set; } = [];

    [JsonPropertyName("featured")]
    public bool Featured { get; set; }

    [JsonPropertyName("twitter_title")]
    public string TwitterTitle { get; set; } = string.Empty;

    [JsonPropertyName("twitter_description")]
    public string TwitterDescription { get; set; } = string.Empty;

    [JsonPropertyName("twitter_image")]
    public string TwitterImage { get; set; } = string.Empty;

    [JsonPropertyName("facebook_title")]
    public string FacebookTitle { get; set; } = string.Empty;

    [JsonPropertyName("facebook_description")]
    public string FacebookDescription { get; set; } = string.Empty;

    [JsonPropertyName("facebook_image")]
    public string FacebookImage { get; set; } = string.Empty;

    [JsonPropertyName("codeinjection_head")]
    public string CodeinjectionHead { get; set; } = string.Empty;

    [JsonPropertyName("codeinjection_foot")]
    public string CodeinjectionFoot { get; set; } = string.Empty;
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

public class GhostApiPagesResponse
{
    [JsonPropertyName("pages")]
    public List<GhostPost> Pages { get; set; } = [];
}

public class MigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProcessedPosts { get; set; }
    public int ProcessedDrafts { get; set; }
    public int ProcessedPages { get; set; }
    public int ProcessedScheduled { get; set; }
    public int ProcessedTags { get; set; }
    public TimeSpan ElapsedDuration { get; set; }
    public List<string> GeneratedFiles { get; set; } = [];
    public string HeaderCodeInjection { get; set; } = string.Empty;
    public string FooterCodeInjection { get; set; } = string.Empty;
    public string ThemeDownloadWarning { get; set; } = string.Empty;
    public string DetectedGhostVersion { get; set; } = string.Empty;
}

public class IconLink
{
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

public class GhostNavItem
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
