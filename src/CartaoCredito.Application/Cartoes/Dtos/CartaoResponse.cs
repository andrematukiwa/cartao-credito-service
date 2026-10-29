using System.Globalization;
using CartaoCredito.Domain.Cartoes;

namespace CartaoCredito.Application.Cartoes.Dtos;

/// <param name="Validade">No formato impresso no cartão: MM/AAAA.</param>
public sealed record CartaoResponse(
    Guid Id,
    Guid PropostaId,
    string NomeTitular,
    string Numero,
    string Validade,
    decimal Limite,
    string Status,
    DateTime EmitidoEm)
{
    public static CartaoResponse De(Cartao cartao) => new(
        cartao.Id,
        cartao.PropostaId,
        cartao.NomeTitular,
        cartao.NumeroMascarado,
        cartao.Validade.ToString("MM/yyyy", CultureInfo.InvariantCulture),
        cartao.Limite,
        cartao.Status.ToString(),
        cartao.EmitidoEm);
}
