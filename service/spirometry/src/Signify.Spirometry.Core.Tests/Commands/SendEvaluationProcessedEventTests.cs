using AutoMapper;
using FakeItEasy;
using NServiceBus.Testing;
using Signify.Spirometry.Core.Commands;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class SendEvaluationProcessedEventTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private SendEvaluationProcessedEventHandler CreateSubject()
        => new(_mapper);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public async Task Handle_When_Requests_ComesIn_Raises_NsbEvents(bool? isPayable)
    {
        var context = new TestableMessageHandlerContext();
        var request =
            A.Fake<SendEvaluationProcessedEvent>(
                o => o.WithArgumentsForConstructor(new object[] { A.Fake<EvaluationProcessedEvent>(), isPayable, context }));
        await CreateSubject().Handle(request, default);

        Assert.NotEmpty(context.SentMessages);
        Assert.NotNull(context.FindSentMessage<EvaluationProcessedEvent>());
        A.CallTo(() => _mapper.Map<EvaluationProcessedEventForPayment>(request.EvaluationProcessedEvent)).MustHaveHappenedOnceExactly();
        Assert.NotNull(context.FindSentMessage<EvaluationProcessedEventForPayment>());
    }
}