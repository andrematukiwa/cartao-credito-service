using CartaoCredito.Application.Cartoes.Repositories;
using CartaoCredito.Application.Common.Abstractions;
using CartaoCredito.Infrastructure.Cartoes;
using CartaoCredito.Infrastructure.Messaging;
using CartaoCredito.Infrastructure.Messaging.Consumers;
using CartaoCredito.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CartaoCredito.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "CartaoDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddMessaging(configuration);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Falha ao subir se a connection string não estiver configurada (fail-fast)
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' não configurada (ConnectionStrings__{ConnectionStringName}).");

        services.AddDbContext<CartaoDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        // Mesma instância (por mensagem/requisição) do DbContext: repositório e Inbox na mesma transação
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CartaoDbContext>());
        services.AddScoped<ICartaoRepository, CartaoRepository>();

        services.AddHealthChecks()
            .AddDbContextCheck<CartaoDbContext>("postgres");
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<PropostaGeradaConsumer, PropostaGeradaConsumerDefinition>();

            bus.AddEntityFrameworkOutbox<CartaoDbContext>(outbox => outbox.UsePostgres());

            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMq = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(rabbitMq.Host, rabbitMq.Port, rabbitMq.VirtualHost, host =>
                {
                    host.Username(rabbitMq.Username);
                    host.Password(rabbitMq.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
