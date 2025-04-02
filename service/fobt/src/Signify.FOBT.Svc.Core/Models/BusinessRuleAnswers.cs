using System.Diagnostics.CodeAnalysis;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public abstract class BusinessRuleAnswers
{
   public Data.Entities.FOBT Exam { get; set; }

   public LabResults LabResults { get; set; }
   
   public bool? IsValidLabResultsReceived { get; set; }
}