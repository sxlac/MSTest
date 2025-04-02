using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Models;

namespace Signify.FOBT.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateLabResult : IRequest<LabResults>
{
    public int LabResultId { get; set; }
    public int FOBTId { get; set; }
    public Guid OrderCorrelationId { get; set; }
    public string Barcode { get; set; }
    public string LabResult { get; set; }
    public string ProductCode { get; set; }
    public string AbnormalIndicator { get; set; }
    public string Exception { get; set; }
    public DateTime? CollectionDate { get; set; }
    public DateTime? ServiceDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
}

public class CreateLabResultHandler : IRequestHandler<CreateLabResult, LabResults>
{
    private readonly ILogger _logger;
    private readonly FOBTDataContext _context;
    private readonly IMapper _mapper;
    private readonly IBillableRules _billableRules;

    public CreateLabResultHandler(ILogger<CreateLabResultHandler> logger,
        FOBTDataContext context,
        IMapper mapper,
        IBillableRules billableRules)
    {
        _logger = logger;
        _context = context;
        _mapper = mapper;
        _billableRules = billableRules;
    }

    [Trace]
    public async Task<LabResults> Handle(CreateLabResult request, CancellationToken cancellationToken)
    {
        var existingLabResult = await _context.LabResults
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FOBTId == request.FOBTId, cancellationToken);

        //Insert only when existing results are invalid
        if (existingLabResult == default || !IsLabResultValid(existingLabResult))
        {
            var labResults = _mapper.Map<LabResults>(request);

            labResults.CreatedDateTime = DateTime.UtcNow;
            if (!IsLabResultValid(labResults))
            {
                labResults.AbnormalIndicator = ApplicationConstants.UNDETERMINISTIC;
            }

            var newLabResult = await _context.LabResults.AddAsync(labResults, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Inserted a new record into LabResults table for FobtId {FobtId}, with LabResultId {LabResultId}",
                request.FOBTId, newLabResult.Entity.LabResultId);

            return newLabResult.Entity;
        }

        _logger.LogInformation("Record already exists in LabResults table without an exception for FobtId {FobtId}, not inserting another record",
            request.FOBTId);

        return null;
    }

    private bool IsLabResultValid(LabResults labResult)
    {
        var answers = new BillableRuleAnswers
        {
            LabResults = labResult
        };
        return _billableRules.IsLabResultValid(answers).IsMet;
    }
}