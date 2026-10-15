using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Notices.Commands;
using DoacaoApp.Application.Notices.Contracts;
using DoacaoApp.Application.Notices.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Mural de avisos dos pontos de coleta (área privada): comunicação entre a rede de gestores.</summary>
[Route("api")]
[Produces("application/json")]
[Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
public sealed class NoticesController(ISender sender) : ApiController(sender)
{
    /// <summary>Mural da rede: avisos ativos de todos os pontos, dos mais recentes aos mais antigos.</summary>
    /// <response code="200">Avisos da rede.</response>
    [HttpGet("notices")]
    [ProducesResponseType(typeof(IReadOnlyList<NoticeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNetwork(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListNetworkNoticesQuery(), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Publica um aviso no mural do ponto (público ou só para gestores).</summary>
    /// <response code="201">Aviso publicado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    [HttpPost("collection-points/{pointId:guid}/notices")]
    [ProducesResponseType(typeof(NoticeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Publish(
        Guid pointId,
        [FromBody] PublishNoticeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PublishNoticeCommand(pointId, request.Title, request.Message, request.Audience);
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, notice => Created($"api/notices/{notice.Id}", notice));
    }

    /// <summary>Retira um aviso do mural (arquivamento lógico).</summary>
    /// <response code="204">Aviso arquivado.</response>
    /// <response code="404">Aviso não encontrado neste ponto.</response>
    [HttpDelete("collection-points/{pointId:guid}/notices/{noticeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid pointId, Guid noticeId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ArchiveNoticeCommand(pointId, noticeId), cancellationToken);
        return Respond(result, _ => NoContent());
    }
}

/// <summary>Corpo do POST de aviso (o ponto vem da rota). <c>Audience</c>: Public ou Managers.</summary>
public sealed record PublishNoticeRequest(string Title, string Message, string Audience);
