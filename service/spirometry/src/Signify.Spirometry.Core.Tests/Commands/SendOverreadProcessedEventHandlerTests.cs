using AutoMapper;
using FakeItEasy;
using NServiceBus.Testing;
using Signify.Spirometry.Core.Commands;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class SendOverreadProcessedEventHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private SendOverreadProcessedEventHandler CreateSubject()
        => new(_mapper);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_When_Requests_ComesIn_Raises_NsbEvents(bool isPayable)
    {
        var context = new TestableMessageHandlerContext();
        var request =
            A.Fake<SendOverreadProcessedEvent>(
                o => o.WithArgumentsForConstructor(new object[] { A.Fake<OverreadProcessedEvent>(), isPayable, context }));
        await CreateSubject().Handle(request, default);

        Assert.NotEmpty(context.SentMessages);
        Assert.NotNull(context.FindSentMessage<OverreadProcessedEvent>());
        A.CallTo(() => _mapper.Map<OverreadProcessedEventForPayment>(request.OverreadProcessedBaseEvent)).MustHaveHappenedOnceExactly();
        Assert.NotNull(context.FindSentMessage<OverreadProcessedEventForPayment>());
    }
}