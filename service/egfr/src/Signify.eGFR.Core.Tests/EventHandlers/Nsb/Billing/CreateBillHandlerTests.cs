using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using Refit;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.ApiClients.RcmApi;
using Signify.eGFR.Core.ApiClients.RcmApi.Requests;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.Billing;

public class CreateBillHandlerTests
{
    private const long EvaluationId = 1;

    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IBufferedTransaction _transaction = A.Fake<IBufferedTransaction>();
    private readonly IApiResponse<Guid?> _fakeResponse = A.Fake<IApiResponse<Guid?>>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IRcmApi _rcmApi = A.Fake<IRcmApi>();
    private readonly IMessageHandlerContext _fakeContext = A.Fake<IMessageHandlerContext>();
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly Guid _eventId = Guid.NewGuid();

    public CreateBillHandlerTests()
    {
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(_transaction);
        
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
             .Returns(Task.FromResult(_fakeResponse));
    }

    private CreateBillHandler CreateSubject()
        => new(A.Dummy<ILogger<CreateBillHandler>>(), _mediator, _transactionSupplier, _publishObservability, _applicationTime,  _mapper, _rcmApi);
    

    [Fact]
    public async Task Handle_CreateBilling_HappyPath()
    {
        // Arrange
        var billId = Guid.NewGuid();
        var request = new CreateBillEvent
        {
            EventId = _eventId,
            EvaluationId = EvaluationId,
            PdfDeliveryDateTime = _applicationTime.UtcNow(),
            BillableDate = _applicationTime.UtcNow(),
            RcmProductCode = ProductCodes.eGFR_RcmBilling
        };

        var fakeResponse = new ApiResponse<Guid?>(new HttpResponseMessage( HttpStatusCode.OK), billId, new RefitSettings());
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._)).Returns(fakeResponse);

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        
        A.CallTo(() => _mediator.Send(A<QueryExam>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
        
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>.That.Matches(r =>
                 !string.IsNullOrWhiteSpace(r.CorrelationId))))
             .MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeContext.Send(A<BillRequestSentEvent>.That.Matches(e => e.EventId == _eventId), A<SendOptions>._))
            .MustHaveHappened(1, Times.Exactly);
        
        A.CallTo(() => _fakeContext.Send(A<BillableExamStatusEvent>.That.Matches(e => e.StatusCode == ExamStatusCode.BillRequestSent), A<SendOptions>._))
            .MustHaveHappened(1, Times.Exactly);

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }
    
    [Fact]
    public async Task Handle_CreateBillingWithRcmApiError_ThrowsRcmBillingRequestException()
    {
        // Arrange
        var request = new CreateBillEvent
        {
            EventId = _eventId,
            EvaluationId = EvaluationId,
            PdfDeliveryDateTime = _applicationTime.UtcNow(),
            BillableDate = _applicationTime.UtcNow(),
            RcmProductCode = ProductCodes.eGFR_RcmBilling
        };
        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(false);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(HttpStatusCode.BadRequest);

        // assert
        await Assert.ThrowsAsync<RcmBillingRequestException>(async () => await CreateSubject().Handle(request, _fakeContext));
    }
}