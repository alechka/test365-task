using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Test365.Common;

namespace Test365.ApiService;

public class ScoreListService(IConnection connection, WorkerRegistry workerRegistry)
{
    public async Task ListAsync(ListFilter filter, CancellationToken cancellationToken = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(exchange: RabbbitConsts.ScatterExchange, type: ExchangeType.Fanout, cancellationToken: cancellationToken);
        
        var correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            //todo
            ReplyTo = "_replyQueueName"
        };
        
        //todo
        // var tcs = new TaskCompletionSource<string>(
        //     TaskCreationOptions.RunContinuationsAsynchronously);
        // _callbackMapper.TryAdd(correlationId, tcs);
        
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(filter));
        await channel.BasicPublishAsync(exchange: RabbbitConsts.ScatterExchange, routingKey: string.Empty,
            basicProperties: props, body: body, mandatory: true, cancellationToken: cancellationToken);
        
    }
}