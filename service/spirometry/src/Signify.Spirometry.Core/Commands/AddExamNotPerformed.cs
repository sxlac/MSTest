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
    /// <summary>
    /// Command to add details about why a Spirometry exam was not performed to database
    /// </summary>
    public class AddExamNotPerformed : IRequest<ExamNotPerformed>
    {
        public SpirometryExam SpirometryExam { get; }

        public NotPerformedInfo Info { get; }

        public AddExamNotPerformed(SpirometryExam spirometryExam, NotPerformedInfo info)
        {
            SpirometryExam = spirometryExam;
            Info = info;
        }
    }

    public class AddExamNotPerformedHandler : IRequestHandler<AddExamNotPerformed, ExamNotPerformed>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _spirometryDataContext;
        private readonly IMapper _mapper;

        public AddExamNotPerformedHandler(ILogger<AddExamNotPerformedHandler> logger,
            SpirometryDataContext spirometryDataContext,
            IMapper mapper)
        {
            _logger = logger;
            _spirometryDataContext = spirometryDataContext;
            _mapper = mapper;
        }

        public async Task<ExamNotPerformed> Handle(AddExamNotPerformed request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<ExamNotPerformed>(request.SpirometryExam);
            _mapper.Map(request.Info, entity);

            entity = (await _spirometryDataContext.ExamNotPerformeds.AddAsync(entity, cancellationToken)).Entity;

            await _spirometryDataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully inserted a new ExamNotPerformed record for SpirometryExamId={SpirometryExamId}; new ExamNotPerformedId={ExamNotPerformedId}",
                entity.SpirometryExamId, entity.ExamNotPerformedId);

            return entity;
        }
    }
}
