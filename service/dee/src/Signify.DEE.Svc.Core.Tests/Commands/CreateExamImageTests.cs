using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class CreateExamImageTests
{
    private readonly CreateExamImageHandler _createExamImageHandler;
    private readonly DataContext _context;

    public CreateExamImageTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName: "DEE_CREATE_EXAM_IMAGE_TEST").Options;
        _context = new DataContext(options);
        _createExamImageHandler = new CreateExamImageHandler(A.Dummy<ILogger<CreateExamImageHandler>>(), _context);
    }

    [Fact]
    public async Task Handle_ImageResultReturned()
    {
        var createExamImage = new CreateExamImage
        {
            Exam = new ExamModel
            {
                ExamId = 1
            }
        };

        var result = await _createExamImageHandler.Handle(createExamImage, CancellationToken.None);

        Assert.Equal(createExamImage.Exam.ExamId, result.ExamId);
        Assert.NotNull(result.ImageLocalId);
        Assert.Equal(1, result.ExamImageId);
    }
}