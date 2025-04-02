using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Behaviors;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ILogger<GetEvalAnswers> _logger = A.Fake<ILogger<GetEvalAnswers>>();

    [Fact]
    public async Task Should_Invoke_LoggingBehavior()
    {
        //Arrange
        var request = new GetEvalAnswers();
        var del = A.Fake<RequestHandlerDelegate<GetEvalAnswersHandler>>();
        var loggingBehavior = new LoggingBehavior<GetEvalAnswers, GetEvalAnswersHandler>(_logger);

        //Act
        var response = await loggingBehavior.Handle(request, del, CancellationToken.None);

        //Assert
        response.Should().NotBeNull();
    }
}