using System;
using System.IO;
using System.Threading.Tasks;
using GhostFx.Core;
using Xunit;

namespace GhostFx.Tests;

public class MigrationEngineTests : IDisposable
{
    private readonly string _testOutputDir;

    public MigrationEngineTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), "GhostFx_Test_Output_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            try { Directory.Delete(_testOutputDir, true); } catch { }
        }
    }

    private readonly string _sampleGhostJson = """
    {
      "db": [
        {
          "data": {
            "posts": [
              {
                "id": "p1",
                "title": "Migrating Ghost to DocFx",
                "slug": "migrating-ghost-to-docfx",
                "html": "<p>Converting Ghost HTML to Markdown formatted for DocFx.</p>",
                "custom_excerpt": "DocFx migration guide",
                "published_at": "2026-07-15T10:00:00.000Z",
                "status": "published"
              },
              {
                "id": "p2",
                "title": "Draft Article on .NET 10",
                "slug": "draft-article-dotnet-10",
                "html": "<p>Upcoming .NET 10 features overview...</p>",
                "status": "draft"
              }
            ],
            "tags": [
              { "id": "t1", "name": "Migration", "slug": "migration" },
              { "id": "t2", "name": "DocFx", "slug": "docfx" }
            ],
            "posts_tags": [
              { "post_id": "p1", "tag_id": "t1" },
              { "post_id": "p1", "tag_id": "t2" }
            ]
          }
        }
      ]
    }
    """;

    [Fact]
    public async Task ExecuteAsync_WithoutDrafts_GeneratesOnlyPublishedPosts()
    {
        var config = new GhostFxConfig
        {
            OutputDir = _testOutputDir,
            IndexFile = Path.Combine(_testOutputDir, "index.md"),
            SiteTitle = "GhostFx Migration Test",
            IncludeDrafts = false
        };

        var engine = new MigrationEngine();
        var result = await engine.ExecuteAsync(config, _sampleGhostJson);

        Assert.True(result.Success);
        Assert.Equal(1, result.ProcessedPosts);
        Assert.Equal(0, result.ProcessedDrafts);

        string postFile = Path.Combine(_testOutputDir, "published", "migrating-ghost-to-docfx.md");
        Assert.True(File.Exists(postFile));

        string postContent = await File.ReadAllTextAsync(postFile);
        Assert.Contains("uid: migrating-ghost-to-docfx", postContent);
        Assert.Contains("Migration", postContent);
        Assert.Contains("DocFx", postContent);

        string indexFile = Path.Combine(_testOutputDir, "index.md");
        Assert.True(File.Exists(indexFile));
        string indexContent = await File.ReadAllTextAsync(indexFile);
        Assert.Contains("GhostFx Migration Test", indexContent);
        Assert.Contains("Migrating Ghost to DocFx", indexContent);

        string tocFile = Path.Combine(_testOutputDir, "toc.yml");
        Assert.True(File.Exists(tocFile));

        string pubTocFile = Path.Combine(_testOutputDir, "published", "toc.yml");
        Assert.True(File.Exists(pubTocFile));

        string tagFile = Path.Combine(_testOutputDir, "tags", "migration.md");
        Assert.True(File.Exists(tagFile));
    }

    [Fact]
    public async Task ExecuteAsync_WithDrafts_GeneratesDraftsInSubfolder()
    {
        var config = new GhostFxConfig
        {
            OutputDir = _testOutputDir,
            IndexFile = Path.Combine(_testOutputDir, "index.md"),
            SiteTitle = "GhostFx Drafts Test",
            IncludeDrafts = true
        };

        var engine = new MigrationEngine();
        var result = await engine.ExecuteAsync(config, _sampleGhostJson);

        Assert.True(result.Success);
        Assert.Equal(1, result.ProcessedPosts);
        Assert.Equal(1, result.ProcessedDrafts);

        string draftFile = Path.Combine(_testOutputDir, "draft", "draft-article-dotnet-10.md");
        Assert.True(File.Exists(draftFile));

        string draftContent = await File.ReadAllTextAsync(draftFile);
        Assert.Contains("Draft Article on .NET 10 (Draft)", draftContent);

        string draftTocFile = Path.Combine(_testOutputDir, "draft", "toc.yml");
        Assert.True(File.Exists(draftTocFile));
    }

    [Fact]
    public async Task ExecuteAsync_WithScheduledAndPages_GeneratesScheduledAndPagesInSubfolders()
    {
        string jsonWithScheduledAndPages = """
        {
          "db": [
            {
              "data": {
                "posts": [
                  {
                    "id": "p1",
                    "title": "Scheduled Article",
                    "slug": "scheduled-article",
                    "html": "<p>Coming soon...</p>",
                    "status": "scheduled",
                    "type": "post"
                  },
                  {
                    "id": "p2",
                    "title": "About Us",
                    "slug": "about",
                    "html": "<p>About company...</p>",
                    "status": "published",
                    "type": "page"
                  }
                ],
                "tags": []
              }
            }
          ]
        }
        """;

        var config = new GhostFxConfig
        {
            OutputDir = _testOutputDir,
            IndexFile = Path.Combine(_testOutputDir, "index.md"),
            SiteTitle = "GhostFx Scheduled/Page Test",
            IncludeDrafts = true
        };

        var engine = new MigrationEngine();
        var result = await engine.ExecuteAsync(config, jsonWithScheduledAndPages);

        Assert.True(result.Success);
        Assert.Equal(1, result.ProcessedScheduled);
        Assert.Equal(1, result.ProcessedPages);

        string scheduledFile = Path.Combine(_testOutputDir, "scheduled", "scheduled-article.md");
        Assert.True(File.Exists(scheduledFile));

        string pageFile = Path.Combine(_testOutputDir, "pages", "about.md");
        Assert.True(File.Exists(pageFile));

        Assert.True(File.Exists(Path.Combine(_testOutputDir, "scheduled", "toc.yml")));
        Assert.True(File.Exists(Path.Combine(_testOutputDir, "pages", "toc.yml")));
    }
}
