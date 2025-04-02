using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events.Status;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class UpdateExamStatusTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ILogger<UpdateExamStatusHandler> _logger = A.Fake<ILogger<UpdateExamStatusHandler>>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    private UpdateExamStatusHandler CreateSubject()
        => new(_logger, _mapper, _mediator, _publishObservability);

    private static UpdateExamStatus CreateRequest(FOBTStatusCode status, string eventName)
    {
        const long evaluationId = 1;
        var request = A.Fake<UpdateExamStatus>();
        request.ExamStatus = A.Fake<ProviderPayStatusEvent>();
        request.ExamStatus.EventId = Guid.NewGuid();
        request.ExamStatus.EvaluationId = evaluationId;
        request.ExamStatus.StatusCode = status;
        ((ProviderPayStatusEvent)request.ExamStatus).ParentCdiEvent = eventName;
        return request;
    }

    private static Core.Data.Entities.FOBT CreateFobt()
    {
        return new Core.Data.Entities.FOBT
        {
            FOBTId = 4, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr",
            ApplicationId = "Signify.Evaluation.Service", AppointmentId = 1000084715, CenseoId = "1234567890",
            City = "Mysuru", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfBirth = DateTimeOffset.UtcNow.UtcDateTime, DateOfService = DateTimeOffset.UtcNow.UtcDateTime,
            EvaluationId = 324356, FirstName = "TestNAme", LastName = "TestLastNAme", MemberId = 11990396,
            MemberPlanId = 21074285, NationalProviderIdentifier = "9230239051", ProviderId = 42879,
            ReceivedDateTime = DateTimeOffset.UtcNow.UtcDateTime, State = "Karnataka", UserName = "vastest1",
            ZipCode = "12345"
        };
    }

    [Fact]
    public async Task Handle_WithMessage_AddsExamStatus_ProviderPayableEventReceived()
    {
        var request = CreateRequest(FOBTStatusCode.ProviderPayableEventReceived, nameof(CDIPassedEvent));
        var exam = CreateFobt();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);

        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayableEventReceived>(A<Core.Data.Entities.FOBT>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(a =>
                    a.FOBT == exam && a.StatusCode == FOBTStatusCode.ProviderPayableEventReceived),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.PayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_ExamStatus_With_StatusCode_ProviderNonPayableEventReceived()
    {
        var request = CreateRequest(FOBTStatusCode.ProviderNonPayableEventReceived, nameof(CDIPassedEvent));
        var exam = CreateFobt();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);

        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderNonPayableEventReceived>(A<Core.Data.Entities.FOBT>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(a =>
                    a.FOBT == exam && a.StatusCode == FOBTStatusCode.ProviderNonPayableEventReceived),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.NonPayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_ExamStatus_With_StatusCode_ProviderPayRequestSentReceived()
    {
        var request = CreateRequest(FOBTStatusCode.ProviderPayRequestSent, nameof(CDIPassedEvent));
        var exam = CreateFobt();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);

        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayRequestSent>(A<Core.Data.Entities.FOBT>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(a =>
                    a.FOBT == exam && a.StatusCode == FOBTStatusCode.ProviderPayRequestSent),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>.That.Matches(c =>
                c.EventType == Constants.Observability.ProviderPay.PayableCdiEvents), false))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [MemberData(nameof(VariousCdiStatusCodes))]
    public async Task Handle_WithMessage_AddsExamStatus_CdiEvents(FOBTStatusCode status, string eventName)
    {
        var request = CreateRequest(status, eventName);
        var exam = CreateFobt();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._)).Returns(exam);

        await CreateSubject().Handle(request, A.Dummy<CancellationToken>());

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenDeterminingWhetherToPublishToKafka_HandlesAllStatusCodes()
    {
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(new Core.Data.Entities.FOBT());
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, default)).Returns(A.Fake<FOBTStatus>());

        foreach (var statusCode in FOBTStatusCode.All)
        {
            var request = CreateRequest(statusCode, nameof(CDIPassedEvent));
            await CreateSubject().Handle(request, default);
        }

        Assert.True(true);
    }

    public static IEnumerable<object[]> VariousCdiStatusCodes()
    {
        yield return [FOBTStatusCode.CdiPassedReceived, nameof(CDIPassedEvent)];
        yield return [FOBTStatusCode.CdiFailedWithPayReceived, nameof(CDIFailedEvent)];
        yield return [FOBTStatusCode.CdiFailedWithoutPayReceived, nameof(CDIFailedEvent)];
    }
}