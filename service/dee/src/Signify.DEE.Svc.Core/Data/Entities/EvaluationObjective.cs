using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class EvaluationObjective
{
    public int EvaluationObjectiveId { get; set; }
    public string Objective { get; set; }

    public static readonly EvaluationObjective Comprehensive = new() { EvaluationObjectiveId = 1, Objective = "Comprehensive" };
    public static readonly EvaluationObjective Focused = new() { EvaluationObjectiveId = 2, Objective = "Focused" };

    public static string GetProductBillingCode(string evaluationObjective)
        => evaluationObjective.Equals(Comprehensive.Objective, System.StringComparison.InvariantCultureIgnoreCase) ? "DEE" : "DEE-DFV";
}