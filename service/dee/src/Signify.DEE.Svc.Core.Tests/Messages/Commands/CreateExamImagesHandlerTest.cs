using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class CreateExamImagesHandlerTest
{
    private readonly ILogger<CreateExamImagesHandler> _log;
    private readonly IMapper _mapper;
    private readonly DataContext _context;
    private CreateExamImagesHandler _handler;

    public CreateExamImagesHandlerTest()
    {
        _log = A.Fake<ILogger<CreateExamImagesHandler>>();
        _mapper = A.Fake<IMapper>();
        _context = A.Fake<DataContext>();
        _handler = new CreateExamImagesHandler(_log, _mapper, _context);
    }

    [Fact]
    public Task Should_Not_Throw_Exception()
    {
        // Arrange
        var request = new CreateExamImages { ExamId = 12, Images = new List<ExamImageModel>() };
        _handler = new CreateExamImagesHandler(_log, _mapper, _context);

        // Act 
        var result = Record.ExceptionAsync(() => _handler.Handle(request, CancellationToken.None));

        //Assert
        Assert.Null(result.Exception);
        return Task.CompletedTask;
    }

    /*
    [Fact]
    public async Task Should_Not_Throw_Exception_When_examImage_Is_Updated()
    {
        // Arrange
        var exam = new Exam { ExamId = 12 };
        var fakeDbSet = FakeDbSet(exam);
        var serviceProvider = ServiceProvider();
        var request = new CreateExamImages { ExamId = 12, Images = new List<ExamImageModel> { new ExamImageModel { ExamId = 12, ImageId = 12, ImageQuality = "High", ImageType = "jpeg" } } };
        var model = new ExamImage { ExamId = 12, ImageQuality = "High", ImageType = "jpeg", DeeImageId = 1212, Exam = exam, ExamImageId = 21, LateralityCode = default, LateralityCodeId = 4 };
        _handler = new CreateExamImagesHandler(_log, _mapper, serviceProvider);
        A.CallTo(() => _context.Exams).Returns(fakeDbSet);
        A.CallTo(() => _mapper.Map<ExamImage>(A<ExamImageModel>._)).Returns(model);

        // Act
        var response = await _handler.Handle(request, CancellationToken.None);

        //Assert
        response.Should().NotBeOfType<Exception>();
    }
    */

    [Fact]
    public async Task Should_Throw_ArgumentNullException()
    {
        // Arrange
        var exam = new Exam { ExamId = 1, EvaluationId = 12 };
        var fakeDbSet = FakeDbSet(exam);
        var request = new CreateExamImages { ExamId = 12, Images = new List<ExamImageModel> { new ExamImageModel { } } };
        _handler = new CreateExamImagesHandler(_log, _mapper, _context);
        A.CallTo(() => _context.Exams).Returns(fakeDbSet);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _handler.Handle(request, CancellationToken.None));
    }

    private static DbSet<Exam> FakeDbSet(Exam exams)
    {
        var fakeIQueryable = new List<Exam> { exams }.AsQueryable();
        var fakeDbSet = A.Fake<DbSet<Exam>>((d => d.Implements(typeof(IQueryable<Exam>))));
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).GetEnumerator()).Returns(fakeIQueryable.GetEnumerator());
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Provider).Returns(fakeIQueryable.Provider);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Expression).Returns(fakeIQueryable.Expression);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).ElementType).Returns(fakeIQueryable.ElementType);
        return fakeDbSet;
    }
}