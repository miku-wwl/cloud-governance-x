using FinOps.Infrastructure.Azure;

namespace FinOps.Tests.Infrastructure;

public sealed class AzureSubscriptionMapperTests
{
    [Fact]
    public void Map_MapsAzureSubscriptionValues()
    {
        var result = AzureSubscriptionMapper.Map(
            "subscription-id",
            "Development",
            "tenant-id",
            "Enabled");

        Assert.Equal("subscription-id", result.SubscriptionId);
        Assert.Equal("Development", result.DisplayName);
        Assert.Equal("tenant-id", result.TenantId);
        Assert.Equal("Enabled", result.State);
    }

    [Fact]
    public void Map_UsesSafeDefaults_WhenAzureReturnsNullValues()
    {
        var result = AzureSubscriptionMapper.Map(null, null, null, null);

        Assert.Equal(string.Empty, result.SubscriptionId);
        Assert.Equal(string.Empty, result.DisplayName);
        Assert.Equal(string.Empty, result.TenantId);
        Assert.Equal("Unknown", result.State);
    }
}
