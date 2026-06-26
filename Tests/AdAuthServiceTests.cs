using System.Runtime.Versioning;
using Auth.Web.Security.Auth;
using Xunit;

namespace Auth.Web.Tests;

[SupportedOSPlatform("windows")]
public class AdAuthServiceTests
{
    [Fact]
    public void ToDuration_ConvertsNegativeAdIntervalToPositiveTimeSpan()
    {
        var interval = -TimeSpan.FromMinutes(30).Ticks;

        var duration = AdAuthService.ToDuration(interval);

        Assert.Equal(TimeSpan.FromMinutes(30), duration);
    }

    [Fact]
    public void ToDuration_ZeroMeansAdministratorUnlock()
    {
        Assert.Equal(TimeSpan.Zero, AdAuthService.ToDuration(0));
    }

    [Fact]
    public void ToFileTimeUtc_ConvertsValidFileTime()
    {
        var expected = DateTimeOffset.UtcNow.AddMinutes(-2);
        var fileTime = expected.UtcDateTime.ToFileTimeUtc();

        var actual = AdAuthService.ToFileTimeUtc(fileTime);

        Assert.NotNull(actual);
        Assert.Equal(expected.ToUnixTimeSeconds(), actual.Value.ToUnixTimeSeconds());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void ToFileTimeUtc_ReturnsNullForMissingOrInvalidValues(long? value)
    {
        Assert.Null(AdAuthService.ToFileTimeUtc(value));
    }
}
