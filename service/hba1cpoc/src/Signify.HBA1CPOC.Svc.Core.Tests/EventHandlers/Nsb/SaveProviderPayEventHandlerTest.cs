using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class SaveProviderPayEventHandlerTest
{
    private readonly IMediator _mediator;
    private readonly SaveProviderPayEventHandler _saveProviderPayEventHandler;
    private readonly TestableInvokeHandlerContext _messageSession = new();

    public SaveProviderPayEventHandlerTest()
    {
        var logger = A.Fake<ILogger<SaveProviderPayEventHandler>>();
        _mediator = A.Fake<IMediator>();
        var transactionSupplier = A.Fake<ITransactionSupplier>();
        var applicationTime = A.Fake<IApplicationTime>();

        _saveProviderPayEventHandler = new SaveProviderPayEventHandler(logger, _mediator, transactionSupplier, applicationTime);
    }

    [Fact]
    public async Task Handler_WriteToDatabase_Raise_NSB_WhenSuccess()
    {
        await _saveProviderPayEventHandler.Handle(A.Dummy<SaveProviderPay>(), _messageSession);

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().NotBeNull();
    }

    [Fact]
    public async Task Handler_DoesNot_WriteToDatabase_Or_RaiseNsb_When_DatabaseRead_Fails()
    {
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, CancellationToken.None)).Throws<Exception>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await _saveProviderPayEventHandler.Handle(A.Dummy<SaveProviderPay>(), _messageSession));

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustNotHaveHappened();
        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().BeNull();
    }
    
    [Fact]
    public async Task Handler_DoesNot_RaiseNsb_When_DatabaseWrite_Fails()
    {
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None)).Throws<Exception>();
        
        await Assert.ThrowsAnyAsync<Exception>(async () => await _saveProviderPayEventHandler.Handle(A.Dummy<SaveProviderPay>(), _messageSession));

        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().BeNull();
    }
}