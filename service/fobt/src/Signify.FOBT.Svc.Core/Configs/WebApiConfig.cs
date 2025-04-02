using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Configs
{
    /// <summary>
    /// Configurations for APIs
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class WebApiConfig
    {
        /// <summary>
        /// Uri to the Signify Evaluation core API
        /// </summary>
        public Uri EvaluationApiUrl { get; set; }

        /// <summary>
        /// Uri to the Signify Inventory API
        /// </summary>
        public Uri InventoryApiUrl { get; set; }

        /// <summary>
        /// Uri to the Signify Member core API
        /// </summary>
        public Uri MemberApiUrl { get; set; }

        /// <summary>
        /// Uri to the Signify Provider core API
        /// </summary>
        public Uri ProviderApiUrl { get; set; }

        /// <summary>
        /// Uri to the Signify Labs API
        /// </summary>
        public Uri LabsApiUrl { get; set; }

        /// <summary>
        /// Uri to the Signify RCM API
        /// </summary>
        public Uri RcmApiUrl { get; set; }
        
        /// <summary>
        /// Uri to the Signify Provider pay API
        /// </summary>
        public Uri ProviderPayApiUrl { get; set; }
    }
}
