using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.ApiClients.RcmApi.Requests;
using Signify.Spirometry.Core.ApiClients.RcmApi;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class CreateBillHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly IApiResponse<Guid?> _fakeResponse = A.Fake<IApiResponse<Guid?>>();
    private readonly IRcmApi _rcmApi = A.Fake<IRcmApi>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    public CreateBillHandlerTests()
    {
        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .Returns(Task.FromResult(_fakeResponse));
    }

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private CreateBillHandler CreateSubject()
        => new(A.Dummy<ILogger<CreateBillHandler>>(), _applicationTime, _rcmApi, _mediator, _mapper, _publishObservability);

    [Fact]
    public async Task Handle_WithUnsuccessfulStatusCode_Throws()
    {
        var request = new CreateBill();

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(false);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(HttpStatusCode.Unauthorized); // Really just anything other than 200-level or 301

        var subject = CreateSubject();

        await Assert.ThrowsAnyAsync<RcmBillingRequestException>(async () => await subject.Handle(request, default));

        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappened(2, Times.Exactly);
    }

    [Fact]
    public async Task Handle_WhenSentToRcm_SetsCorrelationId()
    {
        // Arrange
        const int evaluationId = 10;
        const int spirometryExamId = 11;
        var billId = Guid.NewGuid();

        var request = new CreateBill
        {
            EventId = Guid.NewGuid(),
            EvaluationId = evaluationId,
            BatchName = "BatchName"
        };

        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .Returns(new SpirometryExam
            {
                SpirometryExamId = spirometryExamId
            });

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(true);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(HttpStatusCode.Accepted);

        // 202 puts BillId on the response content
        A.CallTo(() => _fakeResponse.Content)
            .Returns(billId);

        // Act
        var subject = CreateSubject();

        await subject.Handle(request, default);

        // Assert

        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>.That.Matches(r =>
                !string.IsNullOrWhiteSpace(r.CorrelationId))))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, false)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappened(2, Times.Exactly);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_HappyPath(bool isMovedPermanently)
    {
        // Arrange
        const int evaluationId = 10;
        const int spirometryExamId = 11;
        var billId = Guid.NewGuid();

        var request = new CreateBill
        {
            EventId = Guid.NewGuid(),
            EvaluationId = evaluationId,
            BatchName = "BatchName"
        };

        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .Returns(new SpirometryExam
            {
                SpirometryExamId = spirometryExamId
            });

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(!isMovedPermanently);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(isMovedPermanently ? HttpStatusCode.MovedPermanently : HttpStatusCode.Accepted);

        // 202 puts BillId on the response content
        A.CallTo(() => _fakeResponse.Content)
            .Returns(!isMovedPermanently ? billId : null);
        // 301 puts BillId on the error content
        A.CallTo(() => _fakeResponse.Error)
            .Returns(isMovedPermanently ? CreateApiError(billId) : null);

        // Act
        var subject = CreateSubject();

        await subject.Handle(request, default);

        // Assert
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>
                    .That.Matches(q => q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<CreateBillRequest>(A<SpirometryExam>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map(A<CreateBill>._, A<CreateBillRequest>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _rcmApi.SendBillingRequest(A<CreateBillRequest>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>.That.Matches(b =>
                    b.EvaluationId == evaluationId && b.EventId == request.EventId &&
                    b.BillRequestSent.BillId == billId && b.BillRequestSent.SpirometryExamId == spirometryExamId &&
                    b.BillRequestSent.CreatedDateTime == _applicationTime.UtcNow()),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, false)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappened(2, Times.Exactly);
    }

    private static ApiException CreateApiError(Guid billId)
        => new FakeApiException(HttpMethod.Post, HttpStatusCode.MovedPermanently, JsonConvert.SerializeObject(billId));
}