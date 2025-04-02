using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Events.Status;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Infrastructure;
using Signify.FOBT.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.NSB;

public class SaveProviderPayEventHandlerTest
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly TestableInvokeHandlerContext _messageSession = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    

    private SaveProviderPayEventHandler CreateSubject() => new(A.Dummy<ILogger<SaveProviderPayEventHandler>>(), _mediator, _transactionSupplier, _applicationTime, _publishObservability);

    [Fact]
    public async Task Handler_ProviderPayStatusEvent_Raised_And_CreateProviderPay_WrittenTo()
    {
        var message = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, CancellationToken.None)).Returns(A.Dummy<Core.Data.Entities.FOBT>());

        await CreateSubject().Handle(message, _messageSession);

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.Commit())
            .MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handler_DoesNot_WriteToDatabase_When_DatabaseRead_Fails()
    {
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, CancellationToken.None)).Throws<Exception>();
        var message = A.Dummy<SaveProviderPay>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(message, _messageSession));
        
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _publishObservability.Commit())
            .MustNotHaveHappened();
        _transactionSupplier.AssertRollback();
    }
    
    [Fact]
    public async Task Handler_DoesNot_RaiseKafkaEvent_When_DatabaseWrite_ProviderPayTable_Fails()
    {
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None)).Throws<Exception>();
        var message = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, CancellationToken.None)).Returns(A.Dummy<Core.Data.Entities.FOBT>());

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(message, _messageSession));
        
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ProviderPayRequestSent>._, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _publishObservability.Commit())
            .MustNotHaveHappened();
        _transactionSupplier.AssertRollback();
    }
}