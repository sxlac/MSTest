using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;
using Xunit;

namespace Signify.uACR.Core.Tests.Commands;

public sealed class PublishResultsHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private PublishResultsHandler CreateSubject() => new(A.Dummy<ILogger<PublishResultsHandler>>(), _messageProducer, _mapper);

    private readonly MockDbFixture _fixture = new();

    public void Dispose()
    {
        _fixture.Dispose();
    }

    public ValueTask DisposeAsync()
        => _fixture.DisposeAsync();

    [Theory]
    [InlineData(30, "Performed", "A")]
    public async Task Handle_HappyPath_Test(decimal uacrResult, string description, string abnormalIndicator)
    {
        const int evaluationId = 12345;
        var exam = new Exam
        {
            EvaluationId = evaluationId
        };
        var labResults = new LabResult
        {
            UacrResult = uacrResult,
            ResultDescription = description,
        };
        var resultsReceived = new ResultsReceived
        {
            Result = new Group
            {
                UacrResult = uacrResult,
                Description = description,
                AbnormalIndicator = abnormalIndicator
            }
        };
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<LabResult>._)).Returns(resultsReceived);
        var request = new PublishResults(exam, labResults, true);
        var subject = CreateSubject();

        await subject.Handle(request, default);

        // Assert
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<LabResult>.That.Matches(r => r == labResults)))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map(A<Exam>.That.Matches(f => f == exam), A<ResultsReceived>._))
            .MustHaveHappened();
        A.CallTo(() => _messageProducer.Produce(
                A<string>.That.Matches(key => key == evaluationId.ToString()),
                A<ResultsReceived>.That.Matches(r => r.Result.UacrResult.Equals(uacrResult)),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}