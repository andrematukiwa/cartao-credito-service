using CartaoCredito.Application.Cartoes.Repositories;
using CartaoCredito.Application.Cartoes.Services;
using CartaoCredito.Application.Common.Abstractions;
using CartaoCredito.Application.Common.Exceptions;
using CartaoCredito.Application.Contracts;
using CartaoCredito.Domain.Cartoes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CartaoCredito.UnitTests.Application.Cartoes.Services;

public class CartaoServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 29, 15, 0, 0, TimeSpan.Zero);

    private readonly ICartaoRepository _repository = Substitute.For<ICartaoRepository>();
    private readonly IGeradorNumeroCartao _geradorNumero = Substitute.For<IGeradorNumeroCartao>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CartaoService _service;

    // xUnit cria uma instância nova da classe por teste: os mocks nunca vazam entre testes
    public CartaoServiceTests()
    {
        _geradorNumero.GerarFinal().Returns("1234");

        _service = new CartaoService(
            _repository,
            _geradorNumero,
            _unitOfWork,
            new FakeTimeProvider(Agora),
            NullLogger<CartaoService>.Instance);
    }

    private static PropostaGerada PropostaAprovada(int quantidadeCartoes = 1, decimal limitePorCartao = 1_000m) => new(
        PropostaId: Guid.NewGuid(),
        ClienteId: Guid.NewGuid(),
        NomeCliente: "Maria Silva",
        Score: 500,
        Status: PropostaGerada.StatusAprovada,
        QuantidadeCartoes: quantidadeCartoes,
        LimitePorCartao: limitePorCartao,
        GeradaEm: Agora.UtcDateTime);

    // Mensagens que nenhuma retentativa corrige: cada uma altera um único campo de uma proposta válida
    public static TheoryData<string, PropostaGerada> MensagensInvalidas => new()
    {
        { "sem PropostaId", PropostaAprovada() with { PropostaId = Guid.Empty } },
        { "sem ClienteId", PropostaAprovada() with { ClienteId = Guid.Empty } },
        { "status desconhecido", PropostaAprovada() with { Status = "Pendente" } },
        { "aprovada sem cartões", PropostaAprovada(quantidadeCartoes: 0) },
        { "aprovada com 3 cartões", PropostaAprovada(quantidadeCartoes: 3) },
        { "aprovada sem limite", PropostaAprovada(limitePorCartao: 0m) },
        { "sem nome do titular", PropostaAprovada() with { NomeCliente = "  " } },
        { "nome acima de 150 caracteres", PropostaAprovada() with { NomeCliente = new string('a', 151) } },
    };

    [Fact]
    public async Task EmitirAsync_DeveEmitirUmCartaoDeMilReais_QuandoPropostaForDaFaixaIntermediaria()
    {
        // Arrange
        var proposta = PropostaAprovada(quantidadeCartoes: 1, limitePorCartao: 1_000m);

        // Act
        await _service.EmitirAsync(proposta);

        // Assert
        _repository.Received(1).Adicionar(Arg.Is<IEnumerable<Cartao>>(cartoes =>
            cartoes.Count() == 1
            && cartoes.All(c => c.Limite == 1_000m && c.PropostaId == proposta.PropostaId && c.ClienteId == proposta.ClienteId)));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmitirAsync_DeveEmitirDoisCartoesComLimiteIntegralCada_QuandoPropostaForDaFaixaAlta()
    {
        // Arrange
        var proposta = PropostaAprovada(quantidadeCartoes: 2, limitePorCartao: 5_000m);

        // Act
        await _service.EmitirAsync(proposta);

        // Assert: R$ 5.000,00 em cada cartão, R$ 10.000,00 no total
        _repository.Received(1).Adicionar(Arg.Is<IEnumerable<Cartao>>(cartoes =>
            cartoes.Count() == 2 && cartoes.Sum(c => c.Limite) == 10_000m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmitirAsync_NaoDeveEmitirCartoes_QuandoPropostaForNegada()
    {
        // Arrange: proposta negada chega com 0 cartões e sem limite, o que é válido
        var proposta = PropostaAprovada() with
        {
            Score = 100,
            Status = PropostaGerada.StatusNegada,
            QuantidadeCartoes = 0,
            LimitePorCartao = 0m
        };

        // Act
        await _service.EmitirAsync(proposta);

        // Assert
        _repository.DidNotReceive().Adicionar(Arg.Any<IEnumerable<Cartao>>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmitirAsync_NaoDeveDuplicarCartoes_QuandoPropostaJaTiverCartoesEmitidos()
    {
        // Arrange: o mesmo evento republicado depois de já ter sido processado
        var proposta = PropostaAprovada(quantidadeCartoes: 2, limitePorCartao: 5_000m);
        _repository.ExistemParaPropostaAsync(proposta.PropostaId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _service.EmitirAsync(proposta);

        // Assert
        _repository.DidNotReceive().Adicionar(Arg.Any<IEnumerable<Cartao>>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(MensagensInvalidas))]
    public async Task EmitirAsync_DeveLancarMensagemInvalida_QuandoEventoTiverDadosInvalidos(string cenario, PropostaGerada proposta)
    {
        // Arrange: "cenario" só nomeia o caso no relatório de testes
        _ = cenario;

        // Act
        var acao = () => _service.EmitirAsync(proposta);

        // Assert: a exceção faz a mensagem ir direto para a DLQ, sem retry e sem tocar no banco
        await Assert.ThrowsAsync<MensagemInvalidaException>(acao);
        await _repository.DidNotReceive().ExistemParaPropostaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListarPorClienteIdAsync_DeveDevolverCartoesMascaradosComValidadeMesAno()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var cartoes = Cartao.EmitirParaProposta(
            Guid.NewGuid(), clienteId, "Maria Silva", quantidade: 2, limitePorCartao: 5_000m, _geradorNumero, Agora.UtcDateTime);
        _repository.ListarPorClienteIdAsync(clienteId, Arg.Any<CancellationToken>()).Returns(cartoes);

        // Act
        var resposta = await _service.ListarPorClienteIdAsync(clienteId);

        // Assert
        Assert.Equal(2, resposta.Count);
        Assert.All(resposta, c =>
        {
            Assert.Equal("**** **** **** 1234", c.Numero);
            Assert.Equal("10/2031", c.Validade);
            Assert.Equal("Ativo", c.Status);
        });
    }

    [Fact]
    public async Task ListarPorClienteIdAsync_DeveDevolverListaVazia_QuandoClienteNaoTiverCartoes()
    {
        // Arrange
        _repository.ListarPorClienteIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var resposta = await _service.ListarPorClienteIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(resposta);
    }
}
