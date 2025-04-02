using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    public class AddExamResults : IRequest<SpirometryExamResult>
    {
        public int SpirometryExamId { get; }
        public ExamResult ExamResult { get; }

        public AddExamResults(int spirometryExamId, ExamResult examResult)
        {
            SpirometryExamId = spirometryExamId;
            ExamResult = examResult;
        }
    }

    public class AddExamResultsHandler : IRequestHandler<AddExamResults, SpirometryExamResult>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _spirometryDataContext;
        private readonly IMapper _mapper;

        public AddExamResultsHandler(ILogger<AddExamResultsHandler> logger,
            IMapper mapper,
            SpirometryDataContext spirometryDataContext)
        {
            _logger = logger;
            _mapper = mapper;
            _spirometryDataContext = spirometryDataContext;
        }

        public async Task<SpirometryExamResult> Handle(AddExamResults request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<SpirometryExamResult>(request.ExamResult);
            entity.SpirometryExamId = request.SpirometryExamId;

            entity = (await _spirometryDataContext.SpirometryExamResults.AddAsync(entity, cancellationToken)
                    .ConfigureAwait(false)).Entity;

            await _spirometryDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully inserted a new SpirometryExamResult record for SpirometryExamId={SpirometryExamId}. New SpirometryExamResultId={SpirometryExamResultId}",
                entity.SpirometryExamId, entity.SpirometryExamResultsId);

            return entity;
        }
    }
}