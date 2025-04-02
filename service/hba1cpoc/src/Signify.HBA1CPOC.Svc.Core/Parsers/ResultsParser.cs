using System;
using Signify.HBA1CPOC.Svc.Core.Models;

namespace Signify.HBA1CPOC.Svc.Core.Parsers
{
    public class ResultsParser : IResultsParser
    {
        private const string OutOfRangeLow = "Result out of range (low)";
        private const string OutOfRangeHigh = "Result out of range (high)";
        private const string Malformed = "Result malformed";

        private const string LessThanLowLimit = "<4";
        private const string GreaterThanHighLimit = ">13";

        // Outside this range is Abnormal
        private const decimal NormalLowLimitInclusive = 4m;
        private const decimal NormalHighLimitExclusive = 7m;

        // Must be between these values or is considered out of range
        private static readonly decimal LowLimit = decimal.Parse(LessThanLowLimit[1..]);
        private static readonly decimal HighLimit = decimal.Parse(GreaterThanHighLimit[1..]);

        public ResultsModel Parse(string rawValue)
        {
            var result = new ResultsModel
            {
                RawValue = rawValue,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Undetermined,
                Exception = Malformed
            };
            if (rawValue != null)
            {
                rawValue = rawValue.Replace(" ", String.Empty);
                rawValue = rawValue.TrimEnd('%'); // I've seen in prod recently in the format "6.5%"...
                // If the raw value ends with ".0" as in "<4.0" or ">13.0" we will trim that off here
                rawValue = rawValue.EndsWith(".0") ? rawValue.TrimEnd('0', '.') : rawValue;
            }

            switch (rawValue)
            {
                case null:
                case "":
                    return result;
                case LessThanLowLimit:
                    return SetFromRangeLimit(result, true);
                case GreaterThanHighLimit:
                    return SetFromRangeLimit(result, false);
                default:
                    if (!decimal.TryParse(rawValue, out var val))
                        return result;

                    result.ParsedValue = val;

                    // See https://wiki.signifyhealth.com/display/AncillarySvcs/HbA1c+Business+Rules

                    result.Normality = val is >= NormalLowLimitInclusive and < NormalHighLimitExclusive
                        ? Normality.Normal : Normality.Abnormal;

                    result.Exception = GetException(val);

                    return result;
            }
        }

        private static ResultsModel SetFromRangeLimit(ResultsModel model, bool isLowLimit)
        {
            model.ParsedValue = isLowLimit ? LowLimit : HighLimit;
            model.ValueRange = isLowLimit ? ResultValueRange.LessThan : ResultValueRange.GreaterThan;
            model.Normality = Normality.Abnormal;
            model.Exception = null;
            return model;
        }

        private static string GetException(decimal value)
        {
            if (value < LowLimit)
                return OutOfRangeLow;
            if (value > HighLimit)
                return OutOfRangeHigh;
            return null;
        }
    }
}
