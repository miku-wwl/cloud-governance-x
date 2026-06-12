using FinOps.Domain.Costs;

namespace FinOps.Tests.Domain;

public sealed class CloudCostDailyTests
{
    [Fact]
    public void Create_NormalizesCurrencyAndMissingResourceGroup()
    {
        var cost = CloudCostDaily.Create(
            "Azure",
            "subscription-1",
            new DateOnly(2026, 6, 12),
            "Storage",
            null,
            1.25m,
            "nzd",
            "{}");

        Assert.Equal("NZD", cost.Currency);
        Assert.Equal(CloudCostDaily.UnassignedResourceGroup, cost.ResourceGroup);
    }
}
