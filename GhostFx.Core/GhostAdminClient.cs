using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    public static async Task<(List<GhostPost> Posts, string DetectedVersion)> FetchPostsFromApiAsync(string ghostUrl, string adminApiKey, bool includeDrafts = true, HttpClient? customClient = null)
    {
        string filterParam = includeDrafts ? "status:[published,draft]" : "status:published";
        string endpoint = $"posts/?limit=all&include=tags,authors&filter={filterParam}";

        using var client = customClient ?? new HttpClient();

        var (response, version) = await SendWithFallbackAsync(client, ghostUrl, endpoint, adminApiKey);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch posts from Ghost API ({response.StatusCode})");
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<GhostApiPostsResponse>(jsonString, options);

        return (result?.Posts ?? [], version);
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
