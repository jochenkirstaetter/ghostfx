using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GhostFx.Core;

public class GhostJsonParser
{
    public (List<GhostPost> Posts, List<GhostTag> Tags) ParseJsonExport(string jsonContent)
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

        return (data.Posts, data.Tags);
    }
}
