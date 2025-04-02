using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.EventHandlers;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers;

public class SaveProviderPayEventHandlerTests
{
   private readonly ILogger<SaveProviderPayEventHandler> _logger = A.Fake<ILogger<SaveProviderPayEventHandler>>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IApplicationTime _applicationTime = A.Fake<IApplicationTime>();
    private readonly TestableInvokeHandlerContext _messageSession = new();

    private SaveProviderPayEventHandler CreateSubject() => new(_logger, _mediator, _transactionSupplier, _applicationTime);

    [Fact]
    public async Task Handler_ProviderPayStatusEvent_Raised_And_CreateProviderPay_WrittenTo()
    {
        var message = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, CancellationToken.None)).Returns(A.Dummy<Core.Data.Entities.PAD>());
        A.CallTo(() => _applicationTime.UtcNow()).Returns(DateTime.Now);

        await CreateSubject().Handle(message, _messageSession);

        A.CallTo(() => _mediator.Send(A<GetPAD>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().NotBeNull();
    }

    [Fact]
    public async Task Handler_DoesNot_WriteToDatabase_When_DatabaseRead_Fails()
    {
        A.CallTo(() => _mediator.Send(A<GetPAD>._, CancellationToken.None)).Throws<Exception>();
        var message = A.Dummy<SaveProviderPay>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(message, _messageSession));
        
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handler_DoesNot_Raise_ProviderPayStatusEvent_When_DatabaseRead_Fails()
    {
        A.CallTo(() => _mediator.Send(A<GetPAD>._, CancellationToken.None)).Throws<Exception>();
        var message = A.Dummy<SaveProviderPay>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(message, _messageSession));

        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustNotHaveHappened();
        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().BeNull();
    }
    
    [Fact]
    public async Task Handler_DoesNot_RaiseKafkaEvent_When_DatabaseWrite_ProviderPayTable_Fails()
    {
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None)).Throws<Exception>();
        var message = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mediator.Send(A<GetPAD>._, CancellationToken.None)).Returns(A.Dummy<Core.Data.Entities.PAD>());
        A.CallTo(() => _applicationTime.UtcNow()).Returns(DateTime.Now);

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(message, _messageSession));

        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustNotHaveHappened();
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == LogLevel.Error)
            .MustHaveHappenedOnceExactly();
        
        A.CallTo(() => _mediator.Send(A<GetPAD>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreatePadStatus>._, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ProviderPayRequestSent>._, CancellationToken.None))
            .MustNotHaveHappened();
    }
}