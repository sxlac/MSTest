using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Refit;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Infrastructure;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to create a clarification flag in CDI
    /// </summary>
    /// <remarks>
    /// Also makes a request to save the flag to the database if successful
    /// </remarks>
    public class CreateFlag : IRequest<ClarificationFlag>
    {
        public SpirometryExam Exam { get; init; }
        public SpirometryExamResult Results { get; init; }
    }

    public class CreateFlagHandler : IRequestHandler<CreateFlag, ClarificationFlag>
    {
        private readonly ILogger _logger;
        private readonly IApplicationTime _applicationTime;
        private readonly ICdiFlagsApi _api;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public CreateFlagHandler(ILogger<CreateFlagHandler> logger,
            IApplicationTime applicationTime,
            ICdiFlagsApi api,
            IMapper mapper,
            IMediator mediator)
        {
            _logger = logger;
            _applicationTime = applicationTime;
            _api = api;
            _mapper = mapper;
            _mediator = mediator;
        }

        [Transaction]
        public async Task<ClarificationFlag> Handle(CreateFlag request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending save flag request to the CDI API for EvaluationId={EvaluationId}", request.Exam.EvaluationId);

            var saveFlagRequest = _mapper.Map<SaveSystemFlagRequest>(request.Exam);
            _mapper.Map(request.Results, saveFlagRequest);

            var response = await _api.CreateFlag(saveFlagRequest);

            ValidateSuccessful(request.Exam, response);

            var cdiFlagId = response.Content.Flag.FlagId;

            _logger.LogInformation("Successfully created CdiFlagId={CdiFlagId} for EvaluationId={EvaluationId}", cdiFlagId, request.Exam.EvaluationId);

            var addCommand = new AddClarificationFlag(new ClarificationFlag
            {
                CdiFlagId = cdiFlagId,
                SpirometryExamId = request.Exam.SpirometryExamId,
                CreateDateTime = _applicationTime.UtcNow()
            });

            return await _mediator.Send(addCommand, cancellationToken);
        }

        private void ValidateSuccessful(SpirometryExam exam, IApiResponse response)
        {
            // Response codes returned by API:
            // OK (200)
            // Bad Request (400)
            // Forbidden (403)
            // Not Found (404)

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Received StatusCode={StatusCode} from CDI API for EvaluationId={EvaluationId}", response.StatusCode, exam.EvaluationId);
                return;
            }

            if (response.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError)
                _logger.LogWarning("Received StatusCode={StatusCode} from CDI API for EvaluationId={EvaluationId}", response.StatusCode, exam.EvaluationId);

            // Raise for NSB retry
            throw new CdiSaveFlagRequestException(exam.EvaluationId, response.StatusCode,
                "Unsuccessful HTTP status code returned", response.Error);
        }
    }
}
