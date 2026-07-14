using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Reviews.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Moderação de avaliações (área privada — Admin).</summary>
[Route("api/reviews")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public sealed class ReviewsController(ISender sender) : ApiController(sender)
{
    /// <summary>Oculta uma avaliação do público.</summary>
    [HttpPost("{id:guid}/hide")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Hide(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new HideReviewCommand(id), cancellationToken);
        return Respond(result, _ => NoContent());
    }

    /// <summary>Reexibe uma avaliação previamente ocultada.</summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RestoreReviewCommand(id), cancellationToken);
        return Respond(result, _ => NoContent());
    }
}
