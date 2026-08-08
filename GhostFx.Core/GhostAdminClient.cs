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
    public static string GenerateGhostJwt(string adminApiKey)
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
            { "aud", "/admin/" }
        };

        var token = new JwtSecurityToken(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static async Task<(string HeaderCode, string FooterCode)> GetCodeInjectionsAsync(string ghostUrl, string adminApiKey, HttpClient? customClient = null)
    {
        string jwt = GenerateGhostJwt(adminApiKey);
        string requestUrl = $"{ghostUrl.TrimEnd('/')}/ghost/api/admin/settings/";

        using var client = customClient ?? new HttpClient();
        if (customClient == null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ghost", jwt);
        }

        var response = await client.GetAsync(requestUrl);
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

    public static async Task<List<GhostPost>> FetchPostsFromApiAsync(string ghostUrl, string adminApiKey, bool includeDrafts = true, HttpClient? customClient = null)
    {
        string jwt = GenerateGhostJwt(adminApiKey);
        string statusFilter = includeDrafts ? "all" : "published";
        string requestUrl = $"{ghostUrl.TrimEnd('/')}/ghost/api/admin/posts/?limit=all&include=tags,authors&filter=status:[published,draft]";

        using var client = customClient ?? new HttpClient();
        if (customClient == null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ghost", jwt);
        }

        var response = await client.GetAsync(requestUrl);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch posts from Ghost API ({response.StatusCode})");
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<GhostApiPostsResponse>(jsonString, options);

        return result?.Posts ?? [];
    }

    public static async Task DownloadActiveThemeAsync(string ghostUrl, string adminApiKey, string outputPath, HttpClient? customClient = null)
    {
        string jwt = GenerateGhostJwt(adminApiKey);
        string requestUrl = $"{ghostUrl.TrimEnd('/')}/ghost/api/admin/themes/active/download/";

        using var client = customClient ?? new HttpClient();
        if (customClient == null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Ghost", jwt);
        }

        var response = await client.GetAsync(requestUrl);
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
