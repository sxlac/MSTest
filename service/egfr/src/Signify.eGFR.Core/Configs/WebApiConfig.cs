using System;

namespace Signify.eGFR.Core.Configs;

/// <summary>
/// Configurations for APIs
/// </summary>
public class WebApiConfig
{
    /// <summary>
    /// Configuration section key for this config
    /// </summary>
    public const string Key = "ApiUrls";

    /// <summary>
    /// Uri to the Signify Evaluation core API
    /// </summary>
    public Uri EvaluationApiUrl { get; set; }

    /// <summary>
    /// Uri to the Signify Member core API
    /// </summary>
    public Uri MemberApiUrl { get; set; }

    /// <summary>
    /// Uri to the Signify Provider core API
    /// </summary>
    public Uri ProviderApiUrl { get; set; }

    /// <summary>
    /// Uri to the Signify RCM API
    /// </summary>
    public Uri RcmApiUrl { get; set; }
        
    /// <summary>
    /// Uri to the Signify Provider pay API
    /// </summary>
    public Uri ProviderPayApiUrl { get; set; }

    /// <summary>
    /// Uri to the Signify Internal Lab Result API
    /// </summary>
    public Uri InternalLabResultApiUrl { get; set; }
}