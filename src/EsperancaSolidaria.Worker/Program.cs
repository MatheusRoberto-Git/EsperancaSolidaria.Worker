using EsperancaSolidaria.Worker;
using EsperancaSolidaria.Worker.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DonationWorker>();

var host = builder.Build();
host.Run();