using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Infrastructure;
using Signify.CKD.Svc.Core.Queries;
using Signify.CKD.Svc.Core.Tests.Data;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers.Nsb;

public class SaveProviderPayEventHandlerTest
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly TestableInvokeHandlerContext _messageSession = new();

    private SaveProviderPayEventHandler CreateSubject() => new(A.Dummy<ILogger<SaveProviderPayEventHandler>>(), _mediator, _transactionSupplier, _applicationTime);

    [Fact]
    public async Task Handler_ProviderPayStatusEvent_Raised_And_CreateProviderPay_WrittenTo()
    {
        var message = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mediator.Send(A<GetCKD>._, CancellationToken.None)).Returns(A.Dummy<Core.Data.Entities.CKD>());

        await CreateSubject().Handle(message, _messageSession);

        A.CallTo(() => _mediator.Send(A<GetCKD>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>.That.Matches(c =>
                c.ProviderPay.CreatedDateTime == _applicationTime.UtcNow()), CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().NotBeNull();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handler_DoesNot_WriteToDatabase_When_DatabaseRead_Fails()
    {
        A.CallTo(() => _mediator.Send(A<GetCKD>._, CancellationToken.None)).Throws<Exception>();
        var message = A.Dummy<SaveProviderPay>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(message, _messageSession));
        
        A.CallTo(() => _mediator.Send(A<GetCKD>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustNotHaveHappened();

        _transactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task Handler_DoesNot_Raise_ProviderPayStatusEvent_When_DatabaseRead_Fails()
    {
        A.CallTo(() => _mediator.Send(A<GetCKD>._, CancellationToken.None)).Throws<Exception>();
        var message = A.Dummy<SaveProviderPay>();

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(message, _messageSession));

        A.CallTo(() => _mediator.Send(A<GetCKD>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateProviderPay>._, CancellationToken.None))
            .MustNotHaveHappened();
        _messageSession.FindSentMessage<ProviderPayStatusEvent>().Should().BeNull();

        _transactionSupplier.AssertRollback();
    }
}
