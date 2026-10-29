using CartaoCredito.Domain.Cartoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CartaoCredito.Infrastructure.Cartoes;

internal sealed class CartaoConfiguration : IEntityTypeConfiguration<Cartao>
{
    public void Configure(EntityTypeBuilder<Cartao> builder)
    {
        builder.ToTable("cartoes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClienteId).IsRequired();
        builder.Property(c => c.PropostaId).IsRequired();
        builder.Property(c => c.NomeTitular).HasMaxLength(Cartao.NomeTitularTamanhoMaximo).IsRequired();
        builder.Property(c => c.Sequencial).IsRequired();
        builder.Property(c => c.FinalNumero).HasMaxLength(Cartao.FinalNumeroTamanho).IsFixedLength().IsRequired();
        builder.Property(c => c.Validade).IsRequired();
        builder.Property(c => c.Limite)
            .HasPrecision(Cartao.LimitePrecisao, Cartao.LimiteCasasDecimais)
            .IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.EmitidoEm).IsRequired();

        builder.Ignore(c => c.NumeroMascarado);

        // 1º e 2º cartão de cada proposta são únicos: garante a idempotência mesmo com processamento concorrente
        builder.HasIndex(c => new { c.PropostaId, c.Sequencial }).IsUnique();

        // Consulta principal da API: cartões de um cliente
        builder.HasIndex(c => c.ClienteId);
    }
}
