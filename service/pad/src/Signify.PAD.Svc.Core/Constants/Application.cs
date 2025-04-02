using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants;

[ExcludeFromCodeCoverage]
public static class Application
{
    public const string ApplicationId = "Signify.PAD.Svc";

    /// <summary>
    /// Document type as defined in the Evaluation Core API/service
    /// </summary>
    public const string WaveformDocumentType = "PadWaveform";

    public const string ProductCode = "PAD";

    public const string PADPerformed = "PADPerformed";

    public const string PADNotPerformed = "PADNotPerformed";

    public const string AzureServiceBus = "AzureServiceBus";

    public static class Side
    {
        public const string Left = "L";
        public const string Right = "R";
    }

    public static class NormalityIndicator
    {
        public const string Normal = "N";
        public const string Abnormal = "A";
        public const string Undetermined = "U";
    }

    public static class ResultException
    {
        public const string NotSupplied = "Result not supplied";
        public const string Malformed = "Result value malformed";
        public const string OutOfRange = "Result value out of range";
    }

    public static readonly IReadOnlyCollection<NotPerformedReason> NotPerformedRefused = new[]
    {
        new NotPerformedReason{ AnswerId = 30959, Reason = "Member recently completed" },
        new NotPerformedReason{ AnswerId = 30960, Reason = "Scheduled to complete" },
        new NotPerformedReason{ AnswerId = 30961, Reason = "Member apprehension" },
        new NotPerformedReason{ AnswerId = 30962, Reason = "Not interested" },
        new NotPerformedReason{ AnswerId = 30963, Reason = "Other" },
    };

    public static readonly IReadOnlyCollection<NotPerformedReason> NotPerformedUnable = new[]
    {
        new NotPerformedReason{ AnswerId = 31125, Reason = "Not clinically relevant" },
        new NotPerformedReason{ AnswerId = 30966, Reason = "Technical issue" },
        new NotPerformedReason{ AnswerId = 30967, Reason = "Environmental issue" },
        new NotPerformedReason{ AnswerId = 30968, Reason = "No supplies or equipment" },
        new NotPerformedReason{ AnswerId = 30969, Reason = "Insufficient training" },
        new NotPerformedReason{ AnswerId = 50917, Reason = "Member physically unable" },
    };

    public static class ClinicalSupportType
    {
        public const string PainInLegs = "PainInLegs";
        public const string FootPainDisappearsWalkingOrDangling = "FootPainDisappearsWalkingOrDangling";
        public const string FootPainDisappearsWithMeds = "FootPainDisappearsWithMeds";
        public const string PedalPulseCode = "PedalPulseCode";
        public const string HasSymptomsForAoeWithRestingLegPain = "HasSymptomsForAoeWithRestingLegPain";
        public const string HasClinicalSupportForAoeWithRestingLegPain = "HasClinicalSupportForAoeWithRestingLegPain";
        public const string AoeWithRestingLegPainConfirmed = "AoeWithRestingLegPainConfirmed";
        public const string ReasonAoeWithRestingLegPainNotConfirmed = "ReasonAoeWithRestingLegPainNotConfirmed";
    }
}

[ExcludeFromCodeCoverage]
public class NotPerformedReason
{
    public int AnswerId { get; set; }
    public string Reason { get; set; }
}