using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CartaoCredito.Api.Errors;

/// <summary>
/// Garante um único formato de erro na API inteira: exceções, falhas de leitura do JSON e
/// respostas sem corpo (ex.: 404 de rota) saem todas como ProblemDetails em português.
/// </summary>
internal static class ErrorHandlingExtensions
{
    private const string MensagemFormatoInvalido = "Valor em formato inválido.";
    private const string MensagemCorpoInvalido = "O corpo da requisição é obrigatório e deve ser um JSON válido.";
    private const string CampoCorpo = "body";

    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            var status = problem.Status ?? context.HttpContext.Response.StatusCode;

            problem.Title = ProblemTitles.Para(status) ?? problem.Title;
            problem.Instance ??= context.HttpContext.Request.Path;
            problem.Extensions["traceId"] = TraceId(context.HttpContext);
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();

        // 400 gerado pelo próprio ASP.NET (JSON malformado, tipo errado, corpo ausente)
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = new ValidationProblemDetails(TraduzirErros(context.ModelState))
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Status = StatusCodes.Status400BadRequest,
                    Title = ProblemTitles.Para(StatusCodes.Status400BadRequest),
                    Detail = "Um ou mais campos são inválidos.",
                    Instance = context.HttpContext.Request.Path
                };
                problem.Extensions["traceId"] = TraceId(context.HttpContext);

                return new ObjectResult(problem)
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentTypes = { "application/problem+json" }
                };
            });

        return services;
    }

    /// <summary>
    /// Converte os erros do model binding para o mesmo formato da validação de negócio:
    /// "$.dataNascimento" vira "dataNascimento", e mensagens internas do serializador
    /// (que expõem nomes de tipos .NET) são trocadas por textos genéricos.
    /// </summary>
    private static Dictionary<string, string[]> TraduzirErros(ModelStateDictionary modelState)
    {
        var erros = modelState
            .Where(entrada => entrada.Value is { Errors.Count: > 0 })
            .Select(entrada => entrada.Key)
            .ToList();

        var errosDeCampo = erros
            .Where(chave => chave.StartsWith("$.", StringComparison.Ordinal))
            .ToDictionary(chave => chave[2..], _ => new[] { MensagemFormatoInvalido });

        // Sem erro de campo específico: o problema é o corpo como um todo (ausente ou malformado)
        if (errosDeCampo.Count == 0 && erros.Count > 0)
            errosDeCampo[CampoCorpo] = [MensagemCorpoInvalido];

        return errosDeCampo;
    }

    private static string TraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;
}
