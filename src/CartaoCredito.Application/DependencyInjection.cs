using CartaoCredito.Application.Cartoes.Services;
using CartaoCredito.Domain.Cartoes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CartaoCredito.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IGeradorNumeroCartao, GeradorNumeroCartaoAleatorio>();
        services.AddScoped<ICartaoService, CartaoService>();

        return services;
    }
}
