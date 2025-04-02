namespace Signify.uACR.Core.Tests.Queries;
   /* public class QueryExamHandlerTests
    {
        [Fact]
        public async Task Handle_WithRequest_QueriesDatabaseByEvaluationId()
        {
            const int evaluationId = 1;

            var request = new QueryExam(evaluationId);

            await using var fixture = new MockDbFixture();

            var expectedExam = new Exam
            {
                EvaluationId = evaluationId,
                ApplicationId = nameof(ApplicationId)
            };

            fixture.SharedDbContext.Exams.Add(expectedExam);
            await fixture.SharedDbContext.SaveChangesAsync();

            var subject = new QueryExamHandler(fixture.SharedDbContext);

            var actualResult = await subject.Handle(request, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(expectedExam, actualResult);
        }
    }*/
