namespace Signify.Spirometry.Svc.System.Tests.Core.Constants;

public static class Answers
{
    public const int PerformedYesAnswerId = 50919;
    public const int PerformedNoAnswerId = 50920;
    public const int DosAnswerId = 22034;
    public const int SessionGradeIdAnswerId = 51947;
    public const int FVCAnswerId = 50999;
    public const int FEV1AnswerId = 51000;
    public const int FEV1FVCAnswerId = 51002;
    
    public const string ProviderUnableAnswer = "Unable to perform";
    public const int UnablePerformAnswerId = 50922;
    public const int TechnicalIssueAnswerId = 50928;
    public const int EnvironmentalIssueAnswerId = 50929;
    public const int NoSuppliesOrEquipmentAnswerId = 50930;
    public const int InsufficientTrainingAnswerId = 50931;
    public const int MemberPhysicallyUnableAnswerId = 50932;
    public const int MemberOutsideDemographicRangesAnswerId = 51960;
    public const int NotesAnswerId = 50927;
    
    
    public const string MemberRefusedAnswer = "Member refused";
    public const int MemberRefusedAnswerId = 50921;
    public const int MemberRecentlyCompletedAnswerId = 50923;
    public const int MemberScheduledToCompleteAnswerId = 50924;
    public const int MemberApprehensionAnswerId = 50925;
    public const int MemberNotInterestedAnswerId = 50926;
    
    public const int NeverCoughAnswerId = 51405;
    public const int RarelyCoughAnswerId = 51406;
    public const int SometimesCoughAnswerId = 51407;
    public const int OftenCoughAnswerId = 51408;
    public const int VeryOftenCoughAnswerId = 51409;
    
    public const int NeverWheezyChestAnswerId = 51410;
    public const int RarelyWheezyChestAnswerId = 51411;
    public const int SometimesWheezyChestAnswerId = 51412;
    public const int OftenWheezyChestAnswerId = 51413;
    public const int VeryOftenWheezyChestAnswerId = 51414;
    
    public const int NeverBreathShortnessAnswerId = 51415;
    public const int RarelyBreathShortnessAnswerId = 51416;
    public const int SometimesBreathShortnessAnswerId = 51417;
    public const int OftenBreathShortnessAnswerId = 51418;
    public const int VeryOftenBreathShortnessAnswerId = 51419;
    
    public const int LungFunctionScoreAnswerId = 51420;
    public const int DxCOPDAnswerId = 50993;
    public const int HistoryCOPDAnswerId = 52027;
    public static bool IsBillable { get; set; } 
    public static bool NeedsFlag { get; set; }
    
    public static string Normality { get; set; }
    public static string obstructionPerOverread { get; set; }
    
    public const int HasSmokedTrueAnswerId = 20486;
    public const int HasSmokedNoAnswerId = 20485;
    
    public const int SmokingYearsAnswerId = 21211;
    
    public const int ProduceSputumYesAnswerId = 20724;
    public const int ProduceSputumNoAnswerId = 20723;
    
    public const int HadWheezingYesAnswerId = 20484;
    public const int HadWheezingNoAnswerId = 20483;
    public const int HadWheezingUnknownAnswerId = 33594;
    
    public const int ShortBreathatRestYesAnswerId = 20498;
    public const int ShortBreathatRestNoAnswerId = 20497;
    public const int ShortBreathatRestUnknownAnswerId = 33596;
    
    public const int ShortBreathExertionYesAnswerId = 20500;
    public const int ShortBreathExertionNoAnswerId = 20499;
    public const int ShortBreathExertionUnknownAnswerId = 33597;
    
    
    
    
}