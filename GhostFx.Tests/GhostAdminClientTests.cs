using System;
using GhostFx.Core;
using Xunit;

namespace GhostFx.Tests;

public class GhostAdminClientTests
{
    [Fact]
    public void GenerateGhostJwt_ValidKey_GeneratesValidTokenString()
    {
        string validApiKey = "640a1b2c3d4e5f6a7b8c9d0e:1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        string token = GhostAdminClient.GenerateGhostJwt(validApiKey);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Equal(2, token.Split('.').Length - 1);
    }

    [Fact]
    public void GenerateGhostJwt_InvalidKeyFormat_ThrowsArgumentException()
    {
        string invalidKey = "invalid-key-without-colon";
        Assert.Throws<ArgumentException>(() => GhostAdminClient.GenerateGhostJwt(invalidKey));
    }

    [Fact]
    public void OutputUserJwt()
    {
        string key = "6a777175fa1c3d04edb5b95e:ac7b580278fd70336f1713640014e7d528abb981658923a4d6a5f747dad39a1c";
        string token = GhostAdminClient.GenerateGhostJwt(key, "/v3/admin/");
        Assert.NotEmpty(token);
    }
}
