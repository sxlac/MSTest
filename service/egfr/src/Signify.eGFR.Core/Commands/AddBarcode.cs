using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Models;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Exceptions;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Command to Create or Update a <see cref="Data.Entities.Exam"/> entity in db
/// </summary>
public class AddBarcode(Exam exam, RawExamResult results) : IRequest<BarcodeHistory>
{
    public Exam Exam { get; } = exam;

    public RawExamResult Results { get; } = results;
}

public class AddBarcodeHandler(
    ILogger<AddBarcodeHandler> logger,
    DataContext dataContext)
    : IRequestHandler<AddBarcode, BarcodeHistory>
{
    private readonly ILogger _logger = logger;

    public async Task<BarcodeHistory> Handle(AddBarcode request, CancellationToken cancellationToken)
    {
        var barcode = new BarcodeHistory
        {
            Exam = request.Exam,
            Barcode = request.Results.Barcode,
            CreatedDateTime = request.Exam.CreatedDateTime
        };
        var entity = (await dataContext.BarcodeHistories.AddAsync(barcode, cancellationToken)
            .ConfigureAwait(false)).Entity;

        try
        {
            await dataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is {Message: not null} &&
                ex.InnerException.Message.Contains("duplicate key value violates unique constraint"))
                throw new DuplicateBarcodeException(barcode.Exam.EvaluationId, barcode.Barcode, ex);
            throw;
        }

        _logger.LogInformation("Successfully inserted a new barcode record for ExamId={ExamId}. New BarcodeHistoryId={BarcodeHistoryId}",
            entity.Exam, entity.BarcodeHistoryId);

        return entity;
    }
}