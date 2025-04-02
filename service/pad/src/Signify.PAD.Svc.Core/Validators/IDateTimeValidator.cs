using System;

namespace Signify.PAD.Svc.Core.Validators
{
    public interface IDateTimeValidator : IResultValidator<DateTime?>
    {
        bool IsValid(string rawValue);
        bool IsValid(string rawValue, string format);
    }
}
