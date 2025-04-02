using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Signify.eGFR.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.eGFR.Core.Data.Entities;

namespace Signify.eGFR.Core.Tests;

/// <summary>
/// This fixture can be used as a shared db context or as a backbone for individual per-test db contexts.
/// Your test class should implement IClassFixture&lt;MockDbFixture&gt; to use it.
/// Then, in each test either:
/// 1)  var dbContext = _dbFixture._sharedDbContext;  (to use the shared context)
/// or
/// 2) var dbContext = new SqlDataContext(_dbFixture.GetDbOptions("{unique_name}"));  (to use a unique context).
/// Shared contexts are faster but you must be careful that the test data inside the context cannot mess up another test.
/// Tests that test for specific *counts* of data records will need their own context, as it can't be guaranteed that no other test data might accidentally qualify.
/// Be sure to use the NextId function when assigning entity ids to avoid conflicts since the automated generation of ids
/// is not guaranteed to be unique.
/// As such, tests of code blocks that add new data records will circumvent the NextId feature and will need their own context.
/// </summary>
public sealed class MockDbFixture : IAsyncDisposable, IDisposable
{
    public DataContext SharedDbContext { get; }
    private Fixture Fixture { get; }

    public MockDbFixture()
    {
        Fixture = new Fixture();
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior()); //recursionDepth

        var databaseName = $"NonReusedDatabase_{GenerateNextId()}";

        SharedDbContext = new DataContext(GetDbOptions(databaseName));
    }

    public static void PopulateFakeData(DataContext dataContext)
    {
        PopulateExams(dataContext);
        PopulateStatusCodes(dataContext);

        if (!dataContext.ExamStatuses.Any())
        {
            PopulateStatuses(dataContext, ExamStatusCode.ExamPerformed);
            PopulateStatuses(dataContext, ExamStatusCode.CdiPassedReceived, [2, 3]);
            PopulateStatuses(dataContext, ExamStatusCode.CdiFailedWithPayReceived, [3]);
            PopulateStatuses(dataContext, ExamStatusCode.CdiFailedWithoutPayReceived, [1, 2, 3]);
        }
    }

    /// <summary>
    /// Populate specified Exam with specified status in ExamStatus table
    /// </summary>
    /// <param name="evaluationIds"></param>
    /// <param name="dbContext"></param>
    /// <param name="statusCode"></param>
    private static void PopulateStatuses(DataContext dbContext, ExamStatusCode statusCode, long[] evaluationIds = null)
    {
        dbContext.ExamStatuses.AddRange(CreatePerformedExamStatus(dbContext, statusCode, evaluationIds));
        dbContext.SaveChanges();
    }

    /// <summary>
    /// Creates a list of ExamStatus with specified status for specified exam
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="statusCode"></param>
    /// <param name="evaluationIds"></param>
    /// <returns></returns>
    private static IEnumerable<ExamStatus> CreatePerformedExamStatus(DataContext dbContext, ExamStatusCode statusCode, long[] evaluationIds = null)
    {
        if (evaluationIds is null)
        {
            return dbContext.Exams.Select(exam => new ExamStatus { Exam = exam, ExamStatusCode = statusCode }).ToList();
        }

        var examStatuses = new List<ExamStatus>();

        foreach (var evalId in evaluationIds)
        {
            examStatuses.AddRange(dbContext.Exams.Where(exam => exam.EvaluationId == evalId)
                .Select(exam => new ExamStatus { Exam = exam, ExamStatusCode = statusCode })
            );
        }

        return examStatuses;
    }

    /// <summary>
    /// Populates the ExamStatusCode table
    /// </summary>
    private static void PopulateStatusCodes(DataContext dataContext)
    {
        if (!dataContext.ExamStatusCodes.Any())
        {
            dataContext.ExamStatusCodes.AddRange(ExamStatusCode.All);
        }

        dataContext.SaveChanges();
    }

    private static void PopulateExams(DataContext dataContext)
    {
        if (!dataContext.Exams.Any())
        {
            var exams = new List<Exam>
            {
                new()
                {
                    ExamId = 1, AddressLineOne = "TheAddressLineOne", AddressLineTwo = "TheAddressLineTwo", ApplicationId = "Signify.Evaluation.Service",
                    AppointmentId = 1000011111, CenseoId = "Test1234", City = "TheCity", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
                    DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 1, FirstName = "FN", LastName = "LN",
                    MemberId = 100001, MemberPlanId = 200002, NationalProviderIdentifier = "300003", ProviderId = 40004, State = "TheState", ZipCode = "123456"
                },
                new()
                {
                    ExamId = 2, AddressLineOne = "TheAddressLineOne", AddressLineTwo = "TheAddressLineTwo", ApplicationId = "Signify.Evaluation.Service",
                    AppointmentId = 1000022222, CenseoId = "Test1235", City = "TheCity", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
                    DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 2, FirstName = "FN", LastName = "LN",
                    MemberId = 100002, MemberPlanId = 200003, NationalProviderIdentifier = "300004", ProviderId = 40005, State = "TheState", ZipCode = "123456"
                },
                new()
                {
                    ExamId = 3, AddressLineOne = "TheAddressLineOne", AddressLineTwo = "TheAddressLineTwo", ApplicationId = "Signify.Evaluation.Service",
                    AppointmentId = 1000033333, CenseoId = "Test1235", City = "TheCity", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
                    DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 3, FirstName = "FN", LastName = "LN",
                    MemberId = 100003, MemberPlanId = 200004, NationalProviderIdentifier = "300005", ProviderId = 40006, State = "TheState", ZipCode = "123456"
                }
            };
            dataContext.Exams.AddRange(exams);
        }

        dataContext.SaveChanges();
    }

    private static int _currentId = 0;

    private static int GenerateNextId()
    {
        return Interlocked.Increment(ref _currentId);
    }

    private static DbContextOptions<DataContext> GetDbOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    public void Dispose()
    {
        SharedDbContext.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return SharedDbContext.DisposeAsync();
    }
}