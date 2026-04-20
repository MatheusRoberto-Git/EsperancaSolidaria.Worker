using EsperancaSolidaria.Worker.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EsperancaSolidaria.Worker.Infrastructure
{
    public static class DependencyInjectionExtension
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            AddRabbitMq(services, configuration);
            AddRepositories(services, configuration);
        }

        private static void AddRabbitMq(IServiceCollection services, IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration.GetValue<string>("Settings:RabbitMq:HostName")!,
                UserName = configuration.GetValue<string>("Settings:RabbitMq:UserName")!,
                Password = configuration.GetValue<string>("Settings:RabbitMq:Password")!
            };

            services.AddSingleton(factory);
        }

        private static void AddRepositories(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Connection")!;

            services.AddScoped<ICampaignWorkerRepository>(_ =>
                new CampaignWorkerRepository(connectionString));
        }
    }
}
