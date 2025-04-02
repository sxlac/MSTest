namespace Signify.PAD.Svc.System.Tests.Core.Constants;

public static class Answers
{
    public const int PerformedYesAnswerId = 29560;
    public const int PerformedNoAnswerId = 29561;
    public const int LeftResultAnswerId = 29564;
    public const int RightResultAnswerId = 30973;
    public const int DosAnswerId = 22034;
    public const int ReasonAoEWithRestingLegPainNotConfirmed = 52830;
    public const int MemberRefusedAnswerId = 30957;
    public const int UnableToPerformAnswerId = 30958;
    public const int ClinicallyIrrelevantAnswerId = 31125;
    public const int ClinicallyIrrelevantReasonAnswerId = 31126;
    public const int MemberRecentlyCompletedAnswerId = 30959;
    public const int ScheduledToCompleteAnswerId = 30960;
    public const int MemberApprehensionAnswerId = 30961;
    public const int NotInterestedAnswerId = 30962;
    public const int OtherAnswerId = 30963;
    public const int TechnicalIssueAnswerId = 30966;
    public const int EnvironmentalIssueAnswerId = 30967;
    public const int NoSuppliesOrEquipmentAnswerId = 30968;
    public const int InsufficientTrainingAnswerId = 30969;
    public const int MemberPhysicallyUnableAnswerId = 50917;
    public const int MemberRefusalNotesAnswerId = 30964;
    public const int UnableToPerformNotesAnswerId = 30971;
    
    public const int footpainrestingRight = 52178;
    public const int footpainrestingLeft = 52179;
    public const int footpainrestingBoth = 52180;
    public const int footpainrestingNo = 52181;
    
    public const int footpaindisappearbywalkingTrue = 52182;
    public const int footpaindisappearbywalkingFalse = 52183;
    public const int footpaindisappearbyotcTrue = 52184;
    public const int footpaindisappearbyotcFalse = 52185;
    
    public const int pedalpulseNormal = 52186;
    public const int pedalpulseAbNormalLeft = 52187;
    public const int pedalpulseAbNormalRight = 52188;
    public const int pedalpulseAbNormalBoth = 52189;
    public const int pedalpulseNotPerformed = 52190;
    
    public const int AoeWithRestingLegPainConfirmedTrue = 52191;
    public const int AoeWithRestingLegPainConfirmedFalse = 52192;

    public static bool hasClinicalSupport { get; set; } = true;
    public static bool hasSymptomsForAoe { get; set; } = true;
    public static int lateralityCodeId { get; set; } = 3;
    public static int pedalpulseCodeId { get; set; } = 1;



}