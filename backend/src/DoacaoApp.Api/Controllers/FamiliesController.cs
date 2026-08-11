using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Application.Families.Commands;
using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Application.Families.Queries;
using DoacaoApp.Domain.Families;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Corpo de uma requisição de cadastro de beneficiário (a família vem na rota).</summary>
public sealed record RegisterBeneficiaryRequest(
    string Name,
    string Cpf,
    DateOnly? BirthDate,
    Guid RegisteredByPointId);

/// <summary>Corpo de uma requisição de atualização de beneficiário.</summary>
public sealed record UpdateBeneficiaryRequest(
    string Name,
    string Cpf,
    DateOnly? BirthDate,
    Guid RegisteredByPointId);

/// <summary>Corpo de uma requisição de vínculo de parentesco.</summary>
public sealed record AddBeneficiaryKinshipRequest(string RelatedBeneficiaryCpf, KinshipType Type);

/// <summary>Gestão de famílias e beneficiários (área privada).</summary>
[Route("api/families")]
[Produces("application/json")]
[Authorize]
public sealed class FamiliesController(ISender sender) : ApiController(sender)
{
    /// <summary>Cria uma família com sua regra de ciclo.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(FamilyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFamilyCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, value => Created($"/api/families/{value.Id}", value));
    }

    /// <summary>Lista todas as famílias com seus beneficiários.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FamilyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListFamiliesQuery(), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Busca rápida da família pelo CPF de um beneficiário (preenche a retirada).</summary>
    [HttpGet("by-beneficiary-cpf/{cpf}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(FamilyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FindByCpf(string cpf, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new FindFamilyByCpfQuery(cpf), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Obtém uma família e seus beneficiários.</summary>
    [HttpGet("{familyId:guid}")]
    [ProducesResponseType(typeof(FamilyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid familyId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetFamilyByIdQuery(familyId), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Cadastra e vincula um beneficiário à família (LinkBeneficiaryToFamily).</summary>
    [HttpPost("{familyId:guid}/beneficiaries")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterBeneficiary(
        Guid familyId,
        [FromBody] RegisterBeneficiaryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterBeneficiaryCommand(
            familyId, request.Name, request.Cpf, request.BirthDate, request.RegisteredByPointId);

        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, value => Created($"/api/families/{familyId}/beneficiaries/{value.Id}", value));
    }

    /// <summary>Atualiza os dados de um beneficiário já vinculado à família.</summary>
    [HttpPut("{familyId:guid}/beneficiaries/{beneficiaryId:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateBeneficiary(
        Guid familyId,
        Guid beneficiaryId,
        [FromBody] UpdateBeneficiaryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBeneficiaryCommand(
            familyId, beneficiaryId, request.Name, request.Cpf, request.BirthDate, request.RegisteredByPointId);

        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>
    /// Registra um vínculo de parentesco do beneficiário com outro (localizado pelo CPF). Só
    /// Cônjuge entre beneficiários de famílias diferentes forma uma família nova — os demais
    /// tipos são só informativos. A família da rota não é usada para localizar o beneficiário
    /// (ele pode ter mudado de família por uma fusão anterior); fica só para manter a URL
    /// consistente com os outros endpoints de beneficiário.
    /// </summary>
    [HttpPost("{familyId:guid}/beneficiaries/{beneficiaryId:guid}/kinships")]
    [Authorize(Roles = Roles.Admin + "," + Roles.PointManager)]
    [ProducesResponseType(typeof(BeneficiaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddBeneficiaryKinship(
        Guid familyId,
        Guid beneficiaryId,
        [FromBody] AddBeneficiaryKinshipRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddBeneficiaryKinshipCommand(beneficiaryId, request.RelatedBeneficiaryCpf, request.Type);
        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Histórico municipal de retiradas da família + saldo do ciclo vigente.</summary>
    [HttpGet("{familyId:guid}/withdrawals")]
    [ProducesResponseType(typeof(FamilyWithdrawalHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWithdrawalHistory(Guid familyId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetFamilyWithdrawalHistoryQuery(familyId), cancellationToken);
        return Respond(result, Ok);
    }
}