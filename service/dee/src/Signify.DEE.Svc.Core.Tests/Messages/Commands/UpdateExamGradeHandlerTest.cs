using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class UpdateExamGradeHandlerTest
{

    private readonly ILogger<UpdateExamGradeHandler> _log;
    private readonly IMapper _mapper;
    private readonly DataContext _context;
    private UpdateExamGradeHandler _handler;

    public UpdateExamGradeHandlerTest()
    {
        _log = A.Fake<ILogger<UpdateExamGradeHandler>>();
        _mapper = A.Fake<IMapper>();
        _context = A.Fake<DataContext>();
    }

    [Fact]
    public async Task Exam_NotGraded_To_Graded()
    {
        //Arrange
        var request = new UpdateExamGrade() { ExamId = 1, Gradable = true };
        var exams = new Exam
        {
            ExamId = 1,
            Gradeable = false
        };

        var fakeDbSet = FakeDbSet(exams);

        _handler = new UpdateExamGradeHandler(_context);
        A.CallTo(() => _context.Exams).Returns(fakeDbSet);

        //Act
        var actual = await _handler.Handle(request, CancellationToken.None);

        //Assert
        fakeDbSet.FirstOrDefault().Gradeable.Should().BeTrue();
        actual.Should().Be(MediatR.Unit.Value);
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