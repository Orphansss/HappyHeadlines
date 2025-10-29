using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SubscriberService.Infrastructure.Messaging.Topology
{
    public sealed class TopologyBootstrapper : IHostedService
    {
        private readonly IConfiguration _cfg;
        private readonly ILogger<TopologyBootstrapper> _logger;

        public TopologyBootstrapper(IConfiguration cfg, ILogger<TopologyBootstrapper> logger)
        {
            _cfg = cfg;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken ct)
        {
            var uri = new Uri(_cfg["Rabbit:Uri"]!);
            var factory = new ConnectionFactory { Uri = uri };
            using var conn = factory.CreateConnection("subscriber-service-topology");
            using var ch = conn.CreateModel();

            var exchange   = _cfg["Rabbit:Exchange"]                ?? "subscriber.events";
            var queue      = _cfg["Rabbit:QueueNewSubscriber"]      ?? "subscriber.welcome.q";
            var routingKey = _cfg["Rabbit:RoutingKeyNewSubscriber"] ?? "subscriber.created";

            ch.ExchangeDeclare(exchange, type: "topic", durable: true, autoDelete: false);
            ch.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
            ch.QueueBind(queue, exchange, routingKey);

            _logger.LogInformation("Rabbit topology ensured. Exchange={Exchange}, Queue={Queue}, RK={RK}",
                exchange, queue, routingKey);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}