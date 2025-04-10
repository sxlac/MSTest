using System;
using System.Threading.Tasks;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.CKD.Svc.Core.Behaviors;
using Signify.CKD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Behaviors
{
    /// <remarks>
    /// Unfortunately, in-memory databases do not support transactions, so much of the <see cref="MediatrUnitOfWork{TRequest,TResponse}"/>
    /// cannot be tested.
    /// </remarks>
    public sealed class MediatrUnitOfWorkTests : IDisposable, IAsyncDisposable
    {
        // ReSharper disable once MemberCanBePrivate.Global - Needs to be public for FakeItEasy
        public interface INextHandler<T>
        {
            Task<T> Next();
        }
        
        private readonly MockDbFixture _dbFixture = new();
        private readonly IMessageProducer _producer = A.Fake<IMessageProducer>();

        private readonly IRequest<object> _dummyRequest = A.Dummy<IRequest<object>>();
        private readonly INextHandler<object> _nextHandler = A.Fake<INextHandler<object>>();

        public void Dispose()
        {
            _dbFixture.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _dbFixture.DisposeAsync();
        }

        private MediatrUnitOfWork<TRequest, TResponse> CreateSubject<TRequest, TResponse>()
            where TRequest : IRequest<TResponse>
        {
            return new MediatrUnitOfWork<TRequest, TResponse>(
                A.Dummy<ILogger<MediatrUnitOfWork<TRequest, TResponse>>>(),
                _dbFixture.Context, _producer);
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
            var actualResult = await subject.Handle(_dummyRequest, () => _nextHandler.Next(), default);

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
                await subject.Handle(_dummyRequest, () => _nextHandler.Next(), default));
        }
    }
}
