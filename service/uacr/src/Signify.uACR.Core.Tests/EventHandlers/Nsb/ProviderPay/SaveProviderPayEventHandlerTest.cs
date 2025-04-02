using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.EventHandlers.Nsb;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.Infrastructure;
using Signify.uACR.Core.Queries;
using UacrNsbEvents;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;


public class SaveProviderPayEventHandlerTest
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly TestableInvokeHandlerContext _messageSession = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private SaveProviderPayEventHandler CreateSubject() => new(A.Dummy<ILogger<SaveProviderPayEventHandler>>(), _mediator, _mapper, _transactionSupplier, _applicationTime, _publishObservability);

    [Fact]
    public async Task Handler_ProviderPayStatusEvent_Raised_And_CreateProviderPay_WrittenTo()
    {
        var message = A.Fake<SaveProviderPay>();
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, CancellationToken.None)).Returns(A.Dummy<Exam>());

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
        A.CallTo(() => _mediator.Send(A<QueryExamByEvaluation>._, CancellationToken.None)).Returns(A.Dummy<Exam>());

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