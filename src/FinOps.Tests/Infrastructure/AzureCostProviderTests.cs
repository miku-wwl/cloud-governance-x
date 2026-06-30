using System.Text;
using FinOps.Infrastructure.Azure;

namespace FinOps.Tests.Infrastructure;

public sealed class AzureCostProviderTests
{
    [Fact]
    public void ParseResponse_MapsColumnsByName()
    {
        var json = """
            {
              "properties": {
                "columns": [
                  { "name": "Currency", "type": "String" },
                  { "name": "ResourceGroup", "type": "String" },
                  { "name": "PreTaxCost", "type": "Number" },
                  { "name": "UsageDate", "type": "Number" },
                  { "name": "ServiceName", "type": "String" }
                ],
                "rows": [
                  [ "NZD", "rg-demo", 3.75, 20260612, "Storage" ]
                ]
              }
            }
            """;

        var result = AzureCostProvider.ParseResponse(
            "subscription-1",
            BinaryData.FromBytes(Encoding.UTF8.GetBytes(json)));

        var cost = Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 6, 12), cost.UsageDate);
        Assert.Equal("Storage", cost.ServiceName);
        Assert.Equal("rg-demo", cost.ResourceGroup);
        Assert.Equal(3.75m, cost.Cost);
        Assert.Equal("NZD", cost.Currency);
        Assert.Contains("azure-cost-management", cost.RawJson, StringComparison.Ordinal);
    }

}
