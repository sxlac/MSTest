using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers.Nsb;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.Dps.Observability.Library.Services;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.Nsb;

public class SaveProviderPayEventHandlerTest
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly TestableInvokeHandlerContext _messageSession = new();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    private SaveProviderPayEventHandler CreateSubject() =>
        new(A.Dummy<ILogger<SaveProviderPayEventHandler>>(), _mediator, _transactionSupplier, _applicationTime, _mapper, _publishObservability);

    [Fact]
    public async Task Handler_ProviderPayStatusEvent_Raised_And_CreateProviderPay_WrittenTo()
    {
        var message = A.Fake<SaveProviderPay>();

        await CreateSubject().Handle(message, _messageSession);

        A.CallTo(() => _mediator.Send(A<CreateProviderPay>.That.Matches(c =>
                c.ProviderPay.CreatedDateTime == _applicationTime.UtcNow()), CancellationToken.None))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamStatus>.That.Matches(s => s.ExamStatus.StatusCode == ExamStatusCode.ProviderPayRequestSent),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
        A.CallTo(() => _publishObservability.Commit()).MustHaveHappenedOnceExactly();
    }   
}