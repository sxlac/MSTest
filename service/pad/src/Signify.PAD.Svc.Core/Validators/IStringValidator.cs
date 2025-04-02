namespace Signify.PAD.Svc.Core.Validators
{
    public interface IStringValidator : IResultValidator<string>
    {
        bool IsValid(string rawValue);
        bool IsValid(string rawValue, string delimiter);
    }
}
