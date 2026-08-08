using GhostFx.Core;
using Xunit;

namespace GhostFx.Tests;

public class AsciiProgressBarTests
{
    [Fact]
    public void GenerateBar_AtZeroPercent_ReturnsDots()
    {
        string bar = AsciiProgressBar.GenerateBar(0, 10, width: 20);
        Assert.Contains("[....................]   0% (0/10)", bar);
    }

    [Fact]
    public void GenerateBar_AtFiftyPercent_ReturnsHalfFilled()
    {
        string bar = AsciiProgressBar.GenerateBar(5, 10, width: 20);
        Assert.Contains("[=========>..........]  50% (5/10)", bar);
    }

    [Fact]
    public void GenerateBar_AtHundredPercent_ReturnsFullyFilled()
    {
        string bar = AsciiProgressBar.GenerateBar(10, 10, width: 20);
        Assert.Contains("[====================] 100% (10/10)", bar);
    }
}
