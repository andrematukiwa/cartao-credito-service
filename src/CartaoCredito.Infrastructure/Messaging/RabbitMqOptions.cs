using System.ComponentModel.DataAnnotations;

namespace CartaoCredito.Infrastructure.Messaging;

/// <summary>
/// Conexão com o RabbitMQ. Sem valores padrão para host e credenciais: se a configuração
/// estiver faltando, a aplicação falha ao subir em vez de tentar "localhost/guest" em silêncio.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public ushort Port { get; init; } = 5672;

    [Required]
    public string VirtualHost { get; init; } = "/";

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
