using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Notices;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Notices.Commands;

/// <summary>Retira um aviso do mural (arquivamento lógico). Requer Admin ou PointManager do ponto.</summary>
public sealed record ArchiveNoticeCommand(Guid PointId, Guid NoticeId)
    : IRequest<ErrorOr<Success>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class ArchiveNoticeHandler(INoticeRepository noticeRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<ArchiveNoticeCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ArchiveNoticeCommand command, CancellationToken cancellationToken)
    {
        var notice = await noticeRepository.GetByIdAsync(NoticeId.From(command.NoticeId), cancellationToken);
        if (notice is null || notice.PointId.Value != command.PointId)
        {
            return DomainErrors.Notice.NotFound;
        }

        notice.Archive();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
