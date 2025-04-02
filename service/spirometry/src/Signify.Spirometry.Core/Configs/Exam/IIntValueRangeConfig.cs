namespace Signify.Spirometry.Core.Configs.Exam
{
    public interface IIntValueRangeConfig
    {
        /// <summary>
        /// Minimum valid value, inclusive
        /// </summary>
        int MinValueInclusive { get; }

        /// <summary>
        /// Maximum valid value, inclusive
        /// </summary>
        int MaxValueInclusive { get; }
    }
}
