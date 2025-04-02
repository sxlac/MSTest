using System;

namespace Signify.eGFR.Core.Models;

public class PayableRuleAnswers(long evaluationId, Guid eventId) : BusinessRuleAnswers(evaluationId, eventId);