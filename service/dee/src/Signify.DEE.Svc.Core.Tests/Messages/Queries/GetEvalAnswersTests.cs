using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Refit;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetEvalAnswersTests
{
    private readonly GetEvalAnswersHandler _handler;
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();
    private readonly IProviderApi _providerApi = A.Fake<IProviderApi>();
    private readonly FakeApplicationTime _applicationTime = new();

    public GetEvalAnswersTests()
    {
        var logger = A.Dummy<ILogger<GetEvalAnswersHandler>>();
        _handler = new GetEvalAnswersHandler(logger, _evaluationApi, _providerApi);
    }

    [Fact]
    public async Task Should_Retrieve_Evaluation_Answers()
    {
        // Arrange
        var model = GetApiResponse();
        var provider = GetProviderApiResponse();
        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>._)).Returns(model);
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(provider);

        //Act
        var result = await _handler.Handle(CreateRequest(), CancellationToken.None);

        // Assert
        result.MemberPlanId.Should().Be(model.Content.Evaluation.MemberPlanId);
        result.ProviderNpi.Should().Be(provider.Content.NationalProviderIdentifier);
    }

    [Fact]
    public async Task Should_Retrieve_Evaluation_Answers_With_List()
    {
        // Arrange
        var model = GetApiResponse();
        var provider = GetProviderApiResponse();
        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>._)).Returns(model);
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(provider);

        //Act
        var result = await _handler.Handle(CreateRequest(), CancellationToken.None);

        // Assert
        result.MemberPlanId.Should().Be(model.Content.Evaluation.MemberPlanId);
        result.ProviderNpi.Should().Be(provider.Content.NationalProviderIdentifier);
        result.Answers.Should().HaveCount(1);
        result.CreatedDateTime.Date.Should().NotBeAfter(_applicationTime.UtcNow());
    }

    [Fact]
    public async Task Should_Handle_Null_Evaluation()
    {
        // Arrange
        var nullContent = GetNullEvaluationApiResponse();
        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>._)).Returns(nullContent);

        //Act
        var result = await _handler.Handle(CreateRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(null);
    }

    [Fact]
    public async Task Should_Handle_Null_Provider()
    {
        // Arrange
        var eval = GetApiResponse();
        var provider = GetProviderApiResponse(true);
        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>._)).Returns(eval);
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(provider);

        //Act
        var result = await _handler.Handle(CreateRequest(), CancellationToken.None);

        // Assert
        result.Should().Be(null);
    }


    [Fact]
    public async Task Handle_ShouldExtractRetinalImageTestingNotes()
    {
        // Arrange
        var model = GetApiResponseWithRetinalImageTestingNotes();
        var provider = GetProviderApiResponse();
        A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<long>._)).Returns(model);
        A.CallTo(() => _providerApi.GetProviderById(A<int>._)).Returns(provider);

        // Act
        var result = await _handler.Handle(CreateRequest(), CancellationToken.None);

        // Assert
        result.RetinalImageTestingNotes.Should().Be("Some Retinal Testing Notes");
    }

    private static GetEvalAnswers CreateRequest() => new()
    {
        EvaluationId = 1
    };

    private static ApiResponse<EvaluationVersion> GetApiResponse()
    {
        return new ApiResponse<EvaluationVersion>(new HttpResponseMessage(),
            new EvaluationVersion
            {
                Evaluation = new EvaluationModel
                {
                    ProviderId = 658,
                    MemberPlanId = 9870,
                    Answers = new List<EvaluationAnswer>
                    {
                        new()
                        {
                            AnswerId = 1,
                            AnswerValue = "test",
                            QuestionId = 1
                        }
                    }
                },
                Version = 1
            }, new RefitSettings());
    }

    private static ApiResponse<EvaluationVersion> GetNullEvaluationApiResponse() => new(new HttpResponseMessage(), null, new RefitSettings());

    private static ApiResponse<ProviderModel> GetProviderApiResponse(bool isNull = false)
    {
        return new ApiResponse<ProviderModel>(new HttpResponseMessage(), !isNull ? new ProviderModel
        {
            FirstName="FN",
            LastName="LN", NationalProviderIdentifier = "NPI78"
        } : null, new RefitSettings());
    }
    private static ApiResponse<EvaluationVersion> GetApiResponseWithRetinalImageTestingNotes()
    {
        return new ApiResponse<EvaluationVersion>(new HttpResponseMessage(),
            new EvaluationVersion
            {
                Evaluation = new EvaluationModel
                {
                    ProviderId = 658,
                    MemberPlanId = 9870,
                    Answers = new List<EvaluationAnswer>
                    {
                        new()
                        {
                            AnswerId = 50415,
                            AnswerValue = "Some Retinal Testing Notes"
                        }
                    }
                },
                Version = 1
            }, new RefitSettings());
    }
}