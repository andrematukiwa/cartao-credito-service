namespace CartaoCredito.Domain.Cartoes;

public sealed class Cartao
{
    /// <summary>O desafio permite no máximo 2 cartões por proposta.</summary>
    public const int QuantidadeMaximaPorProposta = 2;
    public const int ValidadeEmAnos = 5;
    public const int NomeTitularTamanhoMaximo = 150;
    public const int FinalNumeroTamanho = 4;
    public const int LimitePrecisao = 18;
    public const int LimiteCasasDecimais = 2;

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid PropostaId { get; private set; }
    public string NomeTitular { get; private set; } = string.Empty;

    /// <summary>1º ou 2º cartão da proposta: junto com a proposta, identifica o cartão de forma única.</summary>
    public int Sequencial { get; private set; }

    public string FinalNumero { get; private set; } = string.Empty;
    public DateOnly Validade { get; private set; }
    public decimal Limite { get; private set; }
    public StatusCartao Status { get; private set; }
    public DateTime EmitidoEm { get; private set; }

    public string NumeroMascarado => $"**** **** **** {FinalNumero}";

    private Cartao() { } // usado pelo EF Core

    /// <summary>
    /// Emite os cartões de uma proposta aprovada: <paramref name="quantidade"/> cartões, cada um com o
    /// limite integral da proposta (na faixa alta, 2 × R$ 5.000,00 = R$ 10.000,00 no total).
    /// </summary>
    public static IReadOnlyList<Cartao> EmitirParaProposta(
        Guid propostaId,
        Guid clienteId,
        string nomeTitular,
        int quantidade,
        decimal limitePorCartao,
        IGeradorNumeroCartao geradorNumero,
        DateTime emitidoEm)
    {
        ArgumentNullException.ThrowIfNull(geradorNumero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantidade, QuantidadeMaximaPorProposta);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limitePorCartao);

        var emissao = TruncarParaMicrossegundos(emitidoEm);

        return Enumerable.Range(1, quantidade)
            .Select(sequencial => new Cartao
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                PropostaId = propostaId,
                NomeTitular = nomeTitular.Trim(),
                Sequencial = sequencial,
                FinalNumero = geradorNumero.GerarFinal(),
                Validade = CalcularValidade(emissao),
                // Multiplicar por 1.00m fixa a escala em 2 casas, igual ao banco
                Limite = limitePorCartao * 1.00m,
                Status = StatusCartao.Ativo,
                EmitidoEm = emissao
            })
            .ToList();
    }

    /// <summary>Validade no último dia do mês, daqui a <see cref="ValidadeEmAnos"/> anos (como impresso: MM/AA).</summary>
    private static DateOnly CalcularValidade(DateTime emissao)
    {
        var mesDeVencimento = new DateOnly(emissao.Year, emissao.Month, 1).AddYears(ValidadeEmAnos);
        return mesDeVencimento.AddMonths(1).AddDays(-1);
    }

    // O Postgres guarda microssegundos: truncar evita divergência entre o que é gravado e o que é lido depois
    private static DateTime TruncarParaMicrossegundos(DateTime data) =>
        new(data.Ticks - data.Ticks % (TimeSpan.TicksPerMillisecond / 1000), DateTimeKind.Utc);
}
