using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.Spirometry.Core.ApiClients.EvaluationApi;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.ApiClients.AppointmentApi;
using Signify.Spirometry.Core.ApiClients.AppointmentApi.Responses;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    public class QueryEvaluationId : IRequest<long>
    {
        public long AppointmentId { get; }

        public QueryEvaluationId(long appointmentId)
        {
            AppointmentId = appointmentId;
        }
    }

    public class QueryEvaluationIdHandler : IRequestHandler<QueryEvaluationId, long>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _context;
        private readonly IAppointmentApi _appointmentApi;
        private readonly IEvaluationApi _evaluationApi;

        public QueryEvaluationIdHandler(ILogger<QueryEvaluationIdHandler> logger,
            SpirometryDataContext context,
            IAppointmentApi appointmentApi,
            IEvaluationApi evaluationApi)
        {
            _logger = logger;
            _context = context;
            _appointmentApi = appointmentApi;
            _evaluationApi = evaluationApi;
        }

        [Transaction]
        public async Task<long> Handle(QueryEvaluationId request, CancellationToken cancellationToken)
        {
            var exam = await GetExamFromDb(request, cancellationToken);

            if (exam != null)
                return exam.EvaluationId;

            _logger.LogInformation("No exam found in db with AppointmentId={AppointmentId}, querying APIs to determine EvaluationId",
                request.AppointmentId);

            try
            {
                return await GetEvaluationIdFromApis(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to determine EvaluationId associated with AppointmentId={AppointmentId}", request.AppointmentId);
                throw;
            }
        }

        [Trace]
        private Task<SpirometryExam> GetExamFromDb(QueryEvaluationId request, CancellationToken cancellationToken)
        {
            return _context
                .SpirometryExams
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.AppointmentId == request.AppointmentId, cancellationToken);
        }

        private async Task<long> GetEvaluationIdFromApis(QueryEvaluationId request)
        {
            var appointment = await GetAppointment(request.AppointmentId);

            var evaluation = await GetEvaluation(appointment);

            return evaluation?.Id ?? throw new EvaluationNotFoundException(request.AppointmentId);
        }

        [Trace]
        private async Task<Appointment> GetAppointment(long appointmentId)
        {
            var response = await _appointmentApi.GetAppointment(appointmentId);

            if (!response.IsSuccessStatusCode)
            {
                throw new GetAppointmentException(appointmentId, response.StatusCode,
                    "Unsuccessful HTTP status code returned from the Appointment API", response.Error);
            }

            if (response.Content == null)
            {
                throw new GetAppointmentException(appointmentId, response.StatusCode,
                    "No appointment returned from the Appointment API");
            }

            return response.Content;
        }

        [Trace]
        private async Task<Evaluation> GetEvaluation(Appointment appointment)
        {
            var memberPlanId = appointment.MemberPlanId;

            var response = await _evaluationApi.GetEvaluations(memberPlanId);

            if (!response.IsSuccessStatusCode)
            {
                throw new GetEvaluationsException(memberPlanId, appointment.AppointmentId, response.StatusCode,
                    "Unsuccessful HTTP status code returned from the Evaluation API", response.Error);
            }

            var evaluations = response.Content;
            if (evaluations == null || !evaluations.Any())
            {
                throw new GetEvaluationsException(memberPlanId, appointment.AppointmentId, response.StatusCode,
                    "No evaluations returned from the the Evaluation API");
            }

            _logger.LogInformation("Found {Count} evaluations ({EvaluationIds}) associated with the member tied to AppointmentId={AppointmentId}",
                evaluations.Count, string.Join(',', evaluations.Select(each => each.Id)), appointment.AppointmentId);

            return evaluations.FirstOrDefault(each => each.AppointmentId == appointment.AppointmentId);
        }
    }
}
