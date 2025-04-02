using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Signify.eGFR.Core.ApiClients.ProviderApi.Responses;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.Commands;

public class AggregateExamDetailsTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private AggregateExamDetailsHandler CreateSubject() => new(_mediator, _mapper);
    
    [Fact]
    public async Task Handle_WithRequest_ReturnsExam()
    {

        var evaluationModel = new EvalReceived
        {
            Id = Guid.Empty,
            ApplicationId = "eGFR",
            EvaluationId = 1,
            AppointmentId = 1,
            ProviderId = 1,
            MemberId = 1,
            MemberPlanId = 01,
            ClientId = 1,
            CreatedDateTime = DateTimeOffset.Now,
            ReceivedDateTime = DateTimeOffset.Now,
            DateOfService = DateTimeOffset.Now,
            ReceivedByeGFRProcessManagerDateTime = DateTimeOffset.Now
        };
        var request = new AggregateExamDetails(evaluationModel);
        
        A.CallTo(() => _mapper.Map<Exam>(A<EvalReceived>._)).Returns(new Exam
        {
            ExamId = 1,
            ApplicationId = "eGFR",
            EvaluationId = 1,
            ProviderId = 1,
            MemberId = 1,
            MemberPlanId = 1,
            AppointmentId = 1,
            ClientId = 1
        });
        A.CallTo(() => _mediator.Send(A<QueryProviderInfo>._, A<CancellationToken>._)).Returns(new ProviderInfo()
        {
            NationalProviderIdentifier = "12345"
        });
        
        
        //Act
        var subject = CreateSubject();
        var result = await subject.Handle(request, CancellationToken.None);

        Assert.Equal(1, result.EvaluationId);
        Assert.Equal("12345", result.NationalProviderIdentifier);
    }
}