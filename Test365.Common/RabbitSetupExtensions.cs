using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Test365.Common;

public static class RabbitSetupExtensions
{
    public static async Task<IServiceCollection> SetupRabbitAsync(this IServiceCollection services, IConfiguration configuration)
    {
        var factory = new ConnectionFactory { Uri = new Uri(configuration["ConnectionStrings:messaging"]!) };
        var connection = await factory.CreateConnectionAsync();
        services.AddSingleton(connection);
        return services;
    }
}