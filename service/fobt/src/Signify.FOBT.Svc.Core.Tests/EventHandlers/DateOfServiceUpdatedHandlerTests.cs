using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class DateOfServiceUpdatedHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly DateOfServiceUpdateHandler _dateOfServiceUpdatedHandlerTests;
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly IPublishObservability _publishObservability;
        
    public DateOfServiceUpdatedHandlerTests(MockDbFixture mobFobtFixture)
    {
        var logger = A.Fake<ILogger<DateOfServiceUpdateHandler>>();
        _mediator = A.Fake<IMediator>();
        _mapper = A.Fake<IMapper>();
        _messageHandlerContext = new TestableMessageHandlerContext();
        _publishObservability = A.Fake<IPublishObservability>();
        _dateOfServiceUpdatedHandlerTests = new DateOfServiceUpdateHandler(logger, _mediator, mobFobtFixture.Context, _mapper, _publishObservability);
    }
    
    [Fact]
    public async Task Should_UpdateOfService()
    {
        var @event = new DateOfServiceUpdated(324356, DateTime.Now);

        await _dateOfServiceUpdatedHandlerTests.Handle(@event, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateFOBT>._, CancellationToken.None)).MustHaveHappened();
        A.CallTo(() => _mapper.Map<CreateOrUpdateFOBT>(A<Core.Data.Entities.FOBT>._)).MustHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappened();
    }
}