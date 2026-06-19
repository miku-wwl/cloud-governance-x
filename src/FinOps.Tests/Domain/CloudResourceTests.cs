using FinOps.Domain.CloudResources;

namespace FinOps.Tests.Domain;

public sealed class CloudResourceTests
{
    [Fact]
    public void UpdateObservation_PreservesFirstSeenAndAdvancesLastSeen()
    {
        var firstSeen = DateTimeOffset.Parse("2026-06-12T00:00:00Z");
        var lastSeen = firstSeen.AddMinutes(5);
        var resource = CloudResource.Create(
            Guid.NewGuid(),
            "Azure",
            "subscription-1",
            "/subscriptions/1/resourceGroups/RG/providers/Microsoft.Storage/storageAccounts/demo",
            "demo",
            "microsoft.storage/storageaccounts",
            "australiaeast",
            "RG",
            "{}",
            firstSeen);

        resource.UpdateObservation(
            "subscription-1",
            "demo-renamed",
            "microsoft.storage/storageaccounts",
            "australiaeast",
            "RG",
            """{"environment":"dev"}""",
            lastSeen);

        Assert.Equal(firstSeen, resource.FirstSeenAt);
        Assert.Equal(lastSeen, resource.LastSeenAt);
        Assert.Equal("azure", resource.Provider);
        Assert.Equal("demo-renamed", resource.ResourceName);
        Assert.Equal("""{"environment":"dev"}""", resource.TagsJson);
    }

    [Fact]
    public void NormalizeResourceId_IsCaseInsensitiveAndTrimsWhitespace()
    {
        var result = CloudResource.NormalizeResourceId(" /subscriptions/AbC ");

        Assert.Equal("/SUBSCRIPTIONS/ABC", result);
    }

    [Fact]
    public void Create_requires_tenant()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CloudResource.Create(
                Guid.Empty,
                "Azure",
                "subscription-1",
                "/subscriptions/1/resource",
                "resource",
                "demo/type",
                "australiaeast",
                null,
                "{}",
                DateTimeOffset.UtcNow));
    }
}
