using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CartaoCredito.Api.Errors;

/// <summary>
/// Traduz exceções não tratadas em ProblemDetails (RFC 9457) com status 500, sem expor detalhes
/// internos. Esta API não tem erros de negócio: a única consulta devolve uma lista, vazia ou não.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var path = httpContext.Request.Path.ToString();

        // O cliente desistiu da requisição: não é falha do servidor, então nada de log de erro
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequisicaoCancelada(logger, path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        LogErroNaoTratado(logger, exception, path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Título, instance e traceId são preenchidos em ErrorHandlingExtensions (CustomizeProblemDetails)
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Ocorreu um erro inesperado."
            },
            Exception = exception
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro não tratado em {Path}")]
    private static partial void LogErroNaoTratado(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Requisição cancelada pelo cliente em {Path}")]
    private static partial void LogRequisicaoCancelada(ILogger logger, string path);
}
