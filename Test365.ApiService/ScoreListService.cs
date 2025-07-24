using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Test365.Common;

namespace Test365.ApiService;

public class ScoreListService
{
    private IConnection _connection;
    private WorkerRegistry _workerRegistry;
    private readonly string _replyQueueName = RabbitConsts.GatherQueue + Guid.NewGuid().ToString().Replace("-","").ToLower();
    private ConcurrentDictionary<Guid, TaskCompletionSource<IEnumerable<Score>>> _completionSources = new();
    
    public ScoreListService(IConnection connection, WorkerRegistry workerRegistry)
    {
        _connection = connection;
        _workerRegistry = workerRegistry;
    }

    public async Task StartAsync()
    {
        IChannel channel = await _connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange: RabbitConsts.GatherExchange, ExchangeType.Direct);
        await channel.QueueDeclareAsync(_replyQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null );
        // Setup reply queue
        await channel.QueueBindAsync(_replyQueueName, RabbitConsts.GatherExchange, _replyQueueName);
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += OnListResponseReceived;

        await channel.BasicConsumeAsync(_replyQueueName, true, consumer);
        
        async Task OnListResponseReceived(object model, BasicDeliverEventArgs ea)
        {
            try
            {
                if (Guid.TryParse(ea.BasicProperties.CorrelationId, out var correlationId) &&
                    _completionSources.TryRemove(correlationId, out var tcs))
                {
                    var body = ea.Body.ToArray();
                    var response = Encoding.UTF8.GetString(body);
                    var result = JsonSerializer.Deserialize<IEnumerable<Score>>(response);
                    if (result == null)
                    {
                        tcs.TrySetException(new Exception("Failed to deserialize response"));
                        return;
                    }

                    tcs.TrySetResult(result);
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
    
    public async Task ListAsync(ListFilter filter, CancellationToken ct = default)
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);
        await channel.ExchangeDeclareAsync(exchange: RabbitConsts.ScatterExchange, type: ExchangeType.Fanout, cancellationToken: ct);
        
        var correlationId = Guid.NewGuid();
        var props = new BasicProperties
        {
            CorrelationId = correlationId.ToString(),
            //todo
            ReplyTo = _replyQueueName
        };
        
        //todo
        var tcs = new TaskCompletionSource<IEnumerable<Score>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _completionSources.TryAdd(correlationId, tcs);
        
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(filter));
        await channel.BasicPublishAsync(exchange: RabbitConsts.ScatterExchange, routingKey: string.Empty,
            basicProperties: props, body: body, mandatory: true, cancellationToken: ct);
        
    }
}