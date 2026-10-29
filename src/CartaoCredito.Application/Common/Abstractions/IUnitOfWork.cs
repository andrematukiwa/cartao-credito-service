namespace CartaoCredito.Application.Common.Abstractions;

/// <summary>Confirma numa única transação tudo o que foi alterado (entidades e Outbox).</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
