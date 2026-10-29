using CartaoCredito.Application.Cartoes.Repositories;
using CartaoCredito.Domain.Cartoes;
using CartaoCredito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CartaoCredito.Infrastructure.Cartoes;

internal sealed class CartaoRepository(CartaoDbContext context) : ICartaoRepository
{
    public async Task<IReadOnlyList<Cartao>> ListarPorClienteIdAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Cartao>()
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .OrderBy(c => c.EmitidoEm)
            .ThenBy(c => c.Sequencial)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistemParaPropostaAsync(Guid propostaId, CancellationToken cancellationToken = default) =>
        context.Set<Cartao>().AnyAsync(c => c.PropostaId == propostaId, cancellationToken);

    public void Adicionar(IEnumerable<Cartao> cartoes) => context.Set<Cartao>().AddRange(cartoes);
}
