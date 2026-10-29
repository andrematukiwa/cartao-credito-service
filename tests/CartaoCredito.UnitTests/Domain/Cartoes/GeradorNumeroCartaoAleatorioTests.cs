using CartaoCredito.Domain.Cartoes;

namespace CartaoCredito.UnitTests.Domain.Cartoes;

public class GeradorNumeroCartaoAleatorioTests
{
    [Fact]
    public void GerarFinal_DeveRetornarQuatroDigitos_IncluindoZerosAEsquerda()
    {
        // Arrange
        var gerador = new GeradorNumeroCartaoAleatorio();

        // Act: várias gerações para cobrir números pequenos (ex.: 7 → "0007")
        var finais = Enumerable.Range(0, 1_000).Select(_ => gerador.GerarFinal()).ToList();

        // Assert
        Assert.All(finais, final =>
        {
            Assert.Equal(Cartao.FinalNumeroTamanho, final.Length);
            Assert.True(final.All(char.IsAsciiDigit));
        });
    }
}
