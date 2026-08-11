using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.CollectionPoints.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Gestão de pontos de coleta (área privada).</summary>
[Route("api/collection-points")]
[Produces("application/json")]
public sealed class CollectionPointsController(ISender sender) : ApiController(sender)
{
    /// <summary>Lista todos os pontos de coleta (ativos e inativos) para a grid administrativa.</summary>
    /// <remarks>Requer a role <c>Admin</c>.</remarks>
    /// <response code="200">Lista de pontos.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão.</response>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<CollectionPointResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListCollectionPointsForManagementQuery(), cancellationToken);
        return Respond(result, Ok);
    }

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

    /// <summary>Atualiza um ponto de coleta existente.</summary>
    /// <remarks>Requer a role <c>Admin</c>. A imagem é opcional: informe <c>imageFileId</c> só para trocá-la.</remarks>
    /// <response code="200">Ponto atualizado.</response>
    /// <response code="400">Dados inválidos (endereço, contato, etc.).</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Sem permissão.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCollectionPointCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { Id = id }, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Atualiza o estoque (itens disponíveis) do ponto.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>.</remarks>
    /// <response code="200">Estoque atualizado.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpPut("{id:guid}/stock")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(
        Guid id,
        [FromBody] UpdateCollectionPointStockCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id }, cancellationToken);
        return Respond(result, Ok);
    }
}