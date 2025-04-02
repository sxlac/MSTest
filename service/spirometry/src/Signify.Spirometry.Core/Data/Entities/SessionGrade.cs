using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Signify.Spirometry.Core.Data.Entities
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - Virtual properties are used by EF
    public class SessionGrade : IEntityEnum<SessionGrade>
    {
        public static readonly SessionGrade A = new SessionGrade(1, "A", true);
        public static readonly SessionGrade B = new SessionGrade(2, "B", true);
        public static readonly SessionGrade C = new SessionGrade(3, "C", true);
        public static readonly SessionGrade D = new SessionGrade(4, "D", false);
        public static readonly SessionGrade E = new SessionGrade(5, "E", false);
        public static readonly SessionGrade F = new SessionGrade(6, "F", false);

        /// <inheritdoc />
        public IEnumerable<SessionGrade> GetAllEnumerations()
            => new[]
            {
                A, B, C, D, E, F
            };

        /// <summary>
        /// Identifier of this session grade
        /// </summary>
        [Key]
        public short SessionGradeId { get; init; }

        public string SessionGradeCode { get; }

        /// <summary>
        /// Whether or not exams with this grade are gradable
        /// </summary>
        public bool IsGradable { get; init; }

        private SessionGrade(short sessionGradeId, string sessionGradeCode, bool isGradable)
        {
            SessionGradeId = sessionGradeId;
            SessionGradeCode = sessionGradeCode;
            IsGradable = isGradable;
        }

        public virtual ICollection<SpirometryExamResult> SpirometryExamResults { get; set; } = new HashSet<SpirometryExamResult>();
    }
}
