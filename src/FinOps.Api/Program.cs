using FinOps.Api.Endpoints;
using FinOps.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationUseCases();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddPostgreSqlHealthCheck(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.MapFinOpsEndpoints();

app.Run();

public partial class Program;
