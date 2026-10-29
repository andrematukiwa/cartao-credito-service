namespace CartaoCredito.Application.Common.Exceptions;

/// <summary>
/// Base das exceções de negócio. O handler global da API traduz cada tipo para um status HTTP,
/// então a camada de aplicação nunca precisa conhecer HTTP.
/// </summary>
public abstract class AppException(string message) : Exception(message);
