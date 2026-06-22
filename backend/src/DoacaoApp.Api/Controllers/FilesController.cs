using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Storage.Commands;
using DoacaoApp.Application.Storage.Contracts;
using DoacaoApp.Application.Storage.Queries;
using DoacaoApp.Domain.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoacaoApp.Api.Controllers;

/// <summary>Gestão de arquivos/imagens (upload, consulta e remoção) no object storage.</summary>
[Route("api/files")]
[Produces("application/json")]
public sealed class FilesController(ISender sender) : ApiController(sender)
{
    /// <summary>Envia uma imagem.</summary>
    /// <remarks>Requer a role <c>Admin</c> ou <c>PointManager</c>. Aceita JPEG, PNG, WebP, GIF ou AVIF.</remarks>
    /// <response code="201">Arquivo enviado; retorna metadados + URL temporária.</response>
    /// <response code="400">Arquivo ausente, vazio, grande demais ou tipo não suportado.</response>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PointManager}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(StoredFileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] FileVisibility visibility = FileVisibility.Private,
        [FromForm] string? ownerType = null,
        [FromForm] Guid? ownerId = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Arquivo obrigatório e não pode estar vazio.");
            return ValidationProblem(ModelState);
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadImageCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            visibility,
            ownerType,
            ownerId);

        var result = await Sender.Send(command, cancellationToken);
        return Respond(result, value => Created($"/api/files/{value.Id}", value));
    }

    /// <summary>Metadados de um arquivo + URL temporária de download.</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(StoredFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetStoredFileQuery(id), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Lista os arquivos de um dono (ex.: fotos de um ponto de coleta).</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<StoredFileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByOwner(
        [FromQuery] string ownerType,
        [FromQuery] Guid ownerId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListFilesByOwnerQuery(ownerType, ownerId), cancellationToken);
        return Respond(result, Ok);
    }

    /// <summary>Remove um arquivo (apaga o objeto e marca o metadado como removido).</summary>
    /// <remarks>Requer a role <c>Admin</c>.</remarks>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeleteFileCommand(id), cancellationToken);
        return Respond(result, _ => NoContent());
    }
}
