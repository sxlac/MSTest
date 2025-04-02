using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Models;

[ExcludeFromCodeCoverage]
public class PayableRuleAnswers(long evaluationId, Guid eventId) : BusinessRuleAnswers(evaluationId, eventId);