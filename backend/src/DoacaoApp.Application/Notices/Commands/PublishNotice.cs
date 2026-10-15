using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Notices.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Notices;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Notices.Commands;

/// <summary>
/// Publica um aviso no mural do ponto. <c>Audience</c> é <c>Public</c> (toda a comunidade) ou
/// <c>Managers</c> (só a rede de gestores). Requer Admin ou PointManager do ponto.
/// </summary>
public sealed record PublishNoticeCommand(
    Guid PointId,
    string Title,
    string Message,
    string Audience) : IRequest<ErrorOr<NoticeResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class PublishNoticeValidator : AbstractValidator<PublishNoticeCommand>
{
    public PublishNoticeValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(PointNotice.MaxTitleLength);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(PointNotice.MaxMessageLength);
        RuleFor(x => x.Audience)
            .Must(audience => Enum.TryParse<NoticeAudience>(audience, ignoreCase: true, out _))
            .WithMessage("O público do aviso deve ser Public ou Managers.");
    }
}

public sealed class PublishNoticeHandler(
    INoticeRepository noticeRepository,
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<PublishNoticeCommand, ErrorOr<NoticeResponse>>
{
    public async Task<ErrorOr<NoticeResponse>> Handle(
        PublishNoticeCommand command,
        CancellationToken cancellationToken)
    {
        var point = await pointRepository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var audience = Enum.Parse<NoticeAudience>(command.Audience, ignoreCase: true);
        var notice = PointNotice.Publish(point.Id, command.Title, command.Message, audience, point.Name, clock.UtcNow);
        if (notice.IsError)
        {
            return notice.Errors;
        }

        noticeRepository.Add(notice.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return notice.Value.ToResponse(point.Name);
    }
}
