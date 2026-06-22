using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.CollectionPoints.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Registro de retiradas (área privada). Aplica a regra de equidade municipal.</summary>
[Route("api/withdrawals")]
[Produces("application/json")]
public sealed class WithdrawalsController(ISender sender) : ApiController(sender)
{
    /// <summary>Registra uma retirada de uma família em um ponto de coleta.</summary>
    /// <remarks>
    /// Bloqueado (409) quando a família já atingiu o limite do ciclo vigente,
    /// considerando o histórico de TODOS os pontos do município.
    /// </remarks>
    /// <response code="201">Retirada registrada.</response>
    /// <response code="404">Família, beneficiário ou ponto não encontrado.</response>
    /// <response code="409">Limite de retiradas do ciclo atingido / ponto inativo.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(WithdrawalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterWithdrawalCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, value => Created($"/api/withdrawals/{value.Id}", value));
    }
}