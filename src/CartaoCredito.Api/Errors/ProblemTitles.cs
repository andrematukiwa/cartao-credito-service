namespace CartaoCredito.Api.Errors;

/// <summary>Títulos em português dos ProblemDetails, por status HTTP: fonte única para toda a API.</summary>
internal static class ProblemTitles
{
    private static readonly Dictionary<int, string> Titulos = new()
    {
        [StatusCodes.Status400BadRequest] = "Requisição inválida",
        [StatusCodes.Status404NotFound] = "Recurso não encontrado",
        [StatusCodes.Status405MethodNotAllowed] = "Método não permitido",
        [StatusCodes.Status500InternalServerError] = "Erro interno"
    };

    public static string? Para(int status) => Titulos.GetValueOrDefault(status);
}
