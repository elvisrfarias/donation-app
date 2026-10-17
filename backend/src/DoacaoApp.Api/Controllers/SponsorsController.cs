using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Sponsors.Commands;
using DoacaoApp.Application.Sponsors.Contracts;
using DoacaoApp.Application.Sponsors.Queries;
using DoacaoApp.Domain.Sponsors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>
/// Gestão dos patrocinadores mensais do projeto. Só o Admin cadastra, edita ou remove; a página pública lê
/// os ativos por <c>GET /api/public/sponsors</c>.
/// </summary>
[Route("api/sponsors")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public sealed class SponsorsController(ISender sender) : ApiController(sender)
{
    /// <summary>Lista todos os patrocinadores, ativos e inativos, do Diamante à Prata.</summary>
    /// <response code="200">Patrocinadores.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SponsorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListSponsorsQuery(), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Cadastra um patrocinador com a logo enviada antes por <c>POST /api/files</c>.</summary>
    /// <response code="201">Patrocinador cadastrado.</response>
    /// <response code="400">Dados inválidos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SponsorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSponsorCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, sponsor => Created($"api/sponsors/{sponsor.Id}", sponsor));
    }

    /// <summary>Atualiza nome, categoria, site, situação e (opcionalmente) a logo de um patrocinador.</summary>
    /// <response code="200">Patrocinador atualizado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Patrocinador não encontrado.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SponsorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSponsorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSponsorCommand(
            id, request.Name, request.Tier, request.WebsiteUrl, request.IsActive, request.LogoFileId);
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Remove um patrocinador de vez.</summary>
    /// <response code="204">Patrocinador removido.</response>
    /// <response code="404">Patrocinador não encontrado.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeleteSponsorCommand(id), cancellationToken);
        return Respond(result, _ => NoContent());
    }
}

/// <summary>Corpo do PUT de patrocinador (o Id vem na rota). <c>Tier</c>: Diamond, Gold ou Silver.</summary>
public sealed record UpdateSponsorRequest(
    string Name,
    SponsorTier Tier,
    string? WebsiteUrl,
    bool IsActive,
    Guid? LogoFileId);
