using FinOps.Application.Cloud.Azure;
using FinOps.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddPostgreSqlHealthCheck(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "FinOps.Api",
    status = "running"
}));

app.MapGet(
    "/api/cloud/azure/subscriptions",
    async (IAzureSubscriptionReader subscriptionReader, CancellationToken cancellationToken) =>
    {
        var subscriptions = await subscriptionReader.GetSubscriptionsAsync(cancellationToken);
        return Results.Ok(subscriptions);
    });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.Run();

public partial class Program;
