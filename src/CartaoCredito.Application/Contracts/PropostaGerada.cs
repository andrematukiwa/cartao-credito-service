using MassTransit;

namespace CartaoCredito.Application.Contracts;

/// <summary>
/// Evento consumido: publicado pela Proposta de Crédito para toda proposta, aprovada ou negada.
/// <para>
/// Cópia local do contrato, sem pacote compartilhado entre os serviços. O que liga as duas
/// pontas é a URN/exchange "proposta-gerada" e os nomes dos campos, não o namespace C#.
/// </para>
/// </summary>
[MessageUrn("proposta-gerada")]
[EntityName("proposta-gerada")]
public sealed record PropostaGerada(
    Guid PropostaId,
    Guid ClienteId,
    string NomeCliente,
    int Score,
    string Status,
    int QuantidadeCartoes,
    decimal LimitePorCartao,
    DateTime GeradaEm)
{
    public const string StatusAprovada = "Aprovada";
    public const string StatusNegada = "Negada";

    public bool Aprovada => Status == StatusAprovada;
}
