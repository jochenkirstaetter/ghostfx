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
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
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
        var settingsNode = root?["settings"];
        string headerCode = string.Empty;
        string footerCode = string.Empty;

        if (settingsNode is JsonArray settingsArray)
        {
            foreach (var setting in settingsArray)
            {
                string? key = setting?["key"]?.ToString();
                if (string.Equals(key, "codeinjection_head", StringComparison.OrdinalIgnoreCase))
                {
                    headerCode = setting?["value"]?.ToString() ?? string.Empty;
                }
                else if (string.Equals(key, "codeinjection_foot", StringComparison.OrdinalIgnoreCase))
                {
                    footerCode = setting?["value"]?.ToString() ?? string.Empty;
                }
            }
        }
        else if (settingsNode is JsonObject settingsObj)
        {
            foreach (var kvp in settingsObj)
            {
                if (string.Equals(kvp.Key, "codeinjection_head", StringComparison.OrdinalIgnoreCase))
                {
                    headerCode = kvp.Value?.ToString() ?? string.Empty;
                }
                else if (string.Equals(kvp.Key, "codeinjection_foot", StringComparison.OrdinalIgnoreCase))
                {
                    footerCode = kvp.Value?.ToString() ?? string.Empty;
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

    public static async Task<(string? Title, string? Description, string? IconUrl, string? LogoUrl, string? CoverUrl, List<GhostNavItem> NavItems, string? Locale, string? Twitter, string? Facebook, string? CodeinjectionHead, string? CodeinjectionFoot)> FetchSiteBrandInfoAsync(string ghostUrl, string adminApiKey, HttpClient? customClient = null)
    {
        bool disposeClient = customClient == null;
        var client = customClient ?? new HttpClient();
        List<GhostNavItem> navItems = [];
        string? title = null;
        string? description = null;
        string? icon = null;
        string? logo = null;
        string? cover = null;
        string? locale = null;
        string? twitter = null;
        string? facebook = null;
        string? codeinjectionHead = null;
        string? codeinjectionFoot = null;

        try
        {
            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            }
            string cleanGhostUrl = ghostUrl.TrimEnd('/');

            try
            {
                var (response, _) = await SendWithFallbackAsync(client, ghostUrl, "settings/", adminApiKey);
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var root = JsonNode.Parse(jsonString);
                    var settingsNode = root?["settings"];
                    if (settingsNode is JsonArray settingsArray)
                    {
                        foreach (var setting in settingsArray)
                        {
                            string? key = setting?["key"]?.ToString();
                            string? val = setting?["value"]?.ToString();
                            if (string.Equals(key, "title", StringComparison.OrdinalIgnoreCase)) title = val;
                            if (string.Equals(key, "description", StringComparison.OrdinalIgnoreCase)) description = val;
                            if (string.Equals(key, "icon", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "site_icon", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "favicon", StringComparison.OrdinalIgnoreCase)) icon = val;
                            if (string.Equals(key, "logo", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "site_logo", StringComparison.OrdinalIgnoreCase)) logo = val;
                            if (string.Equals(key, "cover_image", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "cover", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "cover_path", StringComparison.OrdinalIgnoreCase)) cover = val;
                            if (string.Equals(key, "locale", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "lang", StringComparison.OrdinalIgnoreCase)) locale = val;
                            if (string.Equals(key, "twitter", StringComparison.OrdinalIgnoreCase)) twitter = val;
                            if (string.Equals(key, "facebook", StringComparison.OrdinalIgnoreCase)) facebook = val;
                            if (string.Equals(key, "codeinjection_head", StringComparison.OrdinalIgnoreCase)) codeinjectionHead = val;
                            if (string.Equals(key, "codeinjection_foot", StringComparison.OrdinalIgnoreCase)) codeinjectionFoot = val;
                            if (string.Equals(key, "navigation", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(val))
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
                    else if (settingsNode is JsonObject settingsObj)
                    {
                        title ??= settingsObj["title"]?.ToString();
                        description ??= settingsObj["description"]?.ToString();
                        icon ??= settingsObj["icon"]?.ToString() ?? settingsObj["site_icon"]?.ToString() ?? settingsObj["favicon"]?.ToString();
                        logo ??= settingsObj["logo"]?.ToString() ?? settingsObj["site_logo"]?.ToString();
                        cover ??= settingsObj["cover_image"]?.ToString() ?? settingsObj["cover"]?.ToString() ?? settingsObj["cover_path"]?.ToString();
                        locale ??= settingsObj["locale"]?.ToString() ?? settingsObj["lang"]?.ToString();
                        twitter ??= settingsObj["twitter"]?.ToString();
                        facebook ??= settingsObj["facebook"]?.ToString();
                        codeinjectionHead ??= settingsObj["codeinjection_head"]?.ToString();
                        codeinjectionFoot ??= settingsObj["codeinjection_foot"]?.ToString();
                        var navNode = settingsObj["navigation"];
                        if (navNode != null)
                        {
                            try
                            {
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var parsedNav = JsonSerializer.Deserialize<List<GhostNavItem>>(navNode.ToJsonString(), options);
                                if (parsedNav != null && parsedNav.Count > 0) navItems = parsedNav;
                            }
                            catch { }
                        }
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
                    title ??= siteObj?["title"]?.ToString();
                    description ??= siteObj?["description"]?.ToString();
                    icon ??= siteObj?["icon"]?.ToString() ?? siteObj?["site_icon"]?.ToString() ?? siteObj?["favicon"]?.ToString();
                    logo ??= siteObj?["logo"]?.ToString() ?? siteObj?["site_logo"]?.ToString();
                    cover ??= siteObj?["cover_image"]?.ToString() ?? siteObj?["cover"]?.ToString() ?? siteObj?["cover_path"]?.ToString();
                    locale ??= siteObj?["locale"]?.ToString() ?? siteObj?["lang"]?.ToString();
                    twitter ??= siteObj?["twitter"]?.ToString();
                    facebook ??= siteObj?["facebook"]?.ToString();
                    var navArray = siteObj?["navigation"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(navArray))
                    {
                        try
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var parsedNav = JsonSerializer.Deserialize<List<GhostNavItem>>(navArray, options);
                            if (parsedNav != null && parsedNav.Count > 0) navItems = parsedNav;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            if (navItems.Count == 0 || string.IsNullOrWhiteSpace(cover) || string.IsNullOrWhiteSpace(icon))
            {
                try
                {
                    var htmlResponse = await client.GetAsync(cleanGhostUrl);
                    if (htmlResponse.IsSuccessStatusCode)
                    {
                        string html = await htmlResponse.Content.ReadAsStringAsync();

                        if (navItems.Count == 0)
                        {
                            var navMatches = Regex.Matches(html, """<li\s+class=["']nav-[^"']*["']><a\s+href=["']([^"']+)["']>([^<]+)</a></li>""", RegexOptions.IgnoreCase);
                            foreach (Match m in navMatches)
                            {
                                if (m.Success)
                                {
                                    string url = m.Groups[1].Value.Trim();
                                    string label = m.Groups[2].Value.Trim();
                                    navItems.Add(new GhostNavItem { Label = label, Url = url });
                                }
                            }
                        }

                        if (string.IsNullOrWhiteSpace(cover))
                        {
                            var match = Regex.Match(html, """<meta\s+property=["']og:image["']\s+content=["']([^"']+)["']""", RegexOptions.IgnoreCase);
                            if (!match.Success) match = Regex.Match(html, """<meta\s+content=["']([^"']+)["']\s+property=["']og:image["']""", RegexOptions.IgnoreCase);
                            if (match.Success) cover = match.Groups[1].Value.Trim();
                        }

                        if (string.IsNullOrWhiteSpace(icon) || icon.EndsWith("/favicon.png", StringComparison.OrdinalIgnoreCase) || icon.EndsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase))
                        {
                            var match = Regex.Match(html, """<link\s+[^>]*href=["']([^"']*?/content/images/[^"']+)["'][^>]*rel=["'](?:shortcut\s+|apple-touch-)?icon["']""", RegexOptions.IgnoreCase);
                            if (!match.Success) match = Regex.Match(html, """<link\s+[^>]*rel=["'](?:shortcut\s+|apple-touch-)?icon["'][^>]*href=["']([^"']*?/content/images/[^"']+)["']""", RegexOptions.IgnoreCase);
                            if (!match.Success) match = Regex.Match(html, """<link\s+[^>]*rel=["'](?:shortcut\s+)?icon["'][^>]*href=["']([^"']+)["']""", RegexOptions.IgnoreCase);
                            if (!match.Success) match = Regex.Match(html, """<link\s+[^>]*href=["']([^"']+)["'][^>]*rel=["'](?:shortcut\s+)?icon["']""", RegexOptions.IgnoreCase);
                            if (match.Success) icon = match.Groups[1].Value.Trim();
                        }

                        if (string.IsNullOrWhiteSpace(title))
                        {
                            var match = Regex.Match(html, """<meta\s+property=["']og:title["']\s+content=["']([^"']+)["']""", RegexOptions.IgnoreCase);
                            if (match.Success) title = match.Groups[1].Value.Trim();
                        }

                        if (string.IsNullOrWhiteSpace(description))
                        {
                            var match = Regex.Match(html, """<meta\s+property=["']og:description["']\s+content=["']([^"']+)["']""", RegexOptions.IgnoreCase);
                            if (match.Success) description = match.Groups[1].Value.Trim();
                        }
                    }
                }
                catch { }
            }
        }
        finally
        {
            if (disposeClient)
            {
                client.Dispose();
            }
        }

        return (title, description, icon, logo, cover, navItems, locale, twitter, facebook, codeinjectionHead, codeinjectionFoot);
    }

    public static async Task<(string? Title, string? Description, string? IconUrl, string? LogoUrl, string? CoverUrl, List<GhostNavItem> NavItems, string? Locale, string? Twitter, string? Facebook, string? CodeinjectionHead, string? CodeinjectionFoot)> FetchSiteSettingsViaContentApiAsync(string ghostUrl, string contentApiKey, HttpClient? customClient = null)
    {
        using var client = customClient ?? new HttpClient();
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }
        string cleanBase = ghostUrl.TrimEnd('/');
        if (cleanBase.EndsWith("/ghost")) cleanBase = cleanBase[..^6];
        else if (cleanBase.EndsWith("/ghost/api")) cleanBase = cleanBase[..^10];

        string[] versions = ["v3", "v4", "v5", "v6"];
        foreach (var ver in versions)
        {
            try
            {
                string url = $"{cleanBase}/ghost/api/{ver}/content/settings/?key={contentApiKey}";
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var root = JsonNode.Parse(jsonString);
                    var settingsNode = root?["settings"];
                    if (settingsNode is JsonArray settingsArr)
                    {
                        string? title = null, desc = null, icon = null, logo = null, cover = null, locale = null, twitter = null, facebook = null, codeinjectionHead = null, codeinjectionFoot = null;
                        List<GhostNavItem> navItems = [];
                        foreach (var setting in settingsArr)
                        {
                            string? key = setting?["key"]?.ToString();
                            string? val = setting?["value"]?.ToString();
                            if (string.Equals(key, "title", StringComparison.OrdinalIgnoreCase)) title = val;
                            if (string.Equals(key, "description", StringComparison.OrdinalIgnoreCase)) desc = val;
                            if (string.Equals(key, "icon", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "site_icon", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "favicon", StringComparison.OrdinalIgnoreCase)) icon = val;
                            if (string.Equals(key, "logo", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "site_logo", StringComparison.OrdinalIgnoreCase)) logo = val;
                            if (string.Equals(key, "cover_image", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "cover", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "cover_path", StringComparison.OrdinalIgnoreCase)) cover = val;
                            if (string.Equals(key, "locale", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "lang", StringComparison.OrdinalIgnoreCase)) locale = val;
                            if (string.Equals(key, "twitter", StringComparison.OrdinalIgnoreCase)) twitter = val;
                            if (string.Equals(key, "facebook", StringComparison.OrdinalIgnoreCase)) facebook = val;
                            if (string.Equals(key, "codeinjection_head", StringComparison.OrdinalIgnoreCase)) codeinjectionHead = val;
                            if (string.Equals(key, "codeinjection_foot", StringComparison.OrdinalIgnoreCase)) codeinjectionFoot = val;
                            if (string.Equals(key, "navigation", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(val))
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
                        return (title, desc, icon, logo, cover, navItems, locale, twitter, facebook, codeinjectionHead, codeinjectionFoot);
                    }
                    else if (settingsNode is JsonObject settingsObj)
                    {
                        string? title = settingsObj["title"]?.ToString();
                        string? desc = settingsObj["description"]?.ToString();
                        string? icon = settingsObj["icon"]?.ToString() ?? settingsObj["site_icon"]?.ToString() ?? settingsObj["favicon"]?.ToString();
                        string? logo = settingsObj["logo"]?.ToString() ?? settingsObj["site_logo"]?.ToString();
                        string? cover = settingsObj["cover_image"]?.ToString() ?? settingsObj["cover"]?.ToString() ?? settingsObj["cover_path"]?.ToString();
                        string? locale = settingsObj["locale"]?.ToString() ?? settingsObj["lang"]?.ToString();
                        string? twitter = settingsObj["twitter"]?.ToString();
                        string? facebook = settingsObj["facebook"]?.ToString();
                        string? codeinjectionHead = settingsObj["codeinjection_head"]?.ToString();
                        string? codeinjectionFoot = settingsObj["codeinjection_foot"]?.ToString();
                        List<GhostNavItem> navItems = [];
                        var navNode = settingsObj["navigation"];
                        if (navNode != null)
                        {
                            try
                            {
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var parsedNav = JsonSerializer.Deserialize<List<GhostNavItem>>(navNode.ToJsonString(), options);
                                if (parsedNav != null) navItems = parsedNav;
                            }
                            catch { }
                        }
                        return (title, desc, icon, logo, cover, navItems, locale, twitter, facebook, codeinjectionHead, codeinjectionFoot);
                    }
                }
            }
            catch { }
        }

        return (null, null, null, null, null, [], null, null, null, null, null);
    }

    public static async Task<(string? FaviconFile, string? LogoFile, string? CoverFile)> DownloadSiteBrandAssetsAsync(
        string ghostUrl,
        string adminApiKey,
        string outputDir,
        string? knownIcon = null,
        string? knownLogo = null,
        string? knownCover = null,
        HttpClient? customClient = null)
    {
        using var client = customClient ?? new HttpClient();
        string cleanGhostUrl = (ghostUrl ?? "").TrimEnd('/');
        string? faviconSaved = null;
        string? logoSaved = null;
        string? coverSaved = null;

        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        string? iconUrl = knownIcon;
        string? logoUrl = knownLogo;
        string? coverUrl = knownCover;

        if (string.IsNullOrWhiteSpace(iconUrl) || string.IsNullOrWhiteSpace(logoUrl) || string.IsNullOrWhiteSpace(coverUrl))
        {
            try
            {
                var (_, _, apiIcon, apiLogo, apiCover, _, _, _, _, _, _) = await FetchSiteBrandInfoAsync(ghostUrl ?? "", adminApiKey, client);
                iconUrl ??= apiIcon;
                logoUrl ??= apiLogo;
                coverUrl ??= apiCover;
            }
            catch { }
        }

        async Task<string?> EnsureAssetDownloadedAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            string cleanRel = url.Replace('\\', '/');
            int idx = cleanRel.IndexOf("/content/images/", StringComparison.OrdinalIgnoreCase);
            string subPath = idx >= 0 ? cleanRel[(idx + "/content/images/".Length)..] : cleanRel.TrimStart('/');

            string localPath = Path.Combine(outputDir, "content", "images", subPath);

            if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
            {
                return localPath;
            }

            string fullFetchUrl = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? url
                : $"{cleanGhostUrl}/{url.TrimStart('/')}";

            if (!string.IsNullOrWhiteSpace(cleanGhostUrl))
            {
                try
                {
                    var response = await client.GetAsync(fullFetchUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                        await using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await response.Content.CopyToAsync(fs);
                        return localPath;
                    }
                }
                catch { }
            }

            return null;
        }

        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, "content", "images"));

        // 1. Process Icon (settings.icon -> original filename under content/images/ + favicon.png)
        string? iconLocalPath = await EnsureAssetDownloadedAsync(iconUrl);
        if (iconLocalPath != null)
        {
            faviconSaved = iconLocalPath;
            try
            {
                string rootFavicon = Path.Combine(outputDir, "favicon.png");
                string mediaFavicon = Path.Combine(outputDir, "content", "images", "favicon.png");
                File.Copy(iconLocalPath, rootFavicon, overwrite: true);
                File.Copy(iconLocalPath, mediaFavicon, overwrite: true);
            }
            catch { }
        }

        // 2. Process Logo (settings.logo -> original filename under content/images/ + logo.png)
        string? logoLocalPath = await EnsureAssetDownloadedAsync(logoUrl);
        if (logoLocalPath != null)
        {
            logoSaved = logoLocalPath;
            try
            {
                string ext = Path.GetExtension(logoLocalPath);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                string rootLogo = Path.Combine(outputDir, "logo" + ext);
                string mediaLogo = Path.Combine(outputDir, "content", "images", "logo" + ext);
                File.Copy(logoLocalPath, rootLogo, overwrite: true);
                File.Copy(logoLocalPath, mediaLogo, overwrite: true);
            }
            catch { }
        }

        // 3. Process Cover (settings.cover_image -> original filename under content/images/ + cover.jpg)
        string? coverLocalPath = await EnsureAssetDownloadedAsync(coverUrl);
        if (coverLocalPath != null)
        {
            coverSaved = coverLocalPath;
            try
            {
                string rootCover = Path.Combine(outputDir, "cover.jpg");
                string mediaCover = Path.Combine(outputDir, "content", "images", "cover.jpg");
                File.Copy(coverLocalPath, rootCover, overwrite: true);
                File.Copy(coverLocalPath, mediaCover, overwrite: true);
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

    public static async Task<List<GhostUser>> FetchUsersFromApiAsync(string ghostUrl, string adminApiKey, HttpClient? customClient = null)
    {
        using var client = customClient ?? new HttpClient();
        var (response, _) = await SendWithFallbackAsync(client, ghostUrl, "users/?limit=all", adminApiKey);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<GhostApiUsersResponse>(jsonString, options);
        return result?.Users ?? [];
    }
}
