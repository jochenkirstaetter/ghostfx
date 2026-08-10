using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GhostFx.Core;

public class GhostJsonParser
{
    public (List<GhostPost> Posts, List<GhostTag> Tags, List<GhostUser> Users, string? Title, string? Description, string? Icon, string? Logo, string? CoverImage, List<GhostNavItem> NavItems, string? Locale, string? Twitter, string? Facebook, string? CodeinjectionHead, string? CodeinjectionFoot) ParseJsonExport(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            throw new ArgumentException("JSON content cannot be null or empty.");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var export = JsonSerializer.Deserialize<GhostExport>(jsonContent, options);

        var data = export?.Db?.FirstOrDefault()?.Data;
        if (data == null)
        {
            throw new InvalidDataException("Invalid Ghost JSON export format: Could not locate db[0].data.");
        }

        var tagsMap = data.Tags.ToDictionary(t => t.Id, t => t);
        var postToTagsLookup = data.PostsTags
            .GroupBy(m => m.PostId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => tagsMap.TryGetValue(m.TagId, out var tag) ? tag : null)
                      .Where(t => t != null)
                      .Select(t => t!)
                      .ToList()
            );

        foreach (var post in data.Posts)
        {
            if (postToTagsLookup.TryGetValue(post.Id, out var postTags))
            {
                post.Tags = postTags;
            }
        }

        string? title = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "title", StringComparison.OrdinalIgnoreCase))?.Value;
        string? description = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "description", StringComparison.OrdinalIgnoreCase))?.Value;
        string? icon = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "icon", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Key, "site_icon", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Key, "favicon", StringComparison.OrdinalIgnoreCase))?.Value;
        string? logo = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "logo", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Key, "site_logo", StringComparison.OrdinalIgnoreCase))?.Value;
        string? cover = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "cover_image", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Key, "cover", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Key, "cover_path", StringComparison.OrdinalIgnoreCase))?.Value;
        string? locale = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "locale", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Key, "lang", StringComparison.OrdinalIgnoreCase))?.Value;
        string? twitter = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "twitter", StringComparison.OrdinalIgnoreCase))?.Value;
        string? facebook = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "facebook", StringComparison.OrdinalIgnoreCase))?.Value;
        string? codeinjectionHead = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "codeinjection_head", StringComparison.OrdinalIgnoreCase))?.Value;
        string? codeinjectionFoot = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "codeinjection_foot", StringComparison.OrdinalIgnoreCase))?.Value;

        List<GhostNavItem> navItems = [];
        string? navJson = data.Settings?.FirstOrDefault(s => string.Equals(s.Key, "navigation", StringComparison.OrdinalIgnoreCase))?.Value;
        if (!string.IsNullOrWhiteSpace(navJson))
        {
            try
            {
                var parsedNav = JsonSerializer.Deserialize<List<GhostNavItem>>(navJson, options);
                if (parsedNav != null) navItems = parsedNav;
            }
            catch { }
        }

        return (data.Posts, data.Tags, data.Users ?? [], title, description, icon, logo, cover, navItems, locale, twitter, facebook, codeinjectionHead, codeinjectionFoot);
    }
}
