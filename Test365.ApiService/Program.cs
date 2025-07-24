using RabbitMQ.Client;
using Scalar.AspNetCore;
using Test365.ApiService;
using Test365.Common;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//await builder.Services.SetupRabbitAsync(configuration);
builder.Services.AddSingleton<ScoreListService>();
builder.Services.AddSingleton<WorkerRegistry>();
builder.Services.AddHealthChecks();
await builder.Services.SetupRabbitAsync(builder.Configuration);
var app = builder.Build();

await app.Services.GetService<ScoreListService>()!.StartAsync();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Servers = [];
    });
}

app.MapGet("/list", async ([AsParameters]ListFilter list, ScoreListService service, CancellationToken cancellationToken) =>
{
    await service.ListAsync(list, cancellationToken);
    return list;
});

app.MapDefaultEndpoints();

app.Run();
