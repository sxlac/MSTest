using Signify.Spirometry.Core.Configs.Exam;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.DI.Configs
{
    [ExcludeFromCodeCoverage]
    public class ExamResultsConfig
    {
        /// <summary>
        /// Configuration section key for this config
        /// </summary>
        public const string Key = "ExamResults";

        public Fev1Config Fev1 { get; set; }

        public FvcConfig Fvc { get; set; }

        public class Fev1Config : IFev1Config
        {
            public int MinValueInclusive { get; set; }

            public int MaxValueInclusive { get; set; }
        }

        public class FvcConfig : IFvcConfig
        {
            public int MinValueInclusive { get; set; }

            public int MaxValueInclusive { get; set; }
        }
    }
}
