using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.DEE.Svc.Core.Behaviors;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Behaviors;

public sealed class MediatrUnitOfWorkTests : IDisposable, IAsyncDisposable
{
    public interface INextHandler<T>
    {
        Task<T> Next();
    }

    private readonly MockDbFixture _dbFixture = new();
    private readonly IMessageProducer _producer = A.Fake<IMessageProducer>();
    private readonly IRequest<object> _dummyRequest = A.Dummy<IRequest<object>>();
    private readonly INextHandler<object> _nextHandler = A.Fake<INextHandler<object>>();

    public MediatrUnitOfWorkTests()
    {
        // These setups are needed because of the way Signify.AkkaStreams.Postgres.BufferedTransactionExtensions.BeginTransaction uses ActivatorUtilities.CreateInstance
        var sp = A.Fake<IServiceProvider>();

        A.CallTo(() => sp.GetService(typeof(IServiceProviderIsService)))
            .Returns(A.Dummy<IServiceProviderIsService>());

        A.CallTo(() => _producer.ServiceProvider)
            .Returns(sp);
    }

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync() => _dbFixture.DisposeAsync();

    private MediatrUnitOfWork<TRequest, TResponse> CreateSubject<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        return new MediatrUnitOfWork<TRequest, TResponse>(
            A.Dummy<ILogger<MediatrUnitOfWork<TRequest, TResponse>>>(),
            _dbFixture.FakeDatabaseContext, _producer);
    }

    [Fact]
    public async Task Handle_WithNoTransaction_ReturnsResultOfNextDelegate()
    {
        // Arrange
        var subject = CreateSubject<IRequest<object>, object>();

        var expectedResult = new object();

        A.CallTo(() => _nextHandler.Next())
            .Returns(expectedResult);

        // Act
        var actualResult = await subject.Handle(_dummyRequest, () => _nextHandler.Next(), CancellationToken.None);

        // Assert
        A.CallTo(() => _nextHandler.Next())
            .MustHaveHappenedOnceExactly();

        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public async Task Handle_WithNoTransaction_WhenNextDelegateThrows_ExceptionIsThrown()
    {
        // Arrange
        var subject = CreateSubject<IRequest<object>, object>();

        A.CallTo(() => _nextHandler.Next())
            .Throws(new ArithmeticException()); // Actual type doesn't matter, just use a type that isn't likely to be used, so we can ensure it is the same exception that's thrown

        // Act / Assert
        await Assert.ThrowsAnyAsync<ArithmeticException>(async () =>
            await subject.Handle(_dummyRequest, () => _nextHandler.Next(), CancellationToken.None));
    }
}