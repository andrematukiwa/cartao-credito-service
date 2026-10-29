using CartaoCredito.Application.Common.Exceptions;
using CartaoCredito.Infrastructure.Persistence;
using MassTransit;

namespace CartaoCredito.Infrastructure.Messaging.Consumers;

/// <summary>
/// Resiliência do consumo de PropostaGerada:
/// <list type="bullet">
///   <item><b>Retry exponencial</b>: 5 retentativas com intervalos crescentes (de 1s até no máximo 30s)
///   para falhas transitórias, como o banco fora do ar por alguns segundos.</item>
///   <item><b>Dead-letter queue</b>: esgotadas as retentativas, o MassTransit move a mensagem para
///   <c>cartao-proposta-gerada_error</c>, com o motivo da falha nos headers. Nada se perde, e a
///   mensagem pode ser reprocessada depois.</item>
///   <item><b>Mensagem inválida</b> não é retentada: vai direto para a DLQ.</item>
///   <item><b>Inbox</b>: reentregas da mesma mensagem são descartadas.</item>
/// </list>
/// </summary>
internal sealed class PropostaGeradaConsumerDefinition : ConsumerDefinition<PropostaGeradaConsumer>
{
    public const string Fila = "cartao-proposta-gerada";

    public PropostaGeradaConsumerDefinition() => EndpointName = Fila;

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PropostaGeradaConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Exponential(
                retryLimit: 5,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(30),
                intervalDelta: TimeSpan.FromSeconds(2));

            retry.Ignore<MensagemInvalidaException>();
        });

        endpointConfigurator.UseEntityFrameworkOutbox<CartaoDbContext>(context);
    }
}
