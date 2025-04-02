using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Constants;

[ExcludeFromCodeCoverage]
public static class EvaluationStatus
{
    public const int EvaluationFinalized = 3;
    public const int EvaluationCanceled = 4;    
}