using System;

namespace Signify.eGFR.Core.Models;

public class BillableRuleAnswers(long evaluationId, Guid eventId) : BusinessRuleAnswers(evaluationId, eventId);