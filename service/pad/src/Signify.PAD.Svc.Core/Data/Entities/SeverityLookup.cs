using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class SeverityLookup
{
    public int SeverityLookupId { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public string Severity { get; set; }
    public string NormalityIndicator { get; set; }
		
		
    public SeverityLookup()
    {
    }

    public SeverityLookup(int severityLookupId, decimal minScore, decimal maxScore, string severity, string abnormalIndicator)
    {
        SeverityLookupId = severityLookupId;
        MinScore = minScore;
        MaxScore = maxScore;
        Severity = severity;
        NormalityIndicator = abnormalIndicator;
    }

    public override string ToString()
    {
        return $"{nameof(SeverityLookupId)}: {SeverityLookupId}, {nameof(MinScore)}: {MinScore}, {nameof(MaxScore)}: {MaxScore}, {nameof(Severity)}: {Severity}, {nameof(NormalityIndicator)}: {NormalityIndicator}";
    }

    public bool Equals(SeverityLookup other)
    {
        return SeverityLookupId == other.SeverityLookupId && MinScore == other.MinScore && MaxScore == other.MaxScore && Severity == other.Severity && NormalityIndicator == other.NormalityIndicator;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SeverityLookup) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SeverityLookupId;
            hashCode = (hashCode * 397) ^ MinScore.GetHashCode();
            hashCode = (hashCode * 397) ^ MaxScore.GetHashCode();
            hashCode = (hashCode * 397) ^ (Severity != null ? Severity.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (NormalityIndicator != null ? NormalityIndicator.GetHashCode() : 0);
            return hashCode;
        }
    }
}