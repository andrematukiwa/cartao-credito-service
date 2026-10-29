using System.Security.Cryptography;

namespace CartaoCredito.Domain.Cartoes;

/// <summary>
/// Gera os 4 dígitos finais do cartão. Interface para os testes poderem fixar o valor.
/// <para>
/// Por PCI-DSS, o número completo (PAN) não é armazenado: num cenário real ele é gerado e
/// tokenizado pela processadora do cartão, e o sistema guarda só o token e os dígitos finais.
/// </para>
/// </summary>
public interface IGeradorNumeroCartao
{
    string GerarFinal();
}

public sealed class GeradorNumeroCartaoAleatorio : IGeradorNumeroCartao
{
    // RandomNumberGenerator (criptográfico) em vez de Random: número de cartão não deve ser previsível
    public string GerarFinal() => RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
}
