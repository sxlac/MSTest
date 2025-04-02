using AutoMapper;
using Signify.DEE.Messages;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using Signify.DEE.Svc.Core.Infrastructure;

namespace Signify.DEE.Svc.Core.Maps;

public class ResultMapper(IOverallNormalityMapper overallNormalityMapper, IApplicationTime applicationTime)
    : ITypeConverter<Exam, Result>
{
    public Result Convert(Exam source, Result destination, ResolutionContext context)
    {
        destination ??= new Result();

        destination.ProductCode = Constants.ApplicationConstants.ProductCode;
        destination.EvaluationId = source.EvaluationId!.Value;
        destination.PerformedDate = source.CreatedDateTime;

        var resultReceivedStatus = source.ExamStatuses.FirstOrDefault(each =>
            each.ExamStatusCodeId == ExamStatusCode.ResultDataDownloaded.ExamStatusCodeId);

        destination.ReceivedDate = resultReceivedStatus?.ReceivedDateTime ?? applicationTime.UtcNow();
        
        var examResult = source.ExamResults.First();
        
        destination.DateGraded = examResult.DateSigned;

        destination.CarePlan = examResult.CarePlan;

        destination.Grader = new Grader
        {
            FirstName = examResult.GraderFirstName,
            LastName = examResult.GraderLastName,
            NPI = examResult.GraderNpi,
            Taxonomy = examResult.GraderTaxonomy
        };

        destination.DiagnosisCodes = new HashSet<string>(examResult.ExamDiagnoses
            .Where(each => !string.IsNullOrWhiteSpace(each.Diagnosis))
            .Select(each => each.Diagnosis));

        destination.Results = context.Mapper.Map<ICollection<SideResultInfo>>(source);

        destination.IsBillable = IsBillable(destination, source);
        destination.Determination = examResult.NormalityIndicator;

        return destination;
    }

    private static bool IsLeft(int? lateralityCodeId)
        => lateralityCodeId == LateralityCode.Left.LateralityCodeId;

    private static bool IsRight(int? lateralityCodeId)
        => lateralityCodeId == LateralityCode.Right.LateralityCodeId;

    private static bool IsBillable(Result result, Exam exam)
    {
        // See https://wiki.signifyhealth.com/display/AncillarySvcs/DEE+Business+Rules
        //
        // An exam is billable if *any* of the following are true:
        // 1) There are findings for both left and right eye
        // 2) There are findings for the left eye, and at least one image for the right eye exists
        // 3) There are findings for the right eye, and at least one image for the left eye exists
        // 4) There are findings for the left eye and the right eye has undergone enucleation
        // 5) There are findings for the right eye and the left eye has undergone enucleation

        bool leftHasFindings = false, rightHasFindings = false;
        bool rightEyeEnucleated = false, leftEyeEnucleated = false;
        
        foreach (var side in result.Results)
        {
            switch (side.Side)
            {
                case SideResultInfo.LeftSide:
                    leftHasFindings = side.Findings.Any();
                    if (side.NotGradableReasons.Contains(Constants.ApplicationConstants.Enucleation))
                    {
                        leftEyeEnucleated = true;
                    }
                    break;
                case SideResultInfo.RightSide:
                    rightHasFindings = side.Findings.Any();
                    if (side.NotGradableReasons.Contains(Constants.ApplicationConstants.Enucleation))
                    {
                        rightEyeEnucleated = true;
                    }
                    break;
            }
        }

        if (leftHasFindings && rightHasFindings)
            return true; // 1

        if (leftHasFindings)
            return exam.ExamImages.Any(image => IsRight(image.LateralityCodeId)) || rightEyeEnucleated; // 2, 4

        if (rightHasFindings)
            return exam.ExamImages.Any(image => IsLeft(image.LateralityCodeId)) || leftEyeEnucleated; // 3, 5
        
        return false;
    }
}