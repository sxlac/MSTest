using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.A1C.Svc.Core.Behaviors;
using Signify.A1C.Svc.Core.Queries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Behaviors
{
    public class LoggingBehaviorTests
    {
        private readonly ILogger<GetA1C> _logger;

        public LoggingBehaviorTests()
        {
            _logger = A.Fake<ILogger<GetA1C>>();
        }

        [Fact]
        public async Task Should_Invoke_LoggingBehavior()
        {
            var request = new GetA1C();
            var del = A.Fake<RequestHandlerDelegate<Core.Data.Entities.A1C>>();
            var loggingBehavior = new LoggingBehavior<GetA1C, Core.Data.Entities.A1C>(_logger);

            //Act
            var response = await loggingBehavior.Handle(request, CancellationToken.None, del);

            //Assert
            response.Should().NotBeNull();
        }
    }
}
