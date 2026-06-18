using System.Text;
using Azure.Core;
using Azure.ResourceManager;
using FinOps.Infrastructure.Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

    [Fact]
    public void CreateSampleCosts_CreatesTwoServicesPerDay()
    {
        var costs = AzureCostProvider.CreateSampleCosts(
            "subscription-1",
            new DateOnly(2026, 6, 6),
            new DateOnly(2026, 6, 12),
            "test");

        Assert.Equal(14, costs.Count);
        Assert.All(costs, cost =>
            Assert.Contains("\"source\":\"sample\"", cost.RawJson, StringComparison.Ordinal));
        Assert.Equal(2, costs.Select(cost => cost.ServiceName).Distinct().Count());
        Assert.Equal(7, costs.Select(cost => cost.UsageDate).Distinct().Count());
    }

    [Fact]
    public void CreateSampleCosts_RecordsForcedOfflineSource()
    {
        var costs = AzureCostProvider.CreateSampleCosts(
            "sample-subscription",
            new DateOnly(2026, 6, 18),
            new DateOnly(2026, 6, 18),
            "forced");

        Assert.Equal(2, costs.Count);
        Assert.All(costs, cost => Assert.Equal("sample-subscription", cost.AccountId));
        Assert.All(costs, cost =>
            Assert.Contains("\"reason\":\"forced\"", cost.RawJson, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetDailyCostsAsync_ForcedSampleData_DoesNotRequestAzureCredentials()
    {
        var credential = new RejectingTokenCredential();
        var provider = new AzureCostProvider(
            new ArmClient(credential),
            credential,
            new HttpClient(),
            Options.Create(new AzureCostOptions { ForceSampleData = true }),
            NullLogger<AzureCostProvider>.Instance);

        var costs = await provider.GetDailyCostsAsync(
            new DateOnly(2026, 6, 18),
            new DateOnly(2026, 6, 18),
            CancellationToken.None);

        Assert.Equal(2, costs.Count);
        Assert.All(costs, cost => Assert.Equal("sample-subscription", cost.AccountId));
    }

    private sealed class RejectingTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Azure credentials must not be requested.");

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Azure credentials must not be requested.");
    }
}
