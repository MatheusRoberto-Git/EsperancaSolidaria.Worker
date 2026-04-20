using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EsperancaSolidaria.Worker
{
    public class DonationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;
        private const string QUEUE_NAME = "DoacaoRecebidaEvent";

        public DonationWorker(IServiceScopeFactory scopeFactory, ConnectionFactory factory)
        {
            _scopeFactory = scopeFactory;
            _factory = factory;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(
                queue: QUEUE_NAME,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var evento = JsonSerializer.Deserialize<DonationReceivedEvent>(json);

                    if(evento is not null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var repository = scope.ServiceProvider.GetRequiredService<ICampaignWorkerRepository>();
                        await repository.UpdateAmountRaised(evento.CampaignId, evento.Amount);
                    }

                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch
                {
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel!.BasicConsumeAsync(
                queue: QUEUE_NAME,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await (_channel?.CloseAsync(cancellationToken) ?? Task.CompletedTask);
            await (_connection?.CloseAsync(cancellationToken) ?? Task.CompletedTask);
        }
    }
}