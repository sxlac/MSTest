using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Signify.Spirometry.Core.Data.Entities
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - Virtual properties are used by EF
    public class TrileanType : IEntityEnum<TrileanType>
    {
        public static readonly TrileanType Unknown = new TrileanType(1, nameof(Unknown));
        public static readonly TrileanType Yes = new TrileanType(2, nameof(Yes));
        public static readonly TrileanType No = new TrileanType(3, nameof(No));

        /// <inheritdoc />
        public IEnumerable<TrileanType> GetAllEnumerations()
            => new[]
            {
                Unknown,
                Yes,
                No
            };

        /// <summary>
        /// PK Identifier
        /// </summary>
        [Key]
        public short TrileanTypeId { get; init; }
        /// <summary>
        /// String representation of this trilean type
        /// </summary>
        public string TrileanValue { get; }

        private TrileanType(short trileanTypeId, string trileanValue)
        {
            TrileanTypeId = trileanTypeId;
            TrileanValue = trileanValue;
        }

        public virtual ICollection<SpirometryExamResult> SpirometryExamResultGetsShortnessOfBreathAtRestTrileanTypes { get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultGetsShortnessOfBreathWithMildExertionTrileanTypes { get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultHadWheezingPast12moTrileanTypes { get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultHasEnvOrExpRiskTrileanTypes { get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultHasHighComorbidityTrileanTypes { get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultHasHighSymptomTrileanTypes { get; set; } = new HashSet<SpirometryExamResult>();
    }
}
