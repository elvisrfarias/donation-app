using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using ErrorOr;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Common.Security;

public class CollectionPointScopeBehaviorTests
{
    private static readonly Guid OwnPointId = Guid.NewGuid();
    private static readonly Guid OtherPointId = Guid.NewGuid();

    private sealed record ScopedRequest(Guid CollectionPointId) : IScopedToCollectionPoint;

    private sealed record UnscopedRequest;

    private readonly Mock<ICurrentUser> _currentUser = new();
    private bool _nextWasCalled;

    private CollectionPointScopeBehavior<TRequest, ErrorOr<Success>> BuildBehavior<TRequest>()
        where TRequest : notnull =>
        new(_currentUser.Object);

    private Task<ErrorOr<Success>> Next()
    {
        _nextWasCalled = true;
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    private void GivenUser(bool isAdmin, Guid? collectionPointId)
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(isAdmin);
        _currentUser.Setup(u => u.CollectionPointId).Returns(collectionPointId);
    }

    [Fact]
    public async Task Handle_QuandoORequestNaoImplementaOMarcador_ChamaNextIndependenteDoPapel()
    {
        GivenUser(isAdmin: false, collectionPointId: OtherPointId);
        var behavior = BuildBehavior<UnscopedRequest>();

        var result = await behavior.Handle(new UnscopedRequest(), Next, CancellationToken.None);

        _nextWasCalled.Should().BeTrue();
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoUsuarioEAdmin_ChamaNextIndependenteDoPontoInformado()
    {
        GivenUser(isAdmin: true, collectionPointId: OtherPointId);
        var behavior = BuildBehavior<ScopedRequest>();

        var result = await behavior.Handle(new ScopedRequest(OwnPointId), Next, CancellationToken.None);

        _nextWasCalled.Should().BeTrue();
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoPontoInformadoEODoUsuario_ChamaNext()
    {
        GivenUser(isAdmin: false, collectionPointId: OwnPointId);
        var behavior = BuildBehavior<ScopedRequest>();

        var result = await behavior.Handle(new ScopedRequest(OwnPointId), Next, CancellationToken.None);

        _nextWasCalled.Should().BeTrue();
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoPontoInformadoEDeOutroPonto_RetornaForbiddenSemChamarNext()
    {
        GivenUser(isAdmin: false, collectionPointId: OwnPointId);
        var behavior = BuildBehavior<ScopedRequest>();

        var result = await behavior.Handle(new ScopedRequest(OtherPointId), Next, CancellationToken.None);

        _nextWasCalled.Should().BeFalse();
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        result.FirstError.Code.Should().Be("Scope.CollectionPointForbidden");
    }

    [Fact]
    public async Task Handle_QuandoUsuarioSemCollectionPointId_RetornaForbiddenSemChamarNext()
    {
        GivenUser(isAdmin: false, collectionPointId: null);
        var behavior = BuildBehavior<ScopedRequest>();

        var result = await behavior.Handle(new ScopedRequest(OwnPointId), Next, CancellationToken.None);

        _nextWasCalled.Should().BeFalse();
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
    }
}
