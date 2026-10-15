using DoacaoApp.Api.Common;
using DoacaoApp.Application.Activity.Contracts;
using DoacaoApp.Application.Activity.Queries;
using DoacaoApp.Application.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Feed de atividade do ponto (área privada): mecanismo de percepção do modelo 3C.</summary>
[Route("api/collection-points/{pointId:guid}/activity")]
[Produces("application/json")]
[Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
public sealed class ActivityController(ISender sender) : ApiController(sender)
{
    /// <summary>Doações, avisos, avaliações e alertas de estoque recentes do ponto.</summary>
    /// <response code="200">Feed do ponto.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ActivityFeedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid pointId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPointActivityFeedQuery(pointId), cancellationToken);
        return Respond(result, Ok);
    }
}
