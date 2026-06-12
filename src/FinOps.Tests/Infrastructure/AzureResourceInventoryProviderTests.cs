using System.Text;
using FinOps.Infrastructure.Azure;

namespace FinOps.Tests.Infrastructure;

public sealed class AzureResourceInventoryProviderTests
{
    [Fact]
    public void ParseResources_MapsResourceGraphObjectArray()
    {
        var json = """
            [
              {
                "id": "/subscriptions/sub-1/resourceGroups/rg-demo/providers/Microsoft.Storage/storageAccounts/demo",
                "name": "demo",
                "type": "microsoft.storage/storageaccounts",
                "location": "australiaeast",
                "resourceGroup": "rg-demo",
                "subscriptionId": "sub-1",
                "tags": {
                  "environment": "dev",
                  "cost-center": "learning"
                }
              }
            ]
            """;

        var result = AzureResourceInventoryProvider.ParseResources(
            BinaryData.FromBytes(Encoding.UTF8.GetBytes(json)));

        var resource = Assert.Single(result);
        Assert.Equal("Azure", resource.Provider);
        Assert.Equal("sub-1", resource.AccountId);
        Assert.Equal("demo", resource.ResourceName);
        Assert.Equal("australiaeast", resource.Region);
        Assert.Equal("dev", resource.Tags["environment"]);
    }
}
