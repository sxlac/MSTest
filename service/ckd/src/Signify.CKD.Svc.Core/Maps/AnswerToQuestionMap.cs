using System.Collections.Generic;

namespace Signify.CKD.Svc.Core.Maps
{
    public static class AnswerToQuestionMap
    {
        public static readonly IReadOnlyDictionary<int, (string ReasonType, string Reason)> AnswerTypeMap = new Dictionary<int, (string, string)>
        {
            {30863, ("Member Refused", "Member recently completed") },
            {30864, ("Member Refused", "Scheduled to complete") },
            {30865, ("Member Refused", "Member apprehension") },
            {30866, ("Member Refused", "Not interested") },
            {30867, ("Member Refused", "Other") },

            {30870, ("Unable to Perform", "Technical issue") },
            {30871, ("Unable to Perform", "Environmental issue") },
            {30872, ("Unable to Perform", "No supplies or equipment") },
            {30873, ("Unable to Perform", "Insufficient training") },
            {50899, ("Unable to Perform", "Member physically unable") },
        };
    }
}
