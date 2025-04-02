using AutoMapper;
using Signify.DEE.Messages;
using Signify.DEE.Svc.Core.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signify.DEE.Svc.Core.Maps;

public class SideResultInfoMapper(IOverallNormalityMapper overallNormalityMapper)
    : ITypeConverter<Exam, ICollection<SideResultInfo>>
{
    private enum Side
    {
        Left,
        Right
    }

    public ICollection<SideResultInfo> Convert(Exam source, ICollection<SideResultInfo> destination, ResolutionContext context)
    {
        destination ??= new List<SideResultInfo>();

        destination.Add(GetSideInfo(source, Side.Left));
        destination.Add(GetSideInfo(source, Side.Right));

        return destination;
    }

    private SideResultInfo GetSideInfo(Exam exam, Side side)
    {
        var result = new SideResultInfo();

        var examResult = exam.ExamResults.First();

        int lateralityCodeId;

        switch (side)
        {
            case Side.Left:
                result.Side = SideResultInfo.LeftSide;
                result.Pathology = examResult.LeftEyeHasPathology;
                lateralityCodeId = LateralityCode.Left.LateralityCodeId;
                break;
            case Side.Right:
                result.Side = SideResultInfo.RightSide;
                result.Pathology = examResult.RightEyeHasPathology;
                lateralityCodeId = LateralityCode.Right.LateralityCodeId;
                break;
            default:
                throw new NotImplementedException();
        }

        result.Findings = GetFindings(examResult.ExamFindings, lateralityCodeId);

        result.AbnormalIndicator = overallNormalityMapper.GetOverallNormality(result.Findings.Select(finding => finding.AbnormalIndicator));

        SetGradabilityAndNotGradableReasons(result, lateralityCodeId, exam);

        return result;
    }

    private static void SetGradabilityAndNotGradableReasons(SideResultInfo sideResult, int lateralityCodeId, Exam exam)
    {
        sideResult.NotGradableReasons = new List<string>();

        var examLateralityGrade = exam.ExamLateralityGrades.FirstOrDefault(e => e.LateralityCodeId == lateralityCodeId);

        if (examLateralityGrade is not null)
        {
            sideResult.Gradable = examLateralityGrade.Gradable;

            if (!sideResult.Gradable)
            {
                sideResult.NotGradableReasons = examLateralityGrade
                    .NonGradableReasons.Where(e => !string.IsNullOrWhiteSpace(e.Reason))
                    .Select(e => e.Reason)
                    .Distinct()
                    .ToList();
            }
        }

        if (!exam.ExamImages.Any(i => i.LateralityCodeId == lateralityCodeId))
        {
            sideResult.NotGradableReasons.Add("No images are available");
        }
    }

    private static IList<SideFinding> GetFindings(IEnumerable<ExamFinding> examFindings, int lateralityCodeId)
    {
        var findings = new List<SideFinding>();

        foreach (var finding in examFindings)
        {
            if (finding.LateralityCodeId != lateralityCodeId)
                continue;

            // Example Finding value: "Diabetic Retinopathy - Mild"
            var parts = finding.Finding.Split('-'); // Not splitting by " - ", just in case they for some reason remove one of the whitespaces

            findings.Add(new SideFinding
            {
                AbnormalIndicator = finding.NormalityIndicator,
                Finding = parts[0].Trim(),
                Result = parts[1].TrimStart(' ')
            });
        }

        return findings;
    }
}