namespace CartaoCredito.Application.Common.Exceptions;

/// <summary>
/// Mensagem recebida com dados que nunca vão ser processáveis (ex.: renda zerada).
/// A política de retry ignora esta exceção: tentar de novo não resolve, então a mensagem
/// vai direto para a fila de erro (DLQ) para análise, sem gastar retentativas.
/// </summary>
public sealed class MensagemInvalidaException(string message) : AppException(message);
