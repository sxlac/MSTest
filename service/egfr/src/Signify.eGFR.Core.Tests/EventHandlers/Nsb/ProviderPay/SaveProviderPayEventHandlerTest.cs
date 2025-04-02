using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.ProviderPay;

public class SaveProviderPayEventHandlerTest
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly TestableInvokeHandlerContext _messageSession = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private SaveProviderPayEventHandler CreateSubject() => new(A.Dummy<ILogger<SaveProviderPayEventHandler>>(), _mediator, _transactionSupplier,_publishObservability,  _applicationTime, _mapper);

    [Fact]
    public async Task Handler_ProviderPayStatusEvent_Raised_And_CreateProviderPay_WrittenTo()
    {
        var message = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mediator.Send(A<QueryExam>._, CancellationToken.None)).Returns(A.Dummy<Exam>());
        A.CallTo(() => _mediator.Send(A<AddProviderPay>.That.Matches(c =>
            c.ProviderPay.CreatedDateTime == _applicationTime.UtcNow()), CancellationToken.None));

        await CreateSubject().Handle(message, _messageSession);
        
        A.CallTo(() => _mediator.Send(A<AddProviderPay>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.Commit())
            .MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handler_DoesNot_RaiseKafkaEvent_When_DatabaseWrite_ProviderPayTable_Fails()
    {
        A.CallTo(() => _mediator.Send(A<AddProviderPay>._, CancellationToken.None)).Throws<Exception>();
        var message = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mediator.Send(A<QueryExam>._, CancellationToken.None)).Returns(A.Dummy<Exam>());
        A.CallTo(() => _mediator.Send(A<AddProviderPay>.That.Matches(c =>
            c.ProviderPay.CreatedDateTime == _applicationTime.UtcNow()), CancellationToken.None));

        await Assert.ThrowsAnyAsync<Exception>(async () => await CreateSubject().Handle(message, _messageSession));
        
        A.CallTo(() => _mediator.Send(A<AddProviderPay>._, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ProviderPayRequestSent>._, CancellationToken.None))
            .MustNotHaveHappened();
        A.CallTo(() => _publishObservability.Commit())
            .MustNotHaveHappened();
        _transactionSupplier.AssertRollback();
    }
}