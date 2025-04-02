using AutoMapper;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;
using Signify.DEE.Messages;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public sealed class GetResultReceivedDataTest
{
    private readonly GetResultReceivedDataHandler _subject;
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly DataContext _context;

    public GetResultReceivedDataTest()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _context = new DataContext(options);
        _subject = new GetResultReceivedDataHandler(A.Dummy<ILogger<GetResultReceivedDataHandler>>(), _context, _mapper);
    }

    [Fact]
    public async Task Handle_WhenExamNotFound_ReturnsNull()
    {
        //Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => _subject.Handle(new GetResultReceivedData(1), default));
        A.CallTo(() => _mapper.Map<Result>(A<Exam>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenExamFound_ReturnsMappedResults()
    {
        const int examId = 1;

        await _context.Exams.AddAsync(new Exam
        {
            ExamId = examId
        });
        await _context.SaveChangesAsync();

        A.CallTo(() => _mapper.Map<Result>(A<Exam>._))
            .Returns(new Result());

        var actual = await _subject.Handle(new GetResultReceivedData(examId), default);

        A.CallTo(() => _mapper.Map<Result>(A<Exam>._))
            .MustHaveHappened();

        Assert.NotNull(actual);
    }
}