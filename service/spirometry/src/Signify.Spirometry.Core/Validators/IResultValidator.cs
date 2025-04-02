namespace Signify.Spirometry.Core.Validators
{
    /// <summary>
    /// Interface for validating raw answer values of type <see cref="string"/> to their actual type <see cref="T"/>
    /// </summary>
    /// <typeparam name="T">Actual data type of the result</typeparam>
    public interface IResultValidator<T>
    {
        /// <summary>
        /// Verifies whether or not the raw answer value is valid for the result's data type and, if applicable,
        /// range of possible values
        /// </summary>
        /// <param name="rawValue">Raw answer value for the result</param>
        /// <param name="validatedResult">Validated result parsed as type <see cref="T"/> and optionally, validated
        /// against the range of possible values, if applicable</param>
        /// <returns>Whether or not the value is valid</returns>
        bool IsValid(string rawValue, out T validatedResult);

        /// <summary>
        /// Verifies whether or not the given <paramref name="result"/> is valid
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        bool IsValid(T result);
    }
}
