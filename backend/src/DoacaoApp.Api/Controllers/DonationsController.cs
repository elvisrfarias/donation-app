using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Donations.Commands;
using DoacaoApp.Application.Donations.Contracts;
using DoacaoApp.Application.Donations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Gestão das doações prometidas a um ponto de coleta (área privada).</summary>
[Route("api/collection-points/{pointId:guid}/donations")]
[Produces("application/json")]
[Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
public sealed class DonationsController(ISender sender) : ApiController(sender)
{
    /// <summary>Lista as doações do ponto, das mais recentes para as mais antigas.</summary>
    /// <response code="200">Doações do ponto.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DonationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(Guid pointId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListPointDonationsQuery(pointId), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Confirma o recebimento da doação e, se informado, soma os itens ao estoque.</summary>
    /// <response code="200">Doação recebida.</response>
    /// <response code="400">Quantidade inválida ou item de estoque inexistente.</response>
    /// <response code="404">Doação não encontrada neste ponto.</response>
    /// <response code="409">Doação já recebida ou cancelada.</response>
    [HttpPost("{donationId:guid}/receive")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receive(
        Guid pointId,
        Guid donationId,
        [FromBody] ReceiveDonationRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new ReceiveDonationCommand(pointId, donationId, request?.ReceivedStock);
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Cancela uma doação prometida.</summary>
    /// <response code="200">Doação cancelada.</response>
    /// <response code="404">Doação não encontrada neste ponto.</response>
    /// <response code="409">Doação já recebida ou cancelada.</response>
    [HttpPost("{donationId:guid}/cancel")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid pointId, Guid donationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CancelDonationCommand(pointId, donationId), cancellationToken);
        return Respond(result, Ok);
    }
}

/// <summary>Corpo opcional do recebimento: itens de estoque a somar.</summary>
public sealed record ReceiveDonationRequest(IReadOnlyList<StockQuantityEntry>? ReceivedStock);
