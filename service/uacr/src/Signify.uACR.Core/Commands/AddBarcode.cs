using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Models;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Signify.uACR.Core.Exceptions;

namespace Signify.uACR.Core.Commands;

/// <summary>
/// Command to Create or Update a <see cref="Data.Entities.Exam"/> entity in db
/// </summary>
public class AddBarcode(Exam exam, RawExamResult results) : IRequest<BarcodeExam>
{
    public Exam Exam { get; } = exam;

    public RawExamResult Results { get; } = results;
}

public class AddBarcodeHandler(
    ILogger<AddBarcodeHandler> logger,
    DataContext dataContext)
    : IRequestHandler<AddBarcode, BarcodeExam>
{
    public async Task<BarcodeExam> Handle(AddBarcode request, CancellationToken cancellationToken)
    {
        var barcode = new BarcodeExam
        {
            Exam = request.Exam,
            Barcode = request.Results.Barcode,
            CreatedDateTime = request.Exam.CreatedDateTime
        };

        var entity = (await dataContext.BarcodeExams.AddAsync(barcode, cancellationToken)
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

        logger.LogInformation("Successfully inserted a new barcode record for ExamId={ExamId}. New BarcodeExamId={BarcodeExamId}",
            entity.Exam, entity.BarcodeExamId);

        return entity;
    }
}