using FakeItEasy;
using Microsoft.Extensions.Logging;
using Refit;
using Signify.Spirometry.Core.ApiClients.AppointmentApi.Responses;
using Signify.Spirometry.Core.ApiClients.AppointmentApi;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.ApiClients.EvaluationApi;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryEvaluationIdHandlerTests
{
    private readonly IAppointmentApi _appointmentApi = A.Fake<IAppointmentApi>();
    private readonly IEvaluationApi _evaluationApi = A.Fake<IEvaluationApi>();

    private QueryEvaluationIdHandler CreateSubject(SpirometryDataContext context)
        => new(A.Dummy<ILogger<QueryEvaluationIdHandler>>(), context, _appointmentApi, _evaluationApi);

    [Fact]
    public async Task Handle_WhenSpirometryExamFound_ReturnsEvaluationId()
    {
        // Arrange
        const long appointmentId = 1;
        const int evaluationId = 2;
            
        var request = new QueryEvaluationId(appointmentId);

        await using var context = new MockDbFixture();

        await context.SharedDbContext.SpirometryExams.AddAsync(new SpirometryExam
        {
            AppointmentId = appointmentId,
            EvaluationId = evaluationId
        });
        await context.SharedDbContext.SaveChangesAsync();

        // Act
        var subject = CreateSubject(context.SharedDbContext);

        var actual = await subject.Handle(request, default);

        // Assert
        Assert.Equal(evaluationId, actual);

        A.CallTo(_appointmentApi)
            .MustNotHaveHappened();
        A.CallTo(_evaluationApi)
            .MustNotHaveHappened();
    }

    private static IApiResponse<T> CreateUnsuccessfulApiResponse<T>()
    {
        var apiResponse = A.Fake<IApiResponse<T>>();

        A.CallTo(() => apiResponse.IsSuccessStatusCode)
            .Returns(false);

        return apiResponse;
    }

    /// <summary>
    /// Creates a successful response with the included content
    /// </summary>
    private static IApiResponse<T> CreateApiResponse<T>(T content)
    {
        var apiResponse = A.Fake<IApiResponse<T>>();

        A.CallTo(() => apiResponse.IsSuccessStatusCode)
            .Returns(true);

        A.CallTo(() => apiResponse.Content)
            .Returns(content);

        return apiResponse;
    }

    [Fact]
    public async Task Handle_WhenSpirometryExamNotFound_GetsEvaluationIdFromApis()
    {
        // Arrange
        const long memberPlanId = 10;
        const long appointmentId = 20;
        const long evaluationId = 30;

        A.CallTo(() => _appointmentApi.GetAppointment(A<long>._))
            .Returns(CreateApiResponse(new Appointment
            {
                AppointmentId = appointmentId,
                MemberPlanId = memberPlanId
            }));

        A.CallTo(() => _evaluationApi.GetEvaluations(A<long>._))
            .Returns(CreateApiResponse(new List<Evaluation>
            {
                new()
                {
                    AppointmentId = appointmentId + 1, // Some other appointment
                    Id = evaluationId + 1 // Some other evaluation
                },
                new() // The one we want
                {
                    AppointmentId = appointmentId,
                    Id = evaluationId
                },
                new()
                {
                    AppointmentId = appointmentId + 2, // Some other appointment
                    Id = evaluationId + 2 // Some other evaluation
                }
            }));

        var request = new QueryEvaluationId(appointmentId);

        await using var context = new MockDbFixture();

        // Act
        var actualEvaluationId = await CreateSubject(context.SharedDbContext).Handle(request, default);

        // Assert
        A.CallTo(() => _appointmentApi.GetAppointment(appointmentId))
            .MustHaveHappened();

        A.CallTo(() => _evaluationApi.GetEvaluations(memberPlanId))
            .MustHaveHappened();

        Assert.Equal(evaluationId, actualEvaluationId);
    }

    [Fact]
    public async Task Handle_WhenUnsuccessfulStatusCodeFromSchedulingApi_ThrowsGetAppointmentException()
    {
        // Arrange
        var request = new QueryEvaluationId(1);

        A.CallTo(() => _appointmentApi.GetAppointment(A<long>._))
            .Returns(CreateUnsuccessfulApiResponse<Appointment>());

        await using var context = new MockDbFixture();

        var subject = CreateSubject(context.SharedDbContext);

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<GetAppointmentException>(async () => await subject.Handle(request, default));
    }

    [Fact]
    public async Task Handle_WhenNoAppointmentReturnedFromSchedulingApi_ThrowsGetAppointmentException()
    {
        // Arrange
        var request = new QueryEvaluationId(1);

        A.CallTo(() => _appointmentApi.GetAppointment(A<long>._))
            .Returns(CreateApiResponse((Appointment) null));

        await using var context = new MockDbFixture();

        var subject = CreateSubject(context.SharedDbContext);

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<GetAppointmentException>(async () => await subject.Handle(request, default));
    }

    [Fact]
    public async Task Handle_WhenUnsuccessfulStatusCodeReturnedFromEvaluationApi_ThrowsGetEvaluationsException()
    {
        // Arrange
        const long appointmentId = 1;
        const long memberPlanId = 2;

        var request = new QueryEvaluationId(appointmentId);

        A.CallTo(() => _appointmentApi.GetAppointment(A<long>._))
            .Returns(CreateApiResponse(new Appointment
            {
                AppointmentId = appointmentId,
                MemberPlanId = memberPlanId
            }));

        A.CallTo(() => _evaluationApi.GetEvaluations(A<long>._))
            .Returns(CreateUnsuccessfulApiResponse<ICollection<Evaluation>>());

        await using var context = new MockDbFixture();

        var subject = CreateSubject(context.SharedDbContext);

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<GetEvaluationsException>(async () => await subject.Handle(request, default));
    }

    [Fact]
    public async Task Handle_WhenNoEvaluationsFoundForMemberPlanId_ThrowsGetEvaluationsException()
    {
        // Arrange
        var request = new QueryEvaluationId(1);

        A.CallTo(() => _appointmentApi.GetAppointment(A<long>._))
            .Returns(CreateApiResponse(new Appointment
            {
                AppointmentId = request.AppointmentId,
                MemberPlanId = 1
            }));

        A.CallTo(() => _evaluationApi.GetEvaluations(A<long>._))
            .Returns(CreateApiResponse(new List<Evaluation>()));

        await using var context = new MockDbFixture();

        var subject = CreateSubject(context.SharedDbContext);

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<GetEvaluationsException>(async () => await subject.Handle(request, default));
    }

    [Fact]
    public async Task Handle_WhenEvaluationNotFoundInDb_AndNotFoundInApis_ThrowsEvaluationNotFoundException()
    {
        // Arrange
        const long appointmentId = 1;
        const long memberPlanId = 2;

        var request = new QueryEvaluationId(appointmentId);

        A.CallTo(() => _appointmentApi.GetAppointment(A<long>._))
            .Returns(CreateApiResponse(new Appointment
            {
                AppointmentId = appointmentId,
                MemberPlanId = memberPlanId
            }));

        A.CallTo(() => _evaluationApi.GetEvaluations(A<long>._))
            .Returns(CreateApiResponse(new List<Evaluation>
            {
                new()
                {
                    AppointmentId = appointmentId + 1 // some other appointment id
                }
            }));

        await using var context = new MockDbFixture();

        var subject = CreateSubject(context.SharedDbContext);

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<EvaluationNotFoundException>(async () => await subject.Handle(request, default));
    }
}