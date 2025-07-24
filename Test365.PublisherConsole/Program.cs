using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Test365.Common;
using Test365.PublisherConsole;

var builder = new ConfigurationBuilder()
    .AddEnvironmentVariables();

var configuration = builder.Build();
var services = new ServiceCollection();
services.AddSingleton<IRandomFromArray, RandomFromArray>();
var serviceProvider = services.BuildServiceProvider();

var factory = new ConnectionFactory { Uri = new Uri(configuration["ConnectionStrings:messaging"]!) };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: RabbitConsts.NewScoresQueue, durable: true, exclusive: false,
    autoDelete: false, arguments: null);

var rnd = new Random();

string[] sports = ["Soccer", "Football", "Basketball", "Curling", "Hockey", "Volleyball"];
string[] teams =
[
    "Los Angeles Lakers", "Boston Celtics", "Chicago Bulls", "Arsenal", "Beitar", "Maccabi", "CSKA", "Dinamo", "Ajax",
    "Chelsea", "Barcelona", "Valencia", "El Salvador", "Miami Dolphins", "Buffalo Bills"
];

var properties = new BasicProperties
{
    Persistent = true,
    DeliveryMode = DeliveryModes.Persistent
};

var randomizer = serviceProvider.GetService<IRandomFromArray>()!;
while (true)
{
    await SendScoreAsync(channel,
        new Score(randomizer.Get(sports),
            DateTime.UtcNow.AddHours(-rnd.Next(48)),
            randomizer.Get(teams), 
            randomizer.Get(teams),
            rnd.Next(5),
            rnd.Next(5)));
    await Task.Delay(TimeSpan.FromSeconds(1));
}


async Task SendScoreAsync(IChannel channel, Score score)
{
    var json = JsonSerializer.Serialize(score, new JsonSerializerOptions() { WriteIndented = true });
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
    await channel.BasicPublishAsync(string.Empty, routingKey: RabbitConsts.NewScoresQueue, mandatory: true, 
        basicProperties: properties, body: bytes);
    
    Console.WriteLine($" [x] Sent {json}");
}