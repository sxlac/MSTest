using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants;

[ExcludeFromCodeCoverage]
public static class EvaluationStatus
{
    public const int EvaluationFinalized = 3;
    public const int EvaluationCanceled = 4;
}