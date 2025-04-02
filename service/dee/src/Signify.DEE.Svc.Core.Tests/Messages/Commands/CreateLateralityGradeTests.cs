using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Tests.Mocks;
using Signify.DEE.Svc.Core.Tests.Utilities;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Signify.DEE.Svc.Core.Data;
using Microsoft.EntityFrameworkCore;



namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class CreateLateralityGradeHandlerTest : IClassFixture<EntityFixtures>
{
    private CreateLateralityGradeHandler _handler;
    private DataContext _context;
    public CreateLateralityGradeHandlerTest()
    {
        var _log = A.Dummy<ILogger<CreateLateralityGradeHandler>>();
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DeeCreateLateralityGrade").Options;
        _context = new DataContext(options);
        _handler = new CreateLateralityGradeHandler(_log, _context);
    }

    [Fact]
    public async Task Handler_SubmitLateralityGrade_RecordAlreadyInTable()
    {
        // Arrange
        var request = new CreateLateralityGrade
        {
            ExamModel = new ExamModel() { ExamId = 100 },
            Grading = ResultGradingMock.BuildResultEyeSideGrading(true, null),
            LateralityCode = ApplicationConstants.Laterality.RightEyeCode
        };

        _context.ExamLateralityGrade.Add(new ExamLateralityGrade
        {
            ExamId = 100,
            ExamLateralityGradeId = 1,
            LateralityCodeId = 1
        });
        _context.SaveChanges();

        // Act
        var actual = await _handler.Handle(request, CancellationToken.None);

        // Assert Note : Id 1 is already in the table.
        Assert.Equal(1, actual);
    }

    [Fact]
    public async Task Handler_SubmitLateralityGrade_NewRecordCreated()
    {
        // Arrange
        var request = new CreateLateralityGrade
        {
            ExamModel = new ExamModel() { ExamId = 101 },
            Grading = ResultGradingMock.BuildResultEyeSideGrading(true, null),
            LateralityCode = ApplicationConstants.Laterality.RightEyeCode
        };

        // Act
        var actual = await _handler.Handle(request, CancellationToken.None);

        // Assert Note Id : 2 will be a new generated Id
        Assert.Equal(2, actual);
    }
}