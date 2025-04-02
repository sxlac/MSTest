using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetExamHandlerTests
{
    private readonly ILogger<GetExamHandler> _logger = A.Fake<ILogger<GetExamHandler>>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private GetExamHandler _handler;
    private readonly DataContext _context = A.Fake<DataContext>();

    [Fact]
    public async Task Should_Retrieve_Exam_Record()
    {
        // Arrange
        var exams = new Exam
        {
            ExamId = 1
        };
        var model = new ExamModel
        {
            ExamId = 1
        };

        IQueryable<Exam> fakeIQueryable = new List<Exam> { exams }.AsQueryable();
        var fakeDbSet = A.Fake<DbSet<Exam>>((d => d.Implements(typeof(IQueryable<Exam>))));
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).GetEnumerator()).Returns(fakeIQueryable.GetEnumerator());
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Provider).Returns(fakeIQueryable.Provider);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Expression).Returns(fakeIQueryable.Expression);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).ElementType).Returns(fakeIQueryable.ElementType);

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(_context);

        _handler = new GetExamHandler(_logger, _context, _mapper);

        A.CallTo(() => _context.Exams).Returns(fakeDbSet);
        A.CallTo(() => _mapper.Map<ExamModel>(A<Exam>._)).Returns(model);

        //Act
        var result = await _handler.Handle(Request, CancellationToken.None);

        // Assert
        result.ExamId.Should().Be(exams.ExamId);
    }

    [Fact]
    public async Task Should_Retrieve_Exam_With_RetinalImageTestingNotes()
    {
        // Arrange
        var retinalImageTestingNotes = "Some Retinal Testing Notes";
        var exams = new Exam
        {
            ExamId = 1,
            RetinalImageTestingNotes = retinalImageTestingNotes
        };
        var model = new ExamModel
        {
            ExamId = 1,
            RetinalImageTestingNotes = retinalImageTestingNotes
        };

        IQueryable<Exam> fakeIQueryable = new List<Exam> { exams }.AsQueryable();
        var fakeDbSet = A.Fake<DbSet<Exam>>((d => d.Implements(typeof(IQueryable<Exam>))));
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).GetEnumerator()).Returns(fakeIQueryable.GetEnumerator());
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Provider).Returns(fakeIQueryable.Provider);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Expression).Returns(fakeIQueryable.Expression);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).ElementType).Returns(fakeIQueryable.ElementType);

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(_context);

        _handler = new GetExamHandler(_logger, _context, _mapper);

        A.CallTo(() => _context.Exams).Returns(fakeDbSet);
        A.CallTo(() => _mapper.Map<ExamModel>(A<Exam>._)).Returns(model);

        //Act
        var result = await _handler.Handle(Request, CancellationToken.None);

        // Assert
        result.ExamId.Should().Be(exams.ExamId);
        result.RetinalImageTestingNotes.Should().Be(retinalImageTestingNotes);
    }

    private static GetExamRecord Request => new()
    {
        ExamId = 1,
        EvaluationId = 1
    };
}