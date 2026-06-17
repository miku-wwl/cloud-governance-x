using FinOps.Infrastructure;
using FinOps.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationUseCases();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<EtlWorkerOptions>(
    builder.Configuration.GetSection(EtlWorkerOptions.SectionName));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
