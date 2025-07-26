using Test365.Common;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder
        .AddRedis("cache")
        .WithRedisCommander();

var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "password");
var rabbitmq = builder.AddRabbitMQ("messaging", username, password)
    .WithManagementPlugin()
    .WithDataVolume(isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.Test365_ApiService>("apiservice")
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar";
        url.Url = "/scalar";

    })
    .WaitFor(rabbitmq)
    .WithReference(rabbitmq)
    .WithReplicas(2)
    .WithHttpHealthCheck("/health");

var publisher = builder.AddProject<Projects.Test365_PublisherConsole>("pub-console")
    .WaitFor(rabbitmq)
    .WithReference(rabbitmq);

var scoreService = builder.AddProject<Projects.Test365_ScoreService>("score-service")
    .WaitFor(rabbitmq)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WithReplicas((new WorkerRegistry().GetNumberOfWorkers()));


// builder.AddProject<Projects.Test365_Web>("webfrontend")
//     .WithExternalHttpEndpoints()
//     .WithHttpHealthCheck("/health")
//     .WithReference(cache)
//     .WaitFor(cache)
//     .WithReference(apiService)
//     .WaitFor(apiService);
//
builder.Build().Run();
