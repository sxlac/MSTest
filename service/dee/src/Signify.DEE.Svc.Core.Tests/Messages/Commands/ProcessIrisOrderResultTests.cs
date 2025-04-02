using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Tests.Mocks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class ProcessIrisOrderResultTests
{
    private readonly IMediator _mediator;
    private readonly ProcessIrisOrderResultHandler _handler;
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly IMapper _mapper;

    public ProcessIrisOrderResultTests()
    {
        var logger = A.Fake<ILogger<ProcessIrisOrderResultHandler>>();
        _mapper = A.Fake<IMapper>();
        _mediator = A.Fake<IMediator>();
        _handler = new ProcessIrisOrderResultHandler(logger, _mapper, _mediator, _applicationTime);
    }

    [Fact]
    public async Task ProcessIrisOrderResultHandler_HandleProcessIrisOrderResult_SendCreateExamResultRecordMediatrRequest()
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.LocalNow(), MemberPlanId = 2, EvaluationId = 25, ExamId = 11 };

        var processIrisOrderResult = new ProcessIrisOrderResult
        {
            OrderResult = OrderResultMock.BuildOrderResult(),
            Exam = examModel
        };

        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));

        // Act
        await _handler.Handle(processIrisOrderResult, CancellationToken.None);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateExamResultRecord>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.IRISExamCreated.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.IRISInterpreted.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
    }

    [Theory]
    [InlineData("Other - Suspected Dry AMD", true)]
    [InlineData("Other: Suspected Dry AMD", true)]
    [InlineData("Other - Suspected Wet AMD", true)]
    [InlineData("Other: Suspected Wet AMD", true)]
    [InlineData("Other - Suspected Dry AMD", false)]
    [InlineData("Other: Suspected Dry AMD", false)]
    [InlineData("Other - Suspected Wet AMD", false)]
    [InlineData("Other: Suspected Wet AMD", false)]
    public async Task WhenNonDRAmdFindings_HandleProcessIrisOrderResult_SendCreateExamResultRecordMediatrRequest(string amdFinding, bool isRightSide)
    {
        // Arrange
        var examModel = new ExamModel
        { DateOfService = _applicationTime.LocalNow(), MemberPlanId = 2, EvaluationId = 25, ExamId = 11 };

        var processIrisOrderResult = new ProcessIrisOrderResult
        {
            OrderResult = OrderResultMock.BuildOrderResult(),
            Exam = examModel
        };
        var findings = new List<string> { amdFinding };

        var examResultModel = new ExamResultModel();
        if (isRightSide)
        {
            examResultModel.RightEyeFindings = findings;
        }
        else
        {
            examResultModel.LeftEyeFindings = findings;
        }

        A.CallTo(() => _mapper.Map<ExamResultModel>(processIrisOrderResult)).Returns(examResultModel);

        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));

        // Act
        await _handler.Handle(processIrisOrderResult, CancellationToken.None);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateExamResultRecord>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.IRISExamCreated.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(s => s.ExamStatusCode.Name == ExamStatusCode.IRISInterpreted.Name), A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
    }
}