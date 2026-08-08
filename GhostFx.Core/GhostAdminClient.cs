using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace GhostFx.Core;

public static class GhostAdminClient
{
    private static readonly (string Prefix, string Audience, string AcceptVersion, string VersionName)[] ApiRouteCandidates =
    [
        ("/ghost/api/admin/", "/admin/", "v6.0", "v6"),
        ("/ghost/api/v6/admin/", "/v6/admin/", "v6.0", "v6"),
        ("/ghost/api/v5/admin/", "/v5/admin/", "v5.0", "v5"),
        ("/ghost/api/v4/admin/", "/v4/admin/", "v4.0", "v4"),
        ("/ghost/api/v3/admin/", "/v3/admin/", "v3.0", "v3"),
        ("/ghost/api/admin/", "/v3/admin/", "v3.0", "v3"),
        ("/ghost/api/admin/", "/v4/admin/", "v4.0", "v4"),
        ("/ghost/api/admin/", "/v5/admin/", "v5.0", "v5"),
        ("/ghost/api/admin/", "/v6/admin/", "v6.0", "v6")
    ];

    public static string GenerateGhostJwt(string adminApiKey, string audience = "/admin/")
    {
        if (string.IsNullOrWhiteSpace(adminApiKey))
            throw new ArgumentException("Admin API key cannot be null or empty.");

        var parts = adminApiKey.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid Admin API key. Format must be ID:SECRET (e.g. 640a1b2c3d4e:1234567890abcdef)");

        string keyId = parts[0];
        string secretHex = parts[1];

        byte[] secretBytes;
        try
        {
            secretBytes = Enumerable.Range(0, secretHex.Length / 2)
                .Select(x => Convert.ToByte(secretHex.Substring(x * 2, 2), 16))
                .ToArray();
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid hex secret in Admin API key: {ex.Message}");
        }

        var securityKey = new SymmetricSecurityKey(secretBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var header = new JwtHeader(credentials);
        header["kid"] = keyId;

        var payload = new JwtPayload
        {
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() },
            { "aud", audience }
        };

        var token = new JwtSecurityToken(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<(HttpResponseMessage Response, string DetectedVersion)> SendWithFallbackAsync(HttpClient client, string ghostUrl, string relativeEndpoint, string adminApiKey)
    {
        string cleanBase = ghostUrl.TrimEnd('/');
        if (cleanBase.EndsWith("/ghost"))
        {
            cleanBase = cleanBase[..^6];
        }
        else if (cleanBase.EndsWith("/ghost/api"))
        {
            cleanBase = cleanBase[..^10];
        }

        HttpResponseMessage? lastResponse = null;
        string lastVersion = "v5";

        foreach (var (prefix, audience, acceptVersion, versionName) in ApiRouteCandidates)
        {
            string url = $"{cleanBase}{prefix}{relativeEndpoint.TrimStart('/')}";
            string jwt = GenerateGhostJwt(adminApiKey, audience);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Ghost", jwt);
            if (!string.IsNullOrEmpty(acceptVersion))
            {
                request.Headers.TryAddWithoutValidation("Accept-Version", acceptVersion);
            }

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return (response, versionName);
            }

            if (response.StatusCode != System.Net.HttpStatusCode.NotFound &&
                response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                return (response, versionName);
            }

            lastResponse = response;
            lastVersion = versionName;
        }

        return (lastResponse ?? new HttpResponseMessage(System.Net.HttpStatusCode.NotFound), lastVersion);
    }

    public static async Task<(string HeaderCode, string FooterCode)> GetCodeInjectionsAsync(string ghostUrl, string adminApiKey, HttpClient? customClient = null)
    {
        using var client = customClient ?? new HttpClient();

        var (response, _) = await SendWithFallbackAsync(client, ghostUrl, "settings/", adminApiKey);
        if (!response.IsSuccessStatusCode)
        {
            return (string.Empty, string.Empty);
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        var root = JsonNode.Parse(jsonString);
        var settingsArray = root?["settings"]?.AsArray();

        string headerCode = string.Empty;
        string footerCode = string.Empty;

        if (settingsArray != null)
        {
            foreach (var setting in settingsArray)
            {
                string? key = setting?["key"]?.ToString();
                if (key == "codeinjection_head")
                {
                    headerCode = setting?["value"]?.ToString() ?? string.Empty;
                }
                else if (key == "codeinjection_foot")
                {
                    footerCode = setting?["value"]?.ToString() ?? string.Empty;
                }
            }
        }

        return (headerCode, footerCode);
    }

    public static async Task<List<IconLink>> FetchSocialLinksAsync(string ghostUrl, string adminApiKey, HttpClient? customClient = null)
    {
        using var client = customClient ?? new HttpClient();
        List<IconLink> links = [];

        try
        {
            var (response, _) = await SendWithFallbackAsync(client, ghostUrl, "settings/", adminApiKey);
            if (response.IsSuccessStatusCode)
            {
                string jsonString = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(jsonString);
                var settingsArray = root?["settings"]?.AsArray();

                if (settingsArray != null)
                {
                    foreach (var setting in settingsArray)
                    {
                        string? key = setting?["key"]?.ToString();
                        string? val = setting?["value"]?.ToString();
                        if (string.IsNullOrWhiteSpace(val)) continue;

                        if (key == "twitter")
                        {
                            string handle = val.TrimStart('@');
                            string href = handle.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? handle : $"https://twitter.com/{handle}";
                            links.Add(new IconLink { Icon = "twitter", Href = href, Title = "Twitter / X" });
                        }
                        else if (key == "facebook")
                        {
                            string href = val.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? val : $"https://facebook.com/{val}";
                            links.Add(new IconLink { Icon = "facebook", Href = href, Title = "Facebook" });
                        }
                    }
                }
            }
        }
        catch { }

        if (links.Count == 0)
        {
            links.Add(new IconLink { Icon = "github", Href = "https://github.com/jochenkirstaetter/ghostfx", Title = "GitHub" });
        }

        return links;
    }

    public static async Task<(string? Title, string? Description, string? IconUrl, string? LogoUrl, string? CoverUrl, List<GhostNavItem> NavItems)> FetchSiteBrandInfoAsync(string ghostUrl, string adminApiKey, HttpClient? customClient = null)
    {
        using var client = customClient ?? new HttpClient();
        List<GhostNavItem> navItems = [];

        try
        {
            var (response, _) = await SendWithFallbackAsync(client, ghostUrl, "settings/", adminApiKey);
            if (response.IsSuccessStatusCode)
            {
                string jsonString = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(jsonString);
                var settingsArray = root?["settings"]?.AsArray();

                string? title = null;
                string? description = null;
                string? icon = null;
                string? logo = null;
                string? cover = null;

                if (settingsArray != null)
                {
                    foreach (var setting in settingsArray)
                    {
                        string? key = setting?["key"]?.ToString();
                        string? val = setting?["value"]?.ToString();
                        if (key == "title") title = val;
                        if (key == "description") description = val;
                        if (key == "icon") icon = val;
                        if (key == "logo") logo = val;
                        if (key == "cover_image") cover = val;
                        if (key == "navigation" && !string.IsNullOrWhiteSpace(val))
                        {
                            try
                            {
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var parsedNav = JsonSerializer.Deserialize<List<GhostNavItem>>(val, options);
                                if (parsedNav != null) navItems = parsedNav;
                            }
                            catch { }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(description) || !string.IsNullOrWhiteSpace(icon) || !string.IsNullOrWhiteSpace(logo) || !string.IsNullOrWhiteSpace(cover) || navItems.Count > 0)
                {
                    return (title, description, icon, logo, cover, navItems);
                }
            }
        }
        catch { }

        try
        {
            var (response, _) = await SendWithFallbackAsync(client, ghostUrl, "site/", adminApiKey);
            if (response.IsSuccessStatusCode)
            {
                string jsonString = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(jsonString);
                var siteObj = root?["site"];
                string? title = siteObj?["title"]?.ToString();
                string? description = siteObj?["description"]?.ToString();
                string? icon = siteObj?["icon"]?.ToString();
                string? logo = siteObj?["logo"]?.ToString();
                string? cover = siteObj?["cover_image"]?.ToString();
                var navArray = siteObj?["navigation"]?.ToString();
                if (!string.IsNullOrWhiteSpace(navArray))
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var parsedNav = JsonSerializer.Deserialize<List<GhostNavItem>>(navArray, options);
                        if (parsedNav != null) navItems = parsedNav;
                    }
                    catch { }
                }
                return (title, description, icon, logo, cover, navItems);
            }
        }
        catch { }

        return (null, null, null, null, null, navItems);
    }

    public static async Task<(string? FaviconFile, string? LogoFile, string? CoverFile)> DownloadSiteBrandAssetsAsync(string ghostUrl, string adminApiKey, string outputDir, HttpClient? customClient = null)
    {
        using var client = customClient ?? new HttpClient();
        string cleanGhostUrl = ghostUrl.TrimEnd('/');
        string? faviconSaved = null;
        string? logoSaved = null;
        string? coverSaved = null;

        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        var (_, _, iconUrl, logoUrl, coverUrl, _) = await FetchSiteBrandInfoAsync(ghostUrl, adminApiKey, client);

        List<string> candidateFaviconUrls = [];
        if (!string.IsNullOrWhiteSpace(iconUrl))
        {
            string url = iconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? iconUrl : $"{cleanGhostUrl}/{iconUrl.TrimStart('/')}";
            candidateFaviconUrls.Add(url);
        }
        else
        {
            try
            {
                var htmlResponse = await client.GetAsync(cleanGhostUrl);
                if (htmlResponse.IsSuccessStatusCode)
                {
                    string html = await htmlResponse.Content.ReadAsStringAsync();
                    var match = Regex.Match(html, """<link\s+[^>]*rel=["'](?:shortcut\s+)?icon["'][^>]*href=["']([^"']+)["']""", RegexOptions.IgnoreCase);
                    if (!match.Success)
                    {
                        match = Regex.Match(html, """<link\s+[^>]*href=["']([^"']+)["'][^>]*rel=["'](?:shortcut\s+)?icon["']""", RegexOptions.IgnoreCase);
                    }
                    if (match.Success)
                    {
                        string href = match.Groups[1].Value.Trim();
                        string url = href.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : $"{cleanGhostUrl}/{href.TrimStart('/')}";
                        candidateFaviconUrls.Add(url);
                    }
                }
            }
            catch { }
        }

        candidateFaviconUrls.Add($"{cleanGhostUrl}/favicon.png");
        candidateFaviconUrls.Add($"{cleanGhostUrl}/favicon.ico");

        foreach (var candidateUrl in candidateFaviconUrls)
        {
            try
            {
                var response = await client.GetAsync(candidateUrl);
                if (response.IsSuccessStatusCode)
                {
                    string ext = Path.GetExtension(candidateUrl);
                    if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".png";
                    string faviconFile = Path.Combine(outputDir, "favicon" + ext);
                    await using var fs = new FileStream(faviconFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs);
                    faviconSaved = faviconFile;
                    break;
                }
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            string targetLogoUrl = logoUrl;
            if (!targetLogoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !targetLogoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                targetLogoUrl = $"{cleanGhostUrl}/{targetLogoUrl.TrimStart('/')}";
            }

            try
            {
                var response = await client.GetAsync(targetLogoUrl);
                if (response.IsSuccessStatusCode)
                {
                    string ext = Path.GetExtension(targetLogoUrl);
                    if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".png";
                    string logoFile = Path.Combine(outputDir, "logo" + ext);
                    await using var fs = new FileStream(logoFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs);
                    logoSaved = logoFile;
                }
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(coverUrl))
        {
            string targetCoverUrl = coverUrl;
            if (!targetCoverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !targetCoverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                targetCoverUrl = $"{cleanGhostUrl}/{targetCoverUrl.TrimStart('/')}";
            }

            try
            {
                var response = await client.GetAsync(targetCoverUrl);
                if (response.IsSuccessStatusCode)
                {
                    string ext = Path.GetExtension(targetCoverUrl);
                    if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".png";
                    string coverFile = Path.Combine(outputDir, "cover" + ext);
                    await using var fs = new FileStream(coverFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs);
                    coverSaved = coverFile;
                }
            }
            catch { }
        }

        return (faviconSaved, logoSaved, coverSaved);
    }

    public static async Task<(List<GhostPost> Posts, string DetectedVersion)> FetchPostsFromApiAsync(string ghostUrl, string adminApiKey, bool includeDrafts = true, HttpClient? customClient = null)
    {
        string filterParam = includeDrafts ? "status:[published,draft,scheduled]" : "status:published";
        string postsEndpoint = $"posts/?limit=all&formats=html,mobiledoc&include=tags,authors&filter={filterParam}";
        string pagesEndpoint = $"pages/?limit=all&formats=html,mobiledoc&include=tags,authors&filter={filterParam}";

        using var client = customClient ?? new HttpClient();

        var (response, version) = await SendWithFallbackAsync(client, ghostUrl, postsEndpoint, adminApiKey);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch posts from Ghost API ({response.StatusCode})");
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<GhostApiPostsResponse>(jsonString, options);
        List<GhostPost> allItems = result?.Posts ?? [];
        foreach (var item in allItems)
        {
            item.Type = "post";
        }

        try
        {
            var (pagesResponse, _) = await SendWithFallbackAsync(client, ghostUrl, pagesEndpoint, adminApiKey);
            if (pagesResponse.IsSuccessStatusCode)
            {
                string pagesJson = await pagesResponse.Content.ReadAsStringAsync();
                var pagesResult = JsonSerializer.Deserialize<GhostApiPagesResponse>(pagesJson, options);
                if (pagesResult?.Pages != null)
                {
                    foreach (var page in pagesResult.Pages)
                    {
                        page.Type = "page";
                        allItems.Add(page);
                    }
                }
            }
        }
        catch { }

        return (allItems, version);
    }

    public static async Task DownloadActiveThemeAsync(string ghostUrl, string adminApiKey, string outputPath, HttpClient? customClient = null)
    {
        using var client = customClient ?? new HttpClient();

        var (response, _) = await SendWithFallbackAsync(client, ghostUrl, "themes/active/download/", adminApiKey);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to download active theme from Ghost ({response.StatusCode})");
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs);
    }
}
