using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Models;

[ExcludeFromCodeCoverage]
public class BillableRuleAnswers(long evaluationId, Guid eventId) : BusinessRuleAnswers(evaluationId, eventId);