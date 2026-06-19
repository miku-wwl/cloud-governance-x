using FinOps.Domain.Costs;

namespace FinOps.Tests.Domain;

public sealed class CloudCostDailyTests
{
    [Fact]
    public void Create_NormalizesCurrencyAndMissingResourceGroup()
    {
        var cost = CloudCostDaily.Create(
            Guid.NewGuid(),
            "Azure",
            "subscription-1",
            new DateOnly(2026, 6, 12),
            "Storage",
            null,
            1.25m,
            "nzd",
            "{}");

        Assert.Equal("NZD", cost.Currency);
        Assert.Equal("azure", cost.Provider);
        Assert.Equal(CloudCostDaily.UnassignedResourceGroup, cost.ResourceGroup);
    }

    [Fact]
    public void Create_requires_tenant()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CloudCostDaily.Create(
                Guid.Empty,
                "Azure",
                "subscription-1",
                new DateOnly(2026, 6, 19),
                "Storage",
                null,
                1m,
                "NZD",
                "{}"));
    }
}
