using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Corpo de uma requisição de edição de retirada.</summary>
public sealed record UpdateWithdrawalRequest(
    Guid PointId,
    IReadOnlyList<WithdrawalItemEntry> Items,
    string? Notes,
    Guid? BasketTemplateId = null,
    int? BasketQuantity = null);

/// <summary>Registro de retiradas (área privada). Aplica a regra de equidade municipal.</summary>
[Route("api/withdrawals")]
[Produces("application/json")]
public sealed class WithdrawalsController(ISender sender) : ApiController(sender)
{
    /// <summary>Registra uma retirada de um beneficiário em um ponto de coleta.</summary>
    /// <remarks>
    /// Bloqueado (409) quando o beneficiário já atingiu o limite do ciclo vigente, considerando o
    /// histórico de TODOS os pontos do município, ou quando está inativo/não vinculado ao ponto.
    /// <c>occurredAtUtc</c> é opcional (padrão: agora); não pode estar no futuro e, para gestores,
    /// não pode ser anterior a 7 dias.
    /// </remarks>
    /// <response code="201">Retirada registrada.</response>
    /// <response code="400">Dados inválidos ou data da retirada fora da janela permitida.</response>
    /// <response code="404">Beneficiário ou ponto não encontrado.</response>
    /// <response code="409">Limite do ciclo atingido, ponto inativo ou beneficiário inativo/não vinculado ao ponto.</response>
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

    /// <summary>Corrige os itens/observações de uma retirada já registrada.</summary>
    /// <remarks>
    /// Só é possível editar a retirada mais recente do beneficiário (considerando todos os pontos
    /// do município) — evita que um ajuste tardio destoe do estoque e do ciclo já consolidados.
    /// </remarks>
    /// <response code="200">Retirada atualizada.</response>
    /// <response code="404">Ponto ou retirada não encontrados.</response>
    /// <response code="409">Retirada não é mais a mais recente, item sem estoque, limite do ciclo atingido ou beneficiário inativo/não vinculado ao ponto.</response>
    [HttpPut("{withdrawalId:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(WithdrawalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid withdrawalId,
        [FromBody] UpdateWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWithdrawalCommand(
            request.PointId, withdrawalId, request.Items, request.Notes,
            request.BasketTemplateId, request.BasketQuantity);
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, Ok);
    }
}