using FinOps.Application.Cloud;
using FinOps.Infrastructure;
using FinOps.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICloudResourceSyncService, CloudResourceSyncService>();
builder.Services.AddScoped<ICloudCostSyncService, CloudCostSyncService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<EtlWorkerOptions>(
    builder.Configuration.GetSection(EtlWorkerOptions.SectionName));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
