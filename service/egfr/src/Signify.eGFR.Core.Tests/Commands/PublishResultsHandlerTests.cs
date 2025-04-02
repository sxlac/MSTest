using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;
using Xunit;

namespace Signify.eGFR.Core.Tests.Commands;

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
    {
        return _fixture.DisposeAsync();
    }
    
    [Theory]
    [MemberData(nameof(LabResults))]
    public async Task Handle_HappyPath_Test(Exam exam, ResultsReceived resultsReceived)
    {
        var request = new PublishResults(exam, resultsReceived, resultsReceived.IsBillable);
        var subject = CreateSubject();

        await subject.Handle(request, default);

        // Assert
        A.CallTo(() => _mapper.Map(A<Exam>.That.Matches(f => f == exam), A<ResultsReceived>._))
            .MustHaveHappened();
        A.CallTo(() => _messageProducer.Produce(
                A<string>.That.Matches(key => key.Equals(resultsReceived.EvaluationId.ToString())),
                A<ResultsReceived>.That.Matches(r => (r.Result.Result.Equals(resultsReceived.Result.Result) && r.IsBillable.Equals(resultsReceived.IsBillable) )),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
    
    public static IEnumerable<object[]> LabResults()
    {
        yield return
        [
            new Exam
            {
                EvaluationId = 123
            },
            new ResultsReceived
            {
                Result = new Group
                {
                    Result = 60.45m,
                    Description = string.Empty,
                    AbnormalIndicator = "A"
                },
                EvaluationId = 123,
                Determination = "A",
                ReceivedDate = DateTimeOffset.Now,
                PerformedDate = DateTimeOffset.Now,
                IsBillable = true
            }
        ];
    }
}