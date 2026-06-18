using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.ResourceManager;
using FinOps.Application.Cloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinOps.Infrastructure.Azure;

internal sealed class AzureCostProvider(
    ArmClient armClient,
    TokenCredential credential,
    HttpClient httpClient,
    IOptions<AzureCostOptions> options,
    ILogger<AzureCostProvider> logger) : ICloudCostProvider
{
    private const string ApiVersion = "2025-03-01";
    private static readonly string[] TokenScopes = ["https://management.azure.com/.default"];

    public async Task<IReadOnlyList<CloudCostDailyDto>> GetDailyCostsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new ArgumentException("The cost query end date must not precede its start date.");
        }

        if (options.Value.ForceSampleData)
        {
            return CreateSampleCosts("sample-subscription", from, to, "forced");
        }

        var subscriptions = new List<string>();
        await foreach (var subscription in armClient
            .GetSubscriptions()
            .GetAllAsync(cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(subscription.Data.SubscriptionId))
            {
                subscriptions.Add(subscription.Data.SubscriptionId);
            }
        }

        var costs = new List<CloudCostDailyDto>();
        foreach (var subscriptionId in subscriptions)
        {
            var subscriptionCosts = await GetSubscriptionCostsAsync(
                subscriptionId,
                from,
                to,
                cancellationToken);

            if (subscriptionCosts.Count > 0)
            {
                costs.AddRange(subscriptionCosts);
                continue;
            }

            if (options.Value.UseSampleDataWhenUnavailable)
            {
                costs.AddRange(CreateSampleCosts(subscriptionId, from, to, "empty-response"));
            }
        }

        return costs;
    }

    internal static IReadOnlyList<CloudCostDailyDto> ParseResponse(
        string accountId,
        BinaryData data)
    {
        using var document = JsonDocument.Parse(data);
        if (!document.RootElement.TryGetProperty("properties", out var properties))
        {
            throw new JsonException("Azure Cost Management response has no properties object.");
        }

        var columns = properties
            .GetProperty("columns")
            .EnumerateArray()
            .Select((column, index) => new
            {
                Name = column.GetProperty("name").GetString() ?? string.Empty,
                Index = index
            })
            .ToDictionary(column => column.Name, column => column.Index, StringComparer.OrdinalIgnoreCase);

        var costIndex = GetColumnIndex(columns, "PreTaxCost", "Cost");
        var dateIndex = GetColumnIndex(columns, "UsageDate");
        var serviceIndex = GetColumnIndex(columns, "ServiceName", "ServiceTier", "MeterCategory");
        var resourceGroupIndex = GetColumnIndex(columns, "ResourceGroup");
        var currencyIndex = GetColumnIndex(columns, "Currency");
        var costs = new List<CloudCostDailyDto>();

        foreach (var row in properties.GetProperty("rows").EnumerateArray())
        {
            var values = row.EnumerateArray().ToArray();
            var usageDate = ParseUsageDate(values[dateIndex]);
            var serviceName = GetRequiredString(values[serviceIndex], "service name");
            var resourceGroup = GetNullableString(values[resourceGroupIndex]);
            var currency = GetRequiredString(values[currencyIndex], "currency");
            var cost = values[costIndex].GetDecimal();
            var rawJson = JsonSerializer.Serialize(new
            {
                source = "azure-cost-management",
                accountId,
                usageDate,
                serviceName,
                resourceGroup,
                cost,
                currency
            });

            costs.Add(new CloudCostDailyDto(
                "Azure",
                accountId,
                usageDate,
                serviceName,
                resourceGroup,
                cost,
                currency,
                rawJson));
        }

        return costs;
    }

    internal static IReadOnlyList<CloudCostDailyDto> CreateSampleCosts(
        string accountId,
        DateOnly from,
        DateOnly to,
        string reason)
    {
        var costs = new List<CloudCostDailyDto>();
        var services = new[]
        {
            (Name: "Storage", ResourceGroup: "rg-sample-platform", BaseCost: 1.25m),
            (Name: "Service Bus", ResourceGroup: "rg-sample-messaging", BaseCost: 0.45m)
        };

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var dayOffset = date.DayNumber - from.DayNumber;
            foreach (var service in services)
            {
                var cost = service.BaseCost + (dayOffset * 0.05m);
                var rawJson = JsonSerializer.Serialize(new
                {
                    source = "sample",
                    reason,
                    accountId,
                    usageDate = date,
                    serviceName = service.Name,
                    resourceGroup = service.ResourceGroup,
                    cost,
                    currency = "USD"
                });

                costs.Add(new CloudCostDailyDto(
                    "Azure",
                    accountId,
                    date,
                    service.Name,
                    service.ResourceGroup,
                    cost,
                    "USD",
                    rawJson));
            }
        }

        return costs;
    }

    private async Task<IReadOnlyList<CloudCostDailyDto>> GetSubscriptionCostsAsync(
        string subscriptionId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(TokenScopes),
                cancellationToken);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"subscriptions/{subscriptionId}/providers/Microsoft.CostManagement/query?api-version={ApiVersion}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Content = JsonContent.Create(CreateRequest(from, to));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return [];
            }

            var responseData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (!options.Value.UseSampleDataWhenUnavailable)
                {
                    throw new HttpRequestException(
                        $"Azure Cost Management query failed with HTTP {(int)response.StatusCode}: " +
                        BinaryData.FromBytes(responseData).ToString(),
                        null,
                        response.StatusCode);
                }

                logger.LogWarning(
                    "Azure Cost Management returned HTTP {StatusCode} for subscription {SubscriptionId}; using sample data.",
                    (int)response.StatusCode,
                    subscriptionId);
                return CreateSampleCosts(
                    subscriptionId,
                    from,
                    to,
                    $"http-{(int)response.StatusCode}");
            }

            return ParseResponse(subscriptionId, BinaryData.FromBytes(responseData));
        }
        catch (Exception exception) when (
            options.Value.UseSampleDataWhenUnavailable &&
            exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Azure Cost Management query failed for subscription {SubscriptionId}; using sample data.",
                subscriptionId);
            return CreateSampleCosts(subscriptionId, from, to, exception.GetType().Name);
        }
    }

    private static object CreateRequest(DateOnly from, DateOnly to)
    {
        return new
        {
            type = "Usage",
            timeframe = "Custom",
            timePeriod = new
            {
                from = $"{from:yyyy-MM-dd}T00:00:00Z",
                to = $"{to:yyyy-MM-dd}T23:59:59Z"
            },
            dataset = new
            {
                granularity = "Daily",
                aggregation = new
                {
                    totalCost = new
                    {
                        name = "PreTaxCost",
                        function = "Sum"
                    }
                },
                grouping = new[]
                {
                    new
                    {
                        type = "Dimension",
                        name = "ServiceName"
                    },
                    new
                    {
                        type = "Dimension",
                        name = "ResourceGroup"
                    }
                }
            }
        };
    }

    private static int GetColumnIndex(
        IReadOnlyDictionary<string, int> columns,
        params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (columns.TryGetValue(candidate, out var index))
            {
                return index;
            }
        }

        throw new JsonException(
            $"Azure Cost Management response is missing column '{string.Join("' or '", candidates)}'.");
    }

    private static DateOnly ParseUsageDate(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Number => DateOnly.ParseExact(
                value.GetInt32().ToString(CultureInfo.InvariantCulture),
                "yyyyMMdd",
                CultureInfo.InvariantCulture),
            JsonValueKind.String => DateOnly.Parse(
                value.GetString() ?? string.Empty,
                CultureInfo.InvariantCulture),
            _ => throw new JsonException("Azure Cost Management returned an invalid usage date.")
        };
    }

    private static string GetRequiredString(JsonElement value, string fieldName)
    {
        var result = GetNullableString(value);
        return string.IsNullOrWhiteSpace(result)
            ? throw new JsonException($"Azure Cost Management returned an empty {fieldName}.")
            : result;
    }

    private static string? GetNullableString(JsonElement value)
    {
        return value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? null
            : value.ToString();
    }
}
