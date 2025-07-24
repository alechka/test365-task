using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Test365.Common;

namespace Test365.ScoreService;

internal class ScoreReceiver(ScoresService scoresService, IConnection connection)
{
    public async Task RunAsync(CancellationToken ct)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(queue: RabbbitConsts.NewScoresQueue, durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: ct);
        
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: ct);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += ScoreMessageHandler;
        await channel.BasicConsumeAsync(RabbbitConsts.NewScoresQueue, autoAck: false, consumer: consumer, cancellationToken: ct);
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        }
        consumer.ReceivedAsync -= ScoreMessageHandler;
        
        async Task ScoreMessageHandler(object model, BasicDeliverEventArgs ea)
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [got] {message}");
                var score = JsonSerializer.Deserialize<Score>(message);
                if (score == null)
                {
                    throw new ArgumentException($"Cannot deserialize message {message}");
                }

                //emulating like it's slow
                await Task.Delay(TimeSpan.FromMicroseconds(new Random().Next(0, 600)));
                await scoresService.SaveAsync(score, CancellationToken.None);
                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }
}