using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.DEE.Svc.Core.Commands;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class BillRequestSentTests
{
    private readonly BillRequestSentHandler _createBillRequestSentHandler;
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    public BillRequestSentTests()
    {
        _createBillRequestSentHandler = new BillRequestSentHandler(A.Dummy<ILogger<BillRequestSentHandler>>(), _messageProducer);
    }

    [Fact]
    public async Task BillRequestSentHandler_Produces_Message()
    {
        var createProviderPay = new BillRequestSent
        {
            EvaluationId = 1,
            MemberPlanId = 1,
            ProviderId = 1,
            CreatedDateTime = default,
            ReceivedDateTime = default,
            BillId = null,
            PdfDeliveryDate = null
        };

        await _createBillRequestSentHandler.Handle(createProviderPay, CancellationToken.None);

        A.CallTo(() => _messageProducer.Produce(A<string>.That.Matches(key =>
                    key == createProviderPay.EvaluationId.ToString()),
                A<object>.That.Matches(o => o == createProviderPay),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}