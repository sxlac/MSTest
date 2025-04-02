using System;

namespace Signify.PAD.Svc.Core.Validators;

public class StringValidator : IStringValidator
{
    /// <summary>
    /// Does the string split correctly
    /// </summary>
    /// <param name="rawValue"></param>
    /// <param name="validatedResult"></param>
    /// <returns></returns>
    public bool IsValid(string rawValue, out string validatedResult)
    {
        validatedResult = null;
        if (IsValid(rawValue))
        {
            validatedResult = rawValue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Does the string have a value
    /// </summary>
    /// <param name="rawValue"></param>
    /// <returns></returns>
    public bool IsValid(string rawValue)
        => !String.IsNullOrEmpty(rawValue);


    /// <summary>
    /// Does the string to be split contain the value to split by
    /// </summary>
    /// <param name="rawValue"></param>
    /// <param name="delimiter"></param>
    /// <returns></returns>
    public bool IsValid(string rawValue, string delimiter)
    {
        var strings = rawValue.Split(delimiter);
        return strings.Length > 0;
    }
}