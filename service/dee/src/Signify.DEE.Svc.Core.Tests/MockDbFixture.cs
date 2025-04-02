using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;

namespace Signify.DEE.Svc.Core.Tests;

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
///
/// </summary>
public sealed class MockDbFixture : IAsyncDisposable, IDisposable
{
    public readonly DataContext FakeDatabaseContext;

    private Fixture Fixture { get; }

    public MockDbFixture()
    {
        Fixture = new Fixture();
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior()); //recursionDepth
        var databaseName = $"NonReusedDatabase_{GenerateNextId()}";
        FakeDatabaseContext = new DataContext(GetDbOptions(databaseName));
        PopulateFakeData(FakeDatabaseContext);
    }

    private static void PopulateFakeData(DataContext context)
    {
        var isDirty = false;

        if (!context.EvaluationObjective.Any())
        {
            context.EvaluationObjective.Add(new EvaluationObjective { EvaluationObjectiveId = 1, Objective = "Comprehensive" });
            context.EvaluationObjective.Add(new EvaluationObjective { EvaluationObjectiveId = 2, Objective = "Focused" });
        }

        var deeExamOne = ExamEntityMock.BuildExam(45645, 55645, 300000, 200000);
        var deeExamTwo = ExamEntityMock.BuildExam(45646, 55646, 300001, 200001);
        var deeExamThree = ExamEntityMock.BuildExam(45647, 55647, 300002, 200002);
        var deeExamFour = ExamEntityMock.BuildExam(45648, 55648, 300003, 200003);

        if (!context.Exams.Any())
        {
            var deeExams = new List<Exam>
            {
                deeExamOne,
                deeExamTwo,
                deeExamThree,
                deeExamFour
            };

            context.Exams.AddRange(deeExams);
            isDirty = true;

            PopulateBillingRequests(deeExams, context);
        }

        if (!context.ExamStatusCodes.Any())
        {
            var examStatusCodes = new List<ExamStatusCode>
            {
                ExamStatusCode.Create(ExamStatusCode.ExamCreated.Name),
                ExamStatusCode.Create(ExamStatusCode.IRISAwaitingInterpretation.Name),
                ExamStatusCode.Create(ExamStatusCode.IRISInterpreted.Name),
                ExamStatusCode.Create(ExamStatusCode.ResultDataDownloaded.Name),
                ExamStatusCode.Create(ExamStatusCode.PDFDataDownloaded.Name),
                ExamStatusCode.Create(ExamStatusCode.SentToBilling.Name),
                ExamStatusCode.Create(ExamStatusCode.NoDEEImagesTaken.Name),
                ExamStatusCode.Create(ExamStatusCode.IRISImageReceived.Name),
                ExamStatusCode.Create(ExamStatusCode.Gradable.Name),
                ExamStatusCode.Create(ExamStatusCode.NotGradable.Name),
                ExamStatusCode.Create(ExamStatusCode.DEEImagesFound.Name),
                ExamStatusCode.Create(ExamStatusCode.IRISExamCreated.Name),
                ExamStatusCode.Create(ExamStatusCode.IRISResultDownloaded.Name),
                ExamStatusCode.Create(ExamStatusCode.PCPLetterSent.Name),
                ExamStatusCode.Create(ExamStatusCode.NoPCPFound.Name),
                ExamStatusCode.Create(ExamStatusCode.MemberLetterSent.Name),
                ExamStatusCode.Create(ExamStatusCode.SentToProviderPay.Name),
                ExamStatusCode.Create(ExamStatusCode.Performed.Name),
                ExamStatusCode.Create(ExamStatusCode.NotPerformed.Name),
                ExamStatusCode.Create(ExamStatusCode.BillableEventRecieved.Name),
                ExamStatusCode.Create(ExamStatusCode.Incomplete.Name),
                ExamStatusCode.Create(ExamStatusCode.BillRequestNotSent.Name)
            };

            context.ExamStatusCodes.AddRange(examStatusCodes);
            isDirty = true;
        }

        if (!context.ExamImages.Any())
        {
            var examImages = new List<ExamImage>
            {
                new()
                {
                    ExamImageId = 1,
                    Exam = deeExamFour,
                    ExamId = deeExamFour.ExamId,
                    ImageType = "Original",
                    LateralityCodeId = 1,
                    NotGradableReasons = string.Empty,
                    Gradable = true,
                    ImageLocalId = "55"
                },
                new()
                {
                    ExamImageId = 2,
                    Exam = deeExamFour,
                    ExamId = deeExamFour.ExamId,
                    ImageType = "Original",
                    LateralityCodeId = 2,
                    NotGradableReasons = string.Empty,
                    Gradable = true,
                    ImageLocalId = "56"
                },
            };

            context.ExamImages.AddRange(examImages);
            isDirty = true;
        }

        if (isDirty)
        {
            context.SaveChanges();
        }
    }

    private static void PopulateBillingRequests(IEnumerable<Exam> exams, DataContext context)
    {
        context.DEEBilling.AddRange(exams.Select(each => new DEEBilling { BillId = "0C58DF47-B8B7-48FF-BC4F-CC532B6DF653" }));
    }

    private static int _currentId;

    private static int GenerateNextId() => Interlocked.Increment(ref _currentId);

    private static DbContextOptions<DataContext> GetDbOptions(string databaseName)
        => new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    public void Dispose()
    {
        FakeDatabaseContext.Dispose();
    }

    public ValueTask DisposeAsync() => FakeDatabaseContext.DisposeAsync();
}