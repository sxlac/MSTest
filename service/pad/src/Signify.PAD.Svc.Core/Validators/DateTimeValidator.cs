using System;
using System.Globalization;

namespace Signify.PAD.Svc.Core.Validators;

public class DateTimeValidator : IDateTimeValidator
{
    public bool IsValid(string rawValue, out DateTime? validatedResult)
    {
        if (!DateTime.TryParse(rawValue, out var parsed))
        {
            validatedResult = null;
            return false;
        }

        validatedResult = parsed;
        return true;
    }

    public bool IsValid(string rawValue)
        => DateTime.TryParse(rawValue, out var dt);

    public bool IsValid(string rawValue, string format)
        =>DateTime.TryParseExact(rawValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt);
}