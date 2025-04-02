using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Messages.Events.Status;
using Signify.FOBT.Svc.Core.Commands;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class PublishStatusUpdateTests
{
    private readonly PublishStatusUpdateHandler _handler;
    private readonly IMessageProducer _messageProducer = A.Fake<IMessageProducer>();

    public PublishStatusUpdateTests()
    {
        _handler = new PublishStatusUpdateHandler(A.Dummy<ILogger<PublishStatusUpdateHandler>>(), _messageProducer);
    }

    [Fact]
    public async Task Should_Call_Produce_Status_Successfully()
    {
        //Arrange
        var status = new BillRequestNotSent
        {
            BillingProductCode = "FOBT",
            CreatedDate = DateTime.Now,
            EvaluationId = 1234,
            MemberPlanId = 4567,
            ProviderId = 4567,
            ReceivedDate = DateTime.Now,

        };
        var publishStatus = new PublishStatusUpdate(status);

        //Act
        var result = await _handler.Handle(publishStatus, CancellationToken.None);

        //Assert
        result.Should().Be(MediatR.Unit.Value);
    }
}