using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.CollectionPoints.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Gestão de pontos de coleta (área privada).</summary>
[Route("api/collection-points")]
[Produces("application/json")]
public sealed class CollectionPointsController(ISender sender) : ApiController(sender)
{
    /// <summary>Cadastra um novo ponto de coleta.</summary>
    /// <remarks>Requer a role <c>Admin</c>.</remarks>
    /// <response code="201">Ponto criado.</response>
    /// <response code="400">Dados inválidos (endereço, contato, etc.).</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCollectionPointCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, value => Created($"/api/collection-points/{value.Id}", value));
    }
}