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

const int maxScore = 5;
const int delayInSeconds = 1;
const int maxHoursBefore = 48;

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
            DateTime.UtcNow.AddHours(-rnd.Next(maxHoursBefore)),
            randomizer.Get(teams), 
            randomizer.Get(teams),
            rnd.Next(maxScore),
            rnd.Next(maxScore)));
    await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
}


async Task SendScoreAsync(IChannel ch, Score score)
{
    var json = JsonSerializer.Serialize(score, new JsonSerializerOptions() { WriteIndented = true });
    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
    await ch.BasicPublishAsync(string.Empty, routingKey: RabbitConsts.NewScoresQueue, mandatory: true, 
        basicProperties: properties, body: bytes);
    
    Console.WriteLine($" [x] Sent {json}");
}