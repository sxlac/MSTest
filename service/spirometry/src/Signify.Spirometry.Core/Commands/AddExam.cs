using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to Create or Update a <see cref="SpirometryExam"/> entity in db
    /// </summary>
    public class AddExam : IRequest<SpirometryExam>
    {
        public SpirometryExam Exam { get; }

        public AddExam(SpirometryExam exam)
        {
            Exam = exam;
        }
    }

    public class AddExamHandler : IRequestHandler<AddExam, SpirometryExam>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _spirometryDataContext;

        public AddExamHandler(ILogger<AddExamHandler> logger,
            SpirometryDataContext spirometryDataContext)
        {
            _logger = logger;
            _spirometryDataContext = spirometryDataContext;
        }

        public async Task<SpirometryExam> Handle(AddExam request, CancellationToken cancellationToken)
        {
            var entity = (await _spirometryDataContext.SpirometryExams.AddAsync(request.Exam, cancellationToken)
                .ConfigureAwait(false)).Entity;

            await _spirometryDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully inserted a new SpirometryExam record for EvaluationId={EvaluationId}. New SpirometryExamId={SpirometryExamId}",
                entity.EvaluationId, entity.SpirometryExamId);

            return entity;
        }
    }
}
