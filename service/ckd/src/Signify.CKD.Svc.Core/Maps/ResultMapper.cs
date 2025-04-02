using AutoMapper;
using Result = Signify.CKD.Svc.Core.Messages.Result;
using Signify.CKD.Svc.Core.Messages;
using System;
using Signify.CKD.Svc.Core.BusinessRules;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Models;

namespace Signify.CKD.Svc.Core.Maps;

public class ResultsMapper
    : ITypeConverter<Data.Entities.CKD, Result>, ITypeConverter<Data.Entities.LookupCKDAnswer, Result>
{
    private readonly IBillableRules _billableRules;

    public ResultsMapper(IBillableRules billableRules)
    {
        _billableRules = billableRules;
    }
    
    public Result Convert(Data.Entities.CKD source, Result destination, ResolutionContext context)
    {
        destination ??= new Result();
        destination.EvaluationId = source.EvaluationId!.Value;
        destination.ReceivedDate = source.ReceivedDateTime;
        destination.ExpiryDate = source.ExpirationDate;
        destination.PerformedDate = source.CreatedDateTime;
        destination.ProductCode = Application.ProductCode;
        destination.IsBillable = IsBillable(source.CKDAnswer);
        if (string.IsNullOrWhiteSpace(source.CKDAnswer))
        {
            //Exception Invalid strip result
            destination.Results.Add(new ResultType(ResultTypeName.Exception.ToString(), "Invalid Strip Result", String.Empty, String.Empty));
        }
        if (source.ExpirationDate == null)
        {
            //Exception Invalid Expiry Date
            destination.Results.Add(new ResultType(ResultTypeName.Exception.ToString(), "Invalid Expiry Date", String.Empty, String.Empty));
        }
        return destination;
    }

    public Result Convert(Data.Entities.LookupCKDAnswer source, Result destination, ResolutionContext context)
    {
        if (!string.IsNullOrWhiteSpace(source?.CKDAnswerValue))
        {
            destination.Determination = source.NormalityIndicator;
            destination.Results.Add(new ResultType(ResultTypeName.Albumin.ToString(), source.Albumin.ToString(), "mg/L", String.Empty));
            destination.Results.Add(new ResultType(ResultTypeName.Creatinine.ToString(), source.Creatinine.ToString(), "g/L", String.Empty));
            destination.Results.Add(new ResultType(ResultTypeName.uAcr.ToString(), source.Acr, "mg/g", source.Severity));
        }
        else
        {
            destination.Determination = Constants.Application.NormalityIndicator.Undetermined;
        }
        return destination;
    }
    
    /// <summary>
    /// Determine if the exam is billable by rules defined in IBillableRules 
    /// </summary>
    /// <param name="CKDAnswer"></param>
    /// <returns></returns>
    private bool IsBillable(string CKDAnswer)
    {
        var answers = new BillableRuleAnswers
            { CkdAnswer = CKDAnswer};
        return _billableRules.IsBillable(answers).IsMet;
    }
}