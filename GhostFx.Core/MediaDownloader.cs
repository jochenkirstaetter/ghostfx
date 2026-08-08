using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GhostFx.Core;

public static class MediaDownloader
{
    private static readonly Regex ImageUrlRegex = new(
        @"(?:https?://[^""'\s>]+)?/content/images/[^""'\s>\)]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GeneralImgSrcRegex = new(
        @"<img\s+[^>]*src=[""']([^""']+)[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static async Task<List<string>> ProcessAndDownloadMediaAsync(
        List<GhostPost> posts,
        string ghostUrl,
        string outputDir,
        Action<int, int, string>? onProgress = null,
        HttpClient? customClient = null)
    {
        string cleanGhostUrl = ghostUrl.TrimEnd('/');
        string mediaDir = Path.Combine(outputDir, "media");
        Directory.CreateDirectory(mediaDir);

        var urlToLocalPathMap = new Dictionary<string, (string RelativePath, string FullUrl, string LocalFilePath)>(StringComparer.OrdinalIgnoreCase);

        foreach (var post in posts)
        {
            List<string> candidateUrls = [];
            if (!string.IsNullOrWhiteSpace(post.FeatureImage)) candidateUrls.Add(post.FeatureImage);
            if (!string.IsNullOrWhiteSpace(post.OgImage)) candidateUrls.Add(post.OgImage);

            if (!string.IsNullOrWhiteSpace(post.Html))
            {
                foreach (Match match in ImageUrlRegex.Matches(post.Html))
                {
                    candidateUrls.Add(match.Value);
                }
                foreach (Match match in GeneralImgSrcRegex.Matches(post.Html))
                {
                    candidateUrls.Add(match.Groups[1].Value);
                }
            }

            foreach (var url in candidateUrls)
            {
                if (string.IsNullOrWhiteSpace(url) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    continue;

                string fullUrl = url;
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    fullUrl = $"{cleanGhostUrl}/{url.TrimStart('/')}";
                }

                if (!fullUrl.Contains("/content/images/", StringComparison.OrdinalIgnoreCase) &&
                    !fullUrl.StartsWith(cleanGhostUrl, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relativePath = ExtractRelativePath(url);
                if (string.IsNullOrWhiteSpace(relativePath)) continue;

                string localFilePath = Path.Combine(mediaDir, relativePath);

                if (!urlToLocalPathMap.ContainsKey(url))
                {
                    urlToLocalPathMap[url] = (relativePath, fullUrl, localFilePath);
                }
            }
        }

        var uniqueMediaList = urlToLocalPathMap.Values.ToList();
        int totalMedia = uniqueMediaList.Count;
        var downloadedFiles = new ConcurrentBag<string>();

        if (totalMedia > 0)
        {
            using var client = customClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            if (customClient == null && !client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "GhostFx-Migrator/1.0");
            }

            int processedCount = 0;
            using var semaphore = new SemaphoreSlim(8);

            var downloadTasks = uniqueMediaList.Select(async item =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string localFileDir = Path.GetDirectoryName(item.LocalFilePath) ?? mediaDir;
                    Directory.CreateDirectory(localFileDir);

                    if (!File.Exists(item.LocalFilePath))
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                            var response = await client.GetAsync(item.FullUrl, cts.Token);
                            if (response.IsSuccessStatusCode)
                            {
                                await using var fs = new FileStream(item.LocalFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                                await response.Content.CopyToAsync(fs, cts.Token);
                                downloadedFiles.Add(item.LocalFilePath);
                            }
                        }
                        catch
                        {
                            // Ignore individual non-fatal media download failure
                        }
                    }
                    else
                    {
                        downloadedFiles.Add(item.LocalFilePath);
                    }
                }
                finally
                {
                    int current = Interlocked.Increment(ref processedCount);
                    onProgress?.Invoke(current, totalMedia, $"Downloading media: {Path.GetFileName(item.LocalFilePath)}");
                    semaphore.Release();
                }
            });

            await Task.WhenAll(downloadTasks);
        }

        foreach (var post in posts)
        {
            bool isDraft = string.Equals(post.Status, "draft", StringComparison.OrdinalIgnoreCase);
            bool isScheduled = string.Equals(post.Status, "scheduled", StringComparison.OrdinalIgnoreCase);
            bool isPage = string.Equals(post.Type, "page", StringComparison.OrdinalIgnoreCase);
            bool inSubfolder = isDraft || isScheduled || isPage;

            foreach (var kvp in urlToLocalPathMap)
            {
                string origUrl = kvp.Key;
                var info = kvp.Value;

                // Only rewrite link if the media asset was successfully downloaded and exists locally
                if (!File.Exists(info.LocalFilePath))
                {
                    continue;
                }

                string relativePublishedPath = $"media/{info.RelativePath.Replace('\\', '/').TrimStart('/')}";
                string relativeSubfolderPath = $"../media/{info.RelativePath.Replace('\\', '/').TrimStart('/')}";
                string targetRelPath = inSubfolder ? relativeSubfolderPath : relativePublishedPath;

                if (!string.IsNullOrWhiteSpace(post.Html))
                {
                    post.Html = post.Html.Replace(origUrl, targetRelPath);
                    post.Html = post.Html.Replace(info.FullUrl, targetRelPath);
                }

                if (string.Equals(post.FeatureImage, origUrl, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(post.FeatureImage, info.FullUrl, StringComparison.OrdinalIgnoreCase))
                {
                    post.FeatureImage = relativePublishedPath;
                }

                if (string.Equals(post.OgImage, origUrl, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(post.OgImage, info.FullUrl, StringComparison.OrdinalIgnoreCase))
                {
                    post.OgImage = relativePublishedPath;
                }
            }
        }

        return downloadedFiles.ToList();
    }

    private static string ExtractRelativePath(string url)
    {
        int index = url.IndexOf("/content/images/", StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            return url[(index + "/content/images/".Length)..].Replace('/', Path.DirectorySeparatorChar);
        }

        try
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            string path = uri.IsAbsoluteUri ? uri.AbsolutePath : url;
            return Path.GetFileName(path);
        }
        catch
        {
            return Path.GetFileName(url);
        }
    }
}
