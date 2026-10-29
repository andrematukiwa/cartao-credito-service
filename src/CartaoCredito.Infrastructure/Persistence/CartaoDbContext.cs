using CartaoCredito.Application.Common.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CartaoCredito.Infrastructure.Persistence;

public sealed class CartaoDbContext(DbContextOptions<CartaoDbContext> options) : DbContext(options), IUnitOfWork
{
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CartaoDbContext).Assembly);

        // Inbox: descarta reentregas da mesma mensagem (MessageId já processado), na mesma
        // transação da emissão. As tabelas de Outbox são exigidas pelo mesmo mecanismo.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
