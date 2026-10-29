using CartaoCredito.Application.Cartoes.Services;
using CartaoCredito.Application.Contracts;
using MassTransit;

namespace CartaoCredito.Infrastructure.Messaging.Consumers;

/// <summary>Adaptador fino: recebe a mensagem e delega ao caso de uso, sem regra de negócio.</summary>
internal sealed class PropostaGeradaConsumer(ICartaoService cartaoService) : IConsumer<PropostaGerada>
{
    public Task Consume(ConsumeContext<PropostaGerada> context) =>
        cartaoService.EmitirAsync(context.Message, context.CancellationToken);
}
