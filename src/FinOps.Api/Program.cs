using FinOps.Api.Endpoints;
using FinOps.Application.Cloud;
using FinOps.Infrastructure;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICloudResourceSyncService, CloudResourceSyncService>();
builder.Services.AddScoped<ICloudCostSyncService, CloudCostSyncService>();
builder.Services.AddScoped<ICloudCostQueryService, CloudCostQueryService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddPostgreSqlHealthCheck(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FinOpsDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapFinOpsEndpoints();

app.Run();

public partial class Program;
