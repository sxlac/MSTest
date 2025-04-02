using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class FOBTPerformedHandlerTest : IClassFixture<EntityFixtures>
{
    private readonly IMapper _mapper;
    private readonly FOBTPerformedHandler _handler;
    private readonly EntityFixtures _entityFixtures;
    private readonly TestableMessageHandlerContext _messageHandlerContext;

    public FOBTPerformedHandlerTest(EntityFixtures entityFixtures)
    {
        var logger = A.Fake<ILogger<FOBTPerformedHandler>>();
        _mapper = A.Fake<IMapper>();

        _entityFixtures = entityFixtures;
        var publishObservability = A.Fake<IPublishObservability>();
        _handler = new FOBTPerformedHandler(logger, _mapper, publishObservability);
        _messageHandlerContext = new TestableMessageHandlerContext();
    }

    [Fact]
    public async Task FOBTPerformedHandler_PublishCheck()
    {
        A.CallTo(() => _mapper.Map<UpdateInventoryRequest>(A<FOBTPerformedEvent>._)).Returns(_entityFixtures.MockUpdateInventory());

        await _handler.Handle(FobtPerformed, _messageHandlerContext);
        _messageHandlerContext.SentMessages.Length.Should().Be(1); //Updated to 1 while InventoryUpdates are disabled.
    }

    private static readonly FOBTPerformedEvent FobtPerformed = new()
    {
        ApplicationId = "Signify.Evaluation.Service",
        AppointmentId = 1000084716,
        ClientId = 14,
        CreatedDateTime = DateTimeOffset.UtcNow,
        DateOfService = DateTime.UtcNow,
        EvaluationId = 324357,
        MemberId = 11990396,
        MemberPlanId = 21074285,
        ProviderId = 42879,
        ReceivedDateTime = DateTime.UtcNow,
        UserName = "vastest1"
            
    };
}