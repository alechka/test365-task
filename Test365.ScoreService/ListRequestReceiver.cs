using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Test365.Common;

namespace Test365.ScoreService;

internal class ListRequestReceiver(ScoresService scoresService, IConnection connection)
{
    public async Task RunAsync(CancellationToken ct)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var queueName = await SetupExchangesAndQueue(ct, channel);

        Console.WriteLine(" [*] Waiting for list requests...");
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += ListMessageHandler;
        await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer, cancellationToken: ct);
        
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        }
        consumer.ReceivedAsync -= ListMessageHandler;
        return;
        
        async Task ListMessageHandler(object sender, BasicDeliverEventArgs ea)
        {
            List<Score> results = [];
            try
            {
                var requestBody = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(requestBody);
                Console.WriteLine($" [x] [{ea.BasicProperties.CorrelationId}]: {message}\n");
                var filter = JsonSerializer.Deserialize<ListFilter>(requestBody);
                if (filter == null)
                {
                    Console.WriteLine(" Cannot deserialize request body");
                    return;
                }
                
                results = await scoresService.ListAsync(filter, ct);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                var responseJson = JsonSerializer.Serialize(results);
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                var responseProps = new BasicProperties()
                {
                    CorrelationId = ea.BasicProperties.CorrelationId,
                };

                await channel.BasicPublishAsync(exchange: RabbitConsts.GatherExchange, routingKey: ea.BasicProperties.ReplyTo ?? string.Empty,
                    mandatory: true, basicProperties: responseProps, body: responseBytes, cancellationToken: ct);
            }
        }
    }

    private static async Task<string> SetupExchangesAndQueue(CancellationToken ct, IChannel channel)
    {
        await channel.ExchangeDeclareAsync(exchange: RabbitConsts.ScatterExchange, type: ExchangeType.Fanout, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(exchange: RabbitConsts.GatherExchange, ExchangeType.Direct, cancellationToken: ct);
        
        var queueName = (await channel.QueueDeclareAsync(cancellationToken: ct)).QueueName;
        await channel.QueueBindAsync(queueName, RabbitConsts.ScatterExchange, "", cancellationToken: ct);
        return queueName;
    }
}