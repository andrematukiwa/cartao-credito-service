using CartaoCredito.Domain.Cartoes;

namespace CartaoCredito.Application.Cartoes.Repositories;

public interface ICartaoRepository
{
    Task<IReadOnlyList<Cartao>> ListarPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);

    Task<bool> ExistemParaPropostaAsync(Guid propostaId, CancellationToken cancellationToken = default);

    /// <summary>Marca os cartões para inclusão; a gravação acontece em <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    void Adicionar(IEnumerable<Cartao> cartoes);
}
