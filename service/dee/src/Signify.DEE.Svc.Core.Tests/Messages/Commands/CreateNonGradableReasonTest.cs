using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Tests.Utilities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.Logging;
using FakeItEasy;
using Signify.DEE.Svc.Core.Data;
using Microsoft.EntityFrameworkCore;
using Signify.DEE.Svc.Core.Data.Entities;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class CreateNonGradableReasonHandlerTest : IClassFixture<EntityFixtures>
{
    private readonly ILogger<CreateNonGradableReasonsHandler> _log;
    private CreateNonGradableReasonsHandler _handler;
    private DataContext _context;

    public CreateNonGradableReasonHandlerTest()
    {
        _log = A.Dummy<ILogger<CreateNonGradableReasonsHandler>>();
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DeeCreateNonGradableReasons").Options;
        _context = new DataContext(options);
        _handler = new CreateNonGradableReasonsHandler(_log, _context);
    }

    [Fact]
    public async Task Handler_SubmitNonGradableReasons_RecordAlreadyInTable()
    {
        // Arrange
        var request = new CreateNonGradableReasons
        {
            ExamLateralityGradeId = 1,
            Reasons = new List<string>() { "Image blurr", "Not an eye image" }
        };

        //Add initial record in DB
        var examNonGradableReasons = new List<NonGradableReason>()
        {
            new NonGradableReason
            {
                ExamLateralityGradeId = 1,
                Reason = "Image Blurr"
            },
            new NonGradableReason
            {
                ExamLateralityGradeId = 1,
                Reason = "Not an eye image"
            },
        };
        _context.NonGradableReason.AddRange(examNonGradableReasons);
        _context.SaveChanges();
            

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert Note : No new records will be added, since record already exists.
        var count = await _context.NonGradableReason.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Handler_SubmitNonGradableReasons_NewRecordCreated()
    {
        // Arrange
        var request = new CreateNonGradableReasons
        {
            ExamLateralityGradeId = 2,
            Reasons = new List<string>() { "Image blurr", "Not an eye image" }
        };

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert Note : 2 new records will be added. So the count will jump to 4
        var count = await _context.NonGradableReason.CountAsync();
        Assert.Equal(4, count);
    }
}