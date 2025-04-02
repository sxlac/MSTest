namespace Signify.PAD.Svc.Core.Validators
{
    /// <summary>
    /// Interface for validating raw answer values of type <see cref="string"/> to their actual type <see cref="T"/>
    /// </summary>
    /// <typeparam name="T">Actual data type of the result</typeparam>
    public interface IResultValidator<T>
    {
        /// <summary>
        /// Verifies whether or not the raw answer value is valid for the result's data type
        /// </summary>
        /// <param name="rawValue">Raw value to be split</param>
        /// <param name="validatedResult">Validated result parsed as type <see cref="T"/></param>
        /// <returns>Whether or not the value is valid</returns>
        bool IsValid(string rawValue, out T validatedResult);
    }
}

