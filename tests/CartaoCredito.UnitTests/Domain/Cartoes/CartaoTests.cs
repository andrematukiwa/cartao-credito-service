using CartaoCredito.Domain.Cartoes;

namespace CartaoCredito.UnitTests.Domain.Cartoes;

public class CartaoTests
{
    private static readonly DateTime Agora = new(2026, 10, 29, 15, 0, 0, DateTimeKind.Utc);

    private sealed class GeradorFixo : IGeradorNumeroCartao
    {
        public string GerarFinal() => "1234";
    }

    [Fact]
    public void EmitirParaProposta_DeveEmitirDoisCartoesComLimiteIntegralCada_QuandoQuantidadeForDois()
    {
        // Act
        var cartoes = Cartao.EmitirParaProposta(
            Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", quantidade: 2, limitePorCartao: 5_000m, new GeradorFixo(), Agora);

        // Assert
        Assert.Equal(2, cartoes.Count);
        Assert.All(cartoes, c => Assert.Equal(5_000m, c.Limite));
        Assert.Equal([1, 2], cartoes.Select(c => c.Sequencial));
    }

    [Fact]
    public void EmitirParaProposta_DeveDefinirValidadeNoUltimoDiaDoMesDaquiACincoAnos()
    {
        // Act
        var cartao = Cartao.EmitirParaProposta(
            Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", quantidade: 1, limitePorCartao: 1_000m, new GeradorFixo(), Agora)[0];

        // Assert
        Assert.Equal(new DateOnly(2031, 10, 31), cartao.Validade);
        Assert.Equal("**** **** **** 1234", cartao.NumeroMascarado);
    }
}
