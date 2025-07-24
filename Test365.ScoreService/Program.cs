// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using StackExchange.Redis;
using Test365.Common;
using Test365.ScoreService;

var builder = new ConfigurationBuilder()
    .AddEnvironmentVariables();

var configuration = builder.Build();
var services = new ServiceCollection();
// it's in memory, so this is singleton 
services.AddSingleton<IDuplicateCheckingService, RedisDuplicateCheckingService>();
services.AddSingleton<IScoresRepository, ScoresRepository>();
services.AddSingleton<ScoresService>();

var redisConnectionString = configuration["ConnectionStrings:cache"];
Debug.Assert(redisConnectionString != null, nameof(redisConnectionString) + " != null");
var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
services.AddSingleton<IConnectionMultiplexer>(redis);
services.AddTransient<IDatabase>(x=> redis.GetDatabase());
await services.SetupRabbitAsync(configuration);

services.AddTransient<ScoreReceiver>();
services.AddTransient<ListReceiver>();
var serviceProvider = services.BuildServiceProvider();

var cts = new CancellationTokenSource();
Console.WriteLine("Starting listeners");
var scoreReceiver = serviceProvider.GetService<ScoreReceiver>()!;
var scoreTask = scoreReceiver.RunAsync(cts.Token);

var listReceiver = serviceProvider.GetService<ListReceiver>()!;
var listReceiverTask =listReceiver.RunAsync(cts.Token);

Console.WriteLine("Press enter to exit");
Console.ReadLine();
cts.Cancel();

Console.WriteLine("Stopping listeners");
await scoreTask.WaitAsync(TimeSpan.FromSeconds(5));
await listReceiverTask.WaitAsync(TimeSpan.FromSeconds(5));