using FinOps.Application.Cloud;
using FinOps.Infrastructure;
using FinOps.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICloudResourceSyncService, CloudResourceSyncService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
