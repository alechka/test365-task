using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Test365.Common;

namespace Test365.ApiService;

/// <summary>
/// Service to get list of scores from  Test365.ScoreService
/// </summary>
/// <remarks>In real world this should be in a different project, so  anything can use it</remarks>
public class ScoreListService(IConnection connection, WorkerRegistry workerRegistry)
{
    private readonly string _replyQueueName = RabbitConsts.GatherQueue + Guid.NewGuid().ToString().Replace("-","").ToLower();
    private readonly ConcurrentDictionary<Guid, ListRequestHandler> _completionSources = new();

    public async Task StartAsync()
    {
        var channel = await SetupReplyQueue();
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += OnListResponseReceived;
        await channel.BasicConsumeAsync(_replyQueueName, false, consumer);
        return;

        async Task OnListResponseReceived(object model, BasicDeliverEventArgs ea)
        {
            try
            {
                if (Guid.TryParse(ea.BasicProperties.CorrelationId, out var correlationId) &&
                    _completionSources.TryGetValue(correlationId, out var handler))
                {
                    var body = ea.Body.ToArray();
                    var response = Encoding.UTF8.GetString(body);
                    var result = JsonSerializer.Deserialize<IEnumerable<Score>>(response);
                    if (result != null)
                    {
                        foreach (var score in result)
                        {
                            handler.Responses.Add(score);
                        }
                    }

                    Interlocked.Decrement(ref handler.NumberOfResponses);
                    if (handler.NumberOfResponses == 0)
                    {
                        handler.TaskCompletionSource.SetResult(handler.Responses);
                        _completionSources.TryRemove(correlationId, out _);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }
    }
    
    public async Task<IEnumerable<Score>> ListAsync(ListFilter filter, CancellationToken ct = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.ExchangeDeclareAsync(exchange: RabbitConsts.ScatterExchange, type: ExchangeType.Fanout, cancellationToken: ct);
        
        var correlationId = Guid.NewGuid();
        var props = new BasicProperties
        {
            CorrelationId = correlationId.ToString(),
            ReplyTo = _replyQueueName
        };
        
        //todo process cancel & timeouts
        var listRequest = new ListRequestHandler() { NumberOfResponses = workerRegistry.GetNumberOfWorkers() };
        _completionSources.TryAdd(correlationId, listRequest);
        
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(filter));
        await channel.BasicPublishAsync(exchange: RabbitConsts.ScatterExchange, routingKey: string.Empty,
            basicProperties: props, body: body, mandatory: true, cancellationToken: ct);
        
        return await listRequest.TaskCompletionSource.Task;
    } 
    
    private async Task<IChannel> SetupReplyQueue()
    {
        var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange: RabbitConsts.GatherExchange, ExchangeType.Direct);
        await channel.QueueDeclareAsync(_replyQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null );
        // Setup reply queue
        await channel.QueueBindAsync(_replyQueueName, RabbitConsts.GatherExchange, _replyQueueName);
        return channel;
    }

}