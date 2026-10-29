using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CartaoCredito.Infrastructure.Persistence;

public static class MigrationExtensions
{
    /// <summary>
    /// Aplica as migrations pendentes. Usado no ambiente local/docker para o projeto subir com
    /// um único comando; em produção as migrations rodariam no pipeline de deploy.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CartaoDbContext>();
        await context.Database.MigrateAsync();
    }
}
