using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Refit;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Responses;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Infrastructure;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class CreateFlagHandlerTests
{
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly IApiResponse<SaveSystemFlagResponse> _fakeResponse = A.Fake<IApiResponse<SaveSystemFlagResponse>>();
    private readonly ICdiFlagsApi _api = A.Fake<ICdiFlagsApi>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();

    public CreateFlagHandlerTests()
    {
        A.CallTo(() => _api.CreateFlag(A<SaveSystemFlagRequest>._))
            .Returns(Task.FromResult(_fakeResponse));
    }

    private CreateFlagHandler CreateSubject()
        => new(A.Dummy<ILogger<CreateFlagHandler>>(), _applicationTime, _api, _mapper, _mediator);

    [Fact]
    public async Task Handle_WithUnsuccessfulStatusCode_Throws()
    {
        // Arrange
        var request = new CreateFlag
        {
            Exam = new SpirometryExam()
        };

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(false);
        A.CallTo(() => _fakeResponse.StatusCode)
            .Returns(HttpStatusCode.Unauthorized); // Really just anything other than 200-level

        // Act
        // Assert
        var subject = CreateSubject();
        await Assert.ThrowsAnyAsync<CdiSaveFlagRequestException>(async () => await subject.Handle(request, default));

        A.CallTo(() => _api.CreateFlag(A<SaveSystemFlagRequest>._))
            .MustHaveHappened();
        A.CallTo(_mediator)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_HappyPath()
    {
        // Arrange
        const int evaluationId = 1;
        const int spirometryExamId = 2;
        const int cdiFlagId = 3;
        const int clarificationFlagId = 4;

        var request = new CreateFlag
        {
            Exam = new SpirometryExam
            {
                EvaluationId = evaluationId,
                SpirometryExamId = spirometryExamId
            },
            Results = new SpirometryExamResult()
        };

        A.CallTo(() => _fakeResponse.IsSuccessStatusCode)
            .Returns(true);
        A.CallTo(() => _fakeResponse.Content)
            .Returns(new SaveSystemFlagResponse
            {
                Flag = new CdiSystemFlag
                {
                    FlagId = cdiFlagId
                }
            });
        A.CallTo(() => _mediator.Send(A<AddClarificationFlag>._, A<CancellationToken>._))
            .Returns(new ClarificationFlag
            {
                ClarificationFlagId = clarificationFlagId
            });

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        A.CallTo(() => _mapper.Map<SaveSystemFlagRequest>(A<SpirometryExam>._))
            .MustHaveHappened();
        A.CallTo(() => _mapper.Map(A<SpirometryExamResult>._, A<SaveSystemFlagRequest>._))
            .MustHaveHappened();

        Assert.Equal(clarificationFlagId, actual.ClarificationFlagId);

        A.CallTo(() => _api.CreateFlag(A<SaveSystemFlagRequest>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<AddClarificationFlag>.That.Matches(c =>
                    c.Flag.SpirometryExamId == spirometryExamId &&
                    c.Flag.CdiFlagId == cdiFlagId &&
                    c.Flag.CreateDateTime == _applicationTime.UtcNow()),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}