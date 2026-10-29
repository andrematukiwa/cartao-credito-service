using CartaoCredito.Application.Cartoes.Dtos;
using CartaoCredito.Application.Cartoes.Services;
using Microsoft.AspNetCore.Mvc;

namespace CartaoCredito.Api.Controllers;

[ApiController]
[Route("api/v1/cartoes")]
public sealed class CartoesController(ICartaoService cartaoService) : ControllerBase
{
    /// <summary>
    /// Lista os cartões do cliente. Lista vazia quando a proposta foi negada ou ainda está em
    /// processamento: os cartões são emitidos de forma assíncrona (consistência eventual).
    /// </summary>
    [HttpGet("{clienteId:guid}")]
    [ProducesResponseType<IReadOnlyList<CartaoResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<CartaoResponse>>> ListarPorClienteId(
        Guid clienteId,
        CancellationToken cancellationToken) =>
        Ok(await cartaoService.ListarPorClienteIdAsync(clienteId, cancellationToken));
}
