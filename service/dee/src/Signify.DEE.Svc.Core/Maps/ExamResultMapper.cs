using System;
using AutoMapper;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using static Signify.DEE.Svc.Core.Constants.ApplicationConstants;

namespace Signify.DEE.Svc.Core.Maps;

public class ExamResultMapper : ITypeConverter<ExamResultModel, ExamResult>
{
    enum FindingType
    {
        DiabeticRetinopathy,
        MacularEdema,
        WetAmd,
        DryAmd,
        Other
    }

    public ExamResult Convert(ExamResultModel source, ExamResult destination, ResolutionContext context)
    {
        // Note: Unmapped fields in destination:
        // ExamResultId
        // NormalityIndicator
        // Exam

        destination ??= new ExamResult();

        destination.CarePlan = source.CarePlan;
        destination.DateSigned = source.DateSigned;
        destination.ExamId = source.ExamId;
        destination.GradableImage = source.GradableImage;
        destination.GraderFirstName = source.Grader.FirstName;
        destination.GraderLastName = source.Grader.LastName;
        destination.GraderNpi = source.Grader.NPI;
        destination.GraderTaxonomy = source.Grader.Taxonomy;
        destination.LeftEyeHasPathology = source.LeftEyeHasPathology;
        destination.RightEyeHasPathology = source.RightEyeHasPathology;

        foreach (var diagnosis in source.Diagnoses)
        {
            var value = new ExamDiagnosis
            {
                Diagnosis = diagnosis,
                ExamResult = destination
            };
            destination.ExamDiagnoses.Add(value);
        }

        foreach (var finding in source.RightEyeFindings)
        {
            var value = new ExamFinding
            {
                LateralityCodeId = LateralityCode.Right.LateralityCodeId,
                Finding = finding,
                NormalityIndicator = GetNormalityIndicator(finding),
                ExamResult = destination
            };
            destination.ExamFindings.Add(value);
        }
        foreach (var finding in source.LeftEyeFindings)
        {
            var value = new ExamFinding
            {
                LateralityCodeId = LateralityCode.Left.LateralityCodeId,
                Finding = finding,
                NormalityIndicator = GetNormalityIndicator(finding),
                ExamResult = destination
            };
            destination.ExamFindings.Add(value);
        }

        return destination;
    }

    private static bool Contains(string finding, string search)
    {
        return finding.Contains(search, StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool StartsWith(string finding, string search)
    {
        return finding.StartsWith(search, StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool TryDetermineFindingType(string finding, out FindingType findingType)
    {
        if (Contains(finding, "Diabetic Retinopathy"))
        {
            findingType = FindingType.DiabeticRetinopathy;
            return true;
        }

        if (Contains(finding, "Macular Edema"))
        {
            findingType = FindingType.MacularEdema;
            return true;
        }

        if (StartsWith(finding, "Wet AMD"))
        {
            findingType = FindingType.WetAmd;
            return true;
        }

        if (StartsWith(finding, "Dry AMD"))
        {
            findingType = FindingType.DryAmd;
            return true;
        }

        if (Contains(finding, "Other"))
        {
            findingType = FindingType.Other;
            return true;
        }

        findingType = default;
        return false;
    }

    private static string MacularEdemaIndicator(string finding)
    {
        if (Contains(finding, "None"))
        {
            return NormalityIndicator.Normal;
        }
        return Contains(finding, "Indeterminable")
            ? NormalityIndicator.Undetermined : NormalityIndicator.Abnormal;
    }
    private static string WetAmdIndicator(string finding)
    {
        if (Contains(finding, "Indeterminable"))
        { return NormalityIndicator.Undetermined; }
        else if (Contains(finding, "No Observable"))
        { return NormalityIndicator.Normal; }
        else if (Contains(finding, "Positive"))
        { return NormalityIndicator.Abnormal; }

        return NormalityIndicator.Undetermined;
    }
    private static string DryAmdIndicator(string finding)
    {
        if (Contains(finding, "Indeterminable"))
        { return NormalityIndicator.Undetermined; }
        else if (Contains(finding, "No Observable"))
        { return NormalityIndicator.Normal; }
        else if (Contains(finding, "Early Stage") ||
                 Contains(finding, "Intermediate Stage") ||
                 Contains(finding, "Adv. Atrophic w/ Subfoveal Involvement") ||
                 Contains(finding, "Adv. Atrophic w/o Subfoveal Involvement"))
        { return NormalityIndicator.Abnormal; }

        return NormalityIndicator.Undetermined;
    }
    public static string GetNormalityIndicator(string finding)
    {
        if (TryDetermineFindingType(finding, out var findingType))
        {
            switch (findingType)
            {
                case FindingType.DiabeticRetinopathy:
                case FindingType.MacularEdema:
                    return MacularEdemaIndicator(finding);
                case FindingType.WetAmd:
                    return WetAmdIndicator(finding);
                case FindingType.DryAmd:
                    return DryAmdIndicator(finding);
                case FindingType.Other:
                    return NormalityIndicator.Abnormal;
            }
        }

        // If the finding type is not one of the types supported above, we cannot safely determine normality
        return NormalityIndicator.Undetermined;
    }

}