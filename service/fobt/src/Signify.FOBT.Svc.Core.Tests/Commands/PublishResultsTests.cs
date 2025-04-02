using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class PublishResultsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_HappyPath_Test(bool isBillable)
    {
        // Arrange
        const int evaluationId = 1;

        var fobt = new Fobt
        {
            EvaluationId = evaluationId
        };

        var labResults = new LabResults();

        var request = new PublishResults(fobt, labResults, isBillable);

        var mapper = A.Fake<IMapper>();
        var producer = A.Fake<IMessageProducer>();

        var subject = new PublishResultsHandler(A.Dummy<ILogger<PublishResultsHandler>>(),
            producer, mapper);

        // Act
        await subject.Handle(request, default);

        // Assert
        A.CallTo(() => mapper.Map<Results>(A<LabResults>.That.Matches(r => r == labResults)))
            .MustHaveHappened();
        A.CallTo(() => mapper.Map(A<Fobt>.That.Matches(f => f == fobt), A<Results>._))
            .MustHaveHappened();
        A.CallTo(() => producer.Produce(
                A<string>.That.Matches(key => key == evaluationId.ToString()),
                A<Results>.That.Matches(r => r.IsBillable == isBillable),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}