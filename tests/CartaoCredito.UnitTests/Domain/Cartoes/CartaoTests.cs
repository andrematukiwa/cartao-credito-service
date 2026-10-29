using System.Globalization;
using CartaoCredito.Domain.Cartoes;

namespace CartaoCredito.UnitTests.Domain.Cartoes;

public class CartaoTests
{
    private static readonly DateTime Agora = new(2026, 10, 29, 15, 0, 0, DateTimeKind.Utc);

    private sealed class GeradorFixo : IGeradorNumeroCartao
    {
        public string GerarFinal() => "1234";
    }

    private static IReadOnlyList<Cartao> Emitir(
        int quantidade = 1,
        decimal limitePorCartao = 1_000m,
        DateTime? emitidoEm = null) =>
        Cartao.EmitirParaProposta(
            Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", quantidade, limitePorCartao, new GeradorFixo(), emitidoEm ?? Agora);

    [Fact]
    public void EmitirParaProposta_DeveEmitirDoisCartoesComLimiteIntegralCada_QuandoQuantidadeForDois()
    {
        // Act
        var cartoes = Emitir(quantidade: 2, limitePorCartao: 5_000m);

        // Assert
        Assert.Equal(2, cartoes.Count);
        Assert.All(cartoes, c => Assert.Equal(5_000m, c.Limite));
        Assert.Equal([1, 2], cartoes.Select(c => c.Sequencial));
    }

    [Fact]
    public void EmitirParaProposta_DeveDefinirValidadeNoUltimoDiaDoMesDaquiACincoAnos()
    {
        // Act
        var cartao = Emitir()[0];

        // Assert
        Assert.Equal(new DateOnly(2031, 10, 31), cartao.Validade);
        Assert.Equal("**** **** **** 1234", cartao.NumeroMascarado);
    }

    [Fact]
    public void EmitirParaProposta_DeveConsiderarAnoBissexto_QuandoEmissaoForEmFevereiro()
    {
        // Arrange: a validade cai em 2032, ano bissexto, então fevereiro termina no dia 29
        var emissao = new DateTime(2027, 2, 10, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var cartao = Emitir(emitidoEm: emissao)[0];

        // Assert
        Assert.Equal(new DateOnly(2032, 2, 29), cartao.Validade);
    }

    [Fact]
    public void EmitirParaProposta_DeveCriarCartaoAtivoComDadosDaProposta()
    {
        // Arrange
        var propostaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();

        // Act
        var cartao = Cartao.EmitirParaProposta(
            propostaId, clienteId, "  Maria Silva  ", quantidade: 1, limitePorCartao: 1_000m, new GeradorFixo(), Agora)[0];

        // Assert
        Assert.Equal(propostaId, cartao.PropostaId);
        Assert.Equal(clienteId, cartao.ClienteId);
        Assert.Equal("Maria Silva", cartao.NomeTitular);
        Assert.Equal(StatusCartao.Ativo, cartao.Status);
        Assert.NotEqual(Guid.Empty, cartao.Id);
    }

    [Fact]
    public void EmitirParaProposta_DeveGravarLimiteComDuasCasasDecimais()
    {
        // Act
        var cartao = Emitir(limitePorCartao: 1_000m)[0];

        // Assert: decimal compara só o valor (1000m == 1000.00m), por isso a escala é conferida pela string
        Assert.Equal("1000.00", cartao.Limite.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void EmitirParaProposta_DeveTruncarDataDeEmissaoEmMicrossegundos()
    {
        // Arrange: 5 ticks = 500 nanossegundos, abaixo da precisão do Postgres
        var emissao = Agora.AddTicks(5);

        // Act
        var cartao = Emitir(emitidoEm: emissao)[0];

        // Assert
        Assert.Equal(Agora, cartao.EmitidoEm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void EmitirParaProposta_DeveLancarExcecao_QuandoQuantidadeEstiverForaDoLimite(int quantidade)
    {
        // Act
        var acao = () => Emitir(quantidade: quantidade);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(acao);
    }

    [Fact]
    public void EmitirParaProposta_DeveLancarExcecao_QuandoLimiteForZero()
    {
        // Act
        var acao = () => Emitir(limitePorCartao: 0m);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(acao);
    }
}
