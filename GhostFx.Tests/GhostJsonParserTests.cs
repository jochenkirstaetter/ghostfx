using System.IO;
using System.Linq;
using GhostFx.Core;
using Xunit;

namespace GhostFx.Tests;

public class GhostJsonParserTests
{
    private readonly string _sampleJson = """
    {
      "db": [
        {
          "data": {
            "posts": [
              {
                "id": "post-1",
                "title": "First Blog Post",
                "slug": "first-blog-post",
                "html": "<h1>Welcome</h1><p>This is my first post.</p>",
                "custom_excerpt": "A short excerpt",
                "published_at": "2026-06-01T12:00:00.000Z",
                "status": "published",
                "meta_title": "First Post SEO",
                "meta_description": "First post meta description",
                "feature_image": "https://picsum.photos/800/600"
              },
              {
                "id": "post-2",
                "title": "Unpublished Draft",
                "slug": "unpublished-draft",
                "html": "<p>Work in progress content...</p>",
                "published_at": null,
                "status": "draft"
              }
            ],
            "tags": [
              {
                "id": "tag-1",
                "name": "Technology",
                "slug": "technology"
              },
              {
                "id": "tag-2",
                "name": ".NET Core",
                "slug": "net-core"
              }
            ],
            "posts_tags": [
              {
                "post_id": "post-1",
                "tag_id": "tag-1"
              },
              {
                "post_id": "post-1",
                "tag_id": "tag-2"
              }
            ]
          }
        }
      ]
    }
    """;

    [Fact]
    public void ParseJsonExport_ParsesPostsAndTagsCorrectly()
    {
        var parser = new GhostJsonParser();
        var (posts, tags, title, description, icon, logo, cover, navItems, locale) = parser.ParseJsonExport(_sampleJson);

        Assert.Equal(2, posts.Count);
        Assert.Equal(2, tags.Count);

        var publishedPost = posts.First(p => p.Id == "post-1");
        Assert.Equal("First Blog Post", publishedPost.Title);
        Assert.Equal("first-blog-post", publishedPost.Slug);
        Assert.Equal("published", publishedPost.Status);
        Assert.Equal(2, publishedPost.Tags.Count);
        Assert.Equal("Technology", publishedPost.Tags[0].Name);
        Assert.Equal(".NET Core", publishedPost.Tags[1].Name);

        var draftPost = posts.First(p => p.Id == "post-2");
        Assert.Equal("draft", draftPost.Status);
    }

    [Fact]
    public void ParseJsonExport_ParsesSettingsCorrectly()
    {
        string jsonWithSettings = """
        {
          "db": [
            {
              "data": {
                "posts": [],
                "tags": [],
                "posts_tags": [],
                "settings": [
                  { "key": "title", "value": "My Ghost Site" },
                  { "key": "description", "value": "A awesome blog" },
                  { "key": "navigation", "value": "[{\"label\":\"About\",\"url\":\"/about/\"}]" }
                ]
              }
            }
          ]
        }
        """;

        var parser = new GhostJsonParser();
        var (posts, tags, title, description, icon, logo, cover, navItems, locale) = parser.ParseJsonExport(jsonWithSettings);

        Assert.Equal("My Ghost Site", title);
        Assert.Equal("A awesome blog", description);
        Assert.Single(navItems);
        Assert.Equal("About", navItems[0].Label);
        Assert.Equal("/about/", navItems[0].Url);
    }

    [Fact]
    public void ParseJsonExport_ParsesLocaleSettingCorrectly()
    {
        string jsonWithLocale = """
        {
          "db": [
            {
              "data": {
                "posts": [],
                "tags": [],
                "posts_tags": [],
                "settings": [
                  { "key": "locale", "value": "de" }
                ]
              }
            }
          ]
        }
        """;

        var parser = new GhostJsonParser();
        var (_, _, _, _, _, _, _, _, locale) = parser.ParseJsonExport(jsonWithLocale);

        Assert.Equal("de", locale);
    }

    [Fact]
    public void ParseJsonExport_ThrowsOnInvalidFormat()
    {
        var parser = new GhostJsonParser();
        Assert.Throws<InvalidDataException>(() => parser.ParseJsonExport("{}"));
    }
}
