using CartaoCredito.Application.Cartoes.Dtos;
using CartaoCredito.Application.Cartoes.Repositories;
using CartaoCredito.Application.Common.Abstractions;
using CartaoCredito.Application.Common.Exceptions;
using CartaoCredito.Application.Contracts;
using CartaoCredito.Domain.Cartoes;
using Microsoft.Extensions.Logging;

namespace CartaoCredito.Application.Cartoes.Services;

public sealed partial class CartaoService(
    ICartaoRepository repository,
    IGeradorNumeroCartao geradorNumero,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CartaoService> logger) : ICartaoService
{
    public async Task EmitirAsync(PropostaGerada evento, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evento);
        ValidarMensagem(evento);

        if (!evento.Aprovada)
        {
            LogPropostaNegada(logger, evento.PropostaId, evento.ClienteId);
            return;
        }

        // Idempotência de negócio: a mesma proposta nunca gera cartões duas vezes, mesmo que o evento
        // seja republicado. (Reentregas da mesma mensagem já são barradas pelo Inbox do MassTransit.)
        if (await repository.ExistemParaPropostaAsync(evento.PropostaId, cancellationToken))
        {
            LogCartoesJaEmitidos(logger, evento.PropostaId);
            return;
        }

        var cartoes = Cartao.EmitirParaProposta(
            evento.PropostaId,
            evento.ClienteId,
            evento.NomeCliente,
            evento.QuantidadeCartoes,
            evento.LimitePorCartao,
            geradorNumero,
            timeProvider.GetUtcNow().UtcDateTime);

        repository.Adicionar(cartoes);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogCartoesEmitidos(logger, cartoes.Count, evento.PropostaId, evento.ClienteId, evento.LimitePorCartao);
    }

    public async Task<IReadOnlyList<CartaoResponse>> ListarPorClienteIdAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default)
    {
        var cartoes = await repository.ListarPorClienteIdAsync(clienteId, cancellationToken);
        return cartoes.Select(CartaoResponse.De).ToList();
    }

    /// <summary>Dados que nenhuma retentativa vai corrigir: vão direto para a DLQ.</summary>
    private static void ValidarMensagem(PropostaGerada evento)
    {
        if (evento.PropostaId == Guid.Empty || evento.ClienteId == Guid.Empty)
            throw new MensagemInvalidaException("PropostaGerada sem PropostaId ou ClienteId.");

        if (evento.Status is not (PropostaGerada.StatusAprovada or PropostaGerada.StatusNegada))
            throw new MensagemInvalidaException($"PropostaGerada {evento.PropostaId} com status desconhecido: '{evento.Status}'.");

        if (!evento.Aprovada)
            return;

        if (evento.QuantidadeCartoes is < 1 or > Cartao.QuantidadeMaximaPorProposta)
            throw new MensagemInvalidaException(
                $"PropostaGerada {evento.PropostaId} aprovada com quantidade de cartões inválida: {evento.QuantidadeCartoes}.");

        if (evento.LimitePorCartao <= 0)
            throw new MensagemInvalidaException($"PropostaGerada {evento.PropostaId} aprovada sem limite.");

        if (string.IsNullOrWhiteSpace(evento.NomeCliente) || evento.NomeCliente.Trim().Length > Cartao.NomeTitularTamanhoMaximo)
            throw new MensagemInvalidaException($"PropostaGerada {evento.PropostaId} com nome do titular inválido.");
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "{Quantidade} cartão(ões) emitido(s) para a proposta {PropostaId} do cliente {ClienteId}, limite {Limite} cada")]
    private static partial void LogCartoesEmitidos(ILogger logger, int quantidade, Guid propostaId, Guid clienteId, decimal limite);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Proposta {PropostaId} do cliente {ClienteId} negada: nenhum cartão emitido")]
    private static partial void LogPropostaNegada(ILogger logger, Guid propostaId, Guid clienteId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Cartões da proposta {PropostaId} já emitidos; evento ignorado (idempotência)")]
    private static partial void LogCartoesJaEmitidos(ILogger logger, Guid propostaId);
}
