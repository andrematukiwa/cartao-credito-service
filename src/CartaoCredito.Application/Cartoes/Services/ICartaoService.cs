using CartaoCredito.Application.Cartoes.Dtos;
using CartaoCredito.Application.Contracts;

namespace CartaoCredito.Application.Cartoes.Services;

public interface ICartaoService
{
    /// <summary>Emite os cartões de uma proposta aprovada. Idempotente: reprocessar a mesma proposta não duplica.</summary>
    Task EmitirAsync(PropostaGerada evento, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CartaoResponse>> ListarPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
}
