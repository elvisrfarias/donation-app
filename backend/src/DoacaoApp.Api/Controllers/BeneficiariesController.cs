using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Beneficiaries.Commands;
using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.Beneficiaries.Queries;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Domain.Beneficiaries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Corpo de uma requisição de cadastro de beneficiário.</summary>
public sealed record RegisterBeneficiaryRequest(
    string Name,
    string Cpf,
    string Email,
    IReadOnlyList<string>? Phones,
    DateOnly? BirthDate,
    Guid RegisteredByPointId);

/// <summary>Corpo de uma requisição de atualização de beneficiário.</summary>
public sealed record UpdateBeneficiaryRequest(
    string Name,
    string Cpf,
    string Email,
    IReadOnlyList<string>? Phones,
    DateOnly? BirthDate);

/// <summary>Corpo de uma requisição de vínculo do beneficiário a um ponto de coleta.</summary>
public sealed record LinkBeneficiaryToPointRequest(Guid PointId);

/// <summary>Corpo de uma requisição de vínculo de parentesco.</summary>
public sealed record AddBeneficiaryKinshipRequest(string RelatedBeneficiaryCpf, KinshipType Type);

/// <summary>Gestão de beneficiários (área privada).</summary>
[Route("api/beneficiaries")]
[Produces("application/json")]
[Authorize]
public sealed class BeneficiariesController(ISender sender) : ApiController(sender)
{
    /// <summary>Lista os beneficiários visíveis ao usuário.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BeneficiaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListBeneficiariesQuery(), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Busca rápida do beneficiário pelo CPF (preenche a retirada).</summary>
    [HttpGet("by-cpf/{cpf}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FindByCpf(string cpf, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new FindBeneficiaryByCpfQuery(cpf), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Obtém um beneficiário.</summary>
    [HttpGet("{beneficiaryId:guid}")]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid beneficiaryId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetBeneficiaryByIdQuery(beneficiaryId), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Cadastra um beneficiário.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterBeneficiaryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterBeneficiaryCommand(
            request.Name,
            request.Cpf,
            request.Email,
            request.Phones,
            request.BirthDate,
            request.RegisteredByPointId);

        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, value => Created($"/api/beneficiaries/{value.Id}", value));
    }

    /// <summary>Atualiza os dados de um beneficiário (valem para todos os pontos que o atendem).</summary>
    [HttpPut("{beneficiaryId:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid beneficiaryId,
        [FromBody] UpdateBeneficiaryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBeneficiaryCommand(
            beneficiaryId,
            request.Name,
            request.Cpf,
            request.Email,
            request.Phones,
            request.BirthDate);

        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>
    /// Vincula um beneficiário já cadastrado a um ponto de coleta. Uma pessoa (CPF único no município)
    /// pode ser atendida por vários pontos; cada ponto decide vinculá-la. O gestor só vincula ao próprio ponto.
    /// </summary>
    /// <response code="200">Beneficiário vinculado ao ponto.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Beneficiário ou ponto não encontrado.</response>
    /// <response code="409">O beneficiário já está vinculado ao ponto.</response>
    [HttpPost("{beneficiaryId:guid}/points")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkToPoint(
        Guid beneficiaryId,
        [FromBody] LinkBeneficiaryToPointRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LinkBeneficiaryToPointCommand(beneficiaryId, request.PointId);
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>
    /// Registra um vínculo de parentesco do beneficiário com outro (localizado pelo CPF). O vínculo
    /// é espelhado no outro beneficiário e é apenas informativo: não altera a cota de retiradas.
    /// </summary>
    [HttpPost("{beneficiaryId:guid}/kinships")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddKinship(
        Guid beneficiaryId,
        [FromBody] AddBeneficiaryKinshipRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddBeneficiaryKinshipCommand(beneficiaryId, request.RelatedBeneficiaryCpf, request.Type);
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>
    /// Histórico municipal de retiradas do beneficiário + saldo do ciclo vigente segundo a regra do
    /// ponto informado (<c>pointId</c>). O gestor só consulta pelo próprio ponto.
    /// </summary>
    [HttpGet("{beneficiaryId:guid}/withdrawals")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryWithdrawalHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWithdrawalHistory(
        Guid beneficiaryId,
        [FromQuery] Guid pointId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiaryId, pointId), cancellationToken);
        return Respond(result, Ok);
    }
}
