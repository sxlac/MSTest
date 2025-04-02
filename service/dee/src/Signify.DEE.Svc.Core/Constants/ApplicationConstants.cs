
namespace Signify.DEE.Svc.Core.Constants;

public static class ApplicationConstants
{
    public const string ProductCode = "DEE";
    public const string ServiceName = "Signify.DEE.Svc";
    public const string NotGradableReasonsDelimiter = "; ";
    public const string OriginalImageType = "Original";
    public const string MemberApi = "MemberApi";
    public const string EvaluationId = "EvaluationId";
    public const string Enucleation = "Enucleation";

    public static class FindingNames
    {
        public const string DiabeticRetinopathy = "Diabetic Retinopathy";
        public const string MacularEdema = "Macular Edema";
        public const string WetAMD = "Wet AMD";
        public const string DryAMD = "Dry AMD";
    }

    public static class NormalityIndicator
    {
        public const string Normal = "N";
        public const string Abnormal = "A";
        public const string Undetermined = "U";
    }

    public static class PathologyGradingResults
    {
        public const string Positive = "Positive";
        public const string Mild = "Mild";
        public const string Severe = "Severe";
        public const string Moderate = "Moderate";
        public const string Proliferative = "Proliferative";
    }

    public static class WetAMDFindingResult
    {
        public const string Positive = "Positive";
        public const string NoObservable = "No Observable";
        public const string Indeterminable = "Indeterminable";
    }

    public static class DryAMDFindingResult
    {
        public const string NoObservable = "No Observable";
        public const string Indeterminable = "Indeterminable";
        public const string EarlyStage = "Early Stage";
        public const string IntermediateStage = "Intermediate Stage";
        public const string AdvAtrophicWithSubofealInvolvement = "Adv. Atrophic w/ Subfoveal Involvement";
        public const string AdvAtrophicWithoutSubofealInvolvement = "Adv. Atrophic w/o Subfoveal Involvement";
    }

    public static class Laterality
    {
        public const string RightEyeCode = "OD";
        public const string LeftEyeCode = "OS";
        public const string Unknown = "Unknown";
    }
}