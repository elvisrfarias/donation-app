using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.CollectionPoints.Commands.BasketTemplates;
using DoacaoApp.Application.CollectionPoints.Commands.Categories;
using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
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

    /// <summary>Consulta um ponto de coleta, incluindo o estoque.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só consulta o próprio ponto.</remarks>
    /// <response code="200">Ponto encontrado.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCollectionPointQuery(id), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Indicadores de resultado de um ponto em um período: retiradas, atendimentos e últimas retiradas.</summary>
    /// <remarks>
    /// Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só consulta o próprio ponto. Sem
    /// <c>from</c>/<c>to</c> (datas <c>yyyy-MM-dd</c>, UTC), usa o mês atual completo; o período máximo é de 366 dias.
    /// </remarks>
    /// <response code="200">Indicadores do ponto.</response>
    /// <response code="400">Período inválido.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpGet("{id:guid}/dashboard")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboard(
        Guid id,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCollectionPointDashboardQuery(id, from, to), cancellationToken);
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

    /// <summary>Cadastra um novo item no catálogo de estoque granular do ponto.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só cadastra no próprio ponto.</remarks>
    /// <response code="201">Item cadastrado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto não encontrado.</response>
    /// <response code="409">Já existe um item com este nome no ponto.</response>
    [HttpPost("{id:guid}/stock-items")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStockItem(
        Guid id,
        [FromBody] CreateStockItemCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id }, cancellationToken);
        return Respond(result, value => Created($"/api/collection-points/{id}", value));
    }

    /// <summary>Atualiza nome, unidade, categoria e metas de um item de estoque (não altera a quantidade).</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só atualiza o próprio ponto.</remarks>
    /// <response code="200">Item atualizado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto ou item não encontrado.</response>
    /// <response code="409">Já existe outro item com este nome no ponto.</response>
    [HttpPut("{id:guid}/stock-items/{itemId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStockItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateStockItemCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id, StockItemId = itemId }, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Remove um item do catálogo de estoque do ponto.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só remove no próprio ponto.</remarks>
    /// <response code="204">Item removido.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto ou item não encontrado.</response>
    /// <response code="409">Item em uso em alguma receita de cesta.</response>
    [HttpDelete("{id:guid}/stock-items/{itemId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveStockItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveStockItemCommand(id, itemId), cancellationToken);
        return Respond(result, _ => NoContent());
    }

    /// <summary>Salva em bloco as quantidades de vários itens do ponto (aba "Estoque").</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>.</remarks>
    /// <response code="200">Quantidades atualizadas.</response>
    /// <response code="400">Quantidade negativa ou item inexistente.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpPut("{id:guid}/stock-items/quantities")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStockQuantities(
        Guid id,
        [FromBody] UpdateStockQuantitiesCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id }, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Salva em bloco metas, avisos de estoque baixo e acompanhamento de vários itens (aba "Metas e avisos").</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só configura o próprio ponto.</remarks>
    /// <response code="200">Configurações atualizadas.</response>
    /// <response code="400">Valores inválidos (negativos, ou aviso acima da meta).</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpPut("{id:guid}/stock-items/settings")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStockItemSettings(
        Guid id,
        [FromBody] UpdateStockItemSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id }, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Atualiza a regra de retirada do ponto (ciclo + máximo de cestas por pessoa).</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só configura o próprio ponto.</remarks>
    /// <response code="200">Regra atualizada.</response>
    /// <response code="400">Máximo por ciclo inválido.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpPut("{id:guid}/withdrawal-rule")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWithdrawalRule(
        Guid id,
        [FromBody] UpdateWithdrawalRuleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id }, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Cria uma nova receita de cesta básica para o ponto (ativa por padrão).</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só cadastra no próprio ponto.</remarks>
    /// <response code="201">Receita criada.</response>
    /// <response code="400">Dados inválidos (nome vazio, item duplicado, quantidade inválida ou item inexistente).</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto não encontrado.</response>
    [HttpPost("{id:guid}/basket-templates")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBasketTemplate(
        Guid id,
        [FromBody] CreateBasketTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id }, cancellationToken);
        return Respond(result, value => Created($"/api/collection-points/{id}", value));
    }

    /// <summary>Atualiza nome, itens e status (ativa/inativa) de uma receita de cesta básica.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só atualiza no próprio ponto.</remarks>
    /// <response code="200">Receita atualizada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto ou receita não encontrada.</response>
    [HttpPut("{id:guid}/basket-templates/{templateId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBasketTemplate(
        Guid id,
        Guid templateId,
        [FromBody] UpdateBasketTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id, TemplateId = templateId }, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Remove uma receita de cesta básica do ponto.</summary>
    /// <remarks>
    /// Requer a role <c>Admin</c> ou <c>PointManager</c>. Falha se a receita já foi usada em alguma
    /// retirada — nesse caso, desative-a em vez de removê-la.
    /// </remarks>
    /// <response code="204">Receita removida.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto ou receita não encontrada.</response>
    /// <response code="409">Receita já usada em alguma retirada.</response>
    [HttpDelete("{id:guid}/basket-templates/{templateId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveBasketTemplate(
        Guid id, Guid templateId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveBasketTemplateCommand(id, templateId), cancellationToken);
        return Respond(result, _ => NoContent());
    }

    /// <summary>Cadastra uma nova categoria no catálogo do ponto.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só cadastra no próprio ponto.</remarks>
    /// <response code="201">Categoria criada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto não encontrado.</response>
    /// <response code="409">Já existe uma categoria com este nome no ponto.</response>
    [HttpPost("{id:guid}/categories")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        Guid id,
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id }, cancellationToken);
        return Respond(result, value => Created($"/api/collection-points/{id}", value));
    }

    /// <summary>Atualiza nome e ícone de uma categoria do catálogo do ponto.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só atualiza o próprio ponto.</remarks>
    /// <response code="200">Categoria atualizada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto ou categoria não encontrada.</response>
    /// <response code="409">Já existe outra categoria com este nome no ponto.</response>
    [HttpPut("{id:guid}/categories/{categoryId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(typeof(CollectionPointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        Guid categoryId,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { PointId = id, CategoryId = categoryId }, cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Remove uma categoria do catálogo do ponto.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. O gestor só remove no próprio ponto.</remarks>
    /// <response code="204">Categoria removida.</response>
    /// <response code="403">Ponto de outro gestor.</response>
    /// <response code="404">Ponto ou categoria não encontrada.</response>
    /// <response code="409">Categoria em uso em algum item de estoque ou retirada.</response>
    [HttpDelete("{id:guid}/categories/{categoryId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveCategory(Guid id, Guid categoryId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveCategoryCommand(id, categoryId), cancellationToken);
        return Respond(result, _ => NoContent());
    }
}
