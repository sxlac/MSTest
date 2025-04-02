using System.Collections.Generic;

namespace Signify.Spirometry.Core.Data.Entities
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - Virtual properties are used by EF
    public class NormalityIndicator : IEntityEnum<NormalityIndicator>
    {
        public static readonly NormalityIndicator Undetermined = new NormalityIndicator(1, "Undetermined", 'U');
        public static readonly NormalityIndicator Normal = new NormalityIndicator(2, "Normal", 'N');
        public static readonly NormalityIndicator Abnormal = new NormalityIndicator(3, "Abnormal", 'A');

        /// <inheritdoc />
        public IEnumerable<NormalityIndicator> GetAllEnumerations()
            => new[]
            {
                Undetermined,
                Normal,
                Abnormal
            };

        /// <summary>
        /// PK Identifier
        /// </summary>
        public short NormalityIndicatorId { get; init; }
        public string Normality { get; }
        public char Indicator { get; }

        private NormalityIndicator(short normalityIndicatorId, string normality, char indicator)
        {
            NormalityIndicatorId = normalityIndicatorId;
            Normality = normality;
            Indicator = indicator;
        }

        public virtual ICollection<OverreadResult> OverreadResults { get; set; }
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultFev1NormalityIndicators { get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultFvcNormalityIndicators { get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultNormalityIndicators { get; set; } = new HashSet<SpirometryExamResult>();
    }
}
