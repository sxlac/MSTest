using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using Signify.eGFR.Core.Configs;

namespace Signify.eGFR.Core.FeatureFlagging;

public abstract class FeatureFlagsBase
{
    private const string LaunchDarklyContextKey = "eGFR";
    
    private readonly Context _ldContext;
    private readonly LdClient _sharedClient;
    private readonly LdClient _projectClient;
    
    protected readonly LaunchDarklyConfig LdConfig;
    protected FeatureFlagsBase(LaunchDarklyConfig ldConfig)
    {
        _ldContext = Context.Builder(LaunchDarklyContextKey).Build();
        
        // if a SharedLicenseKey is provided, create a shared Launch Darkly client using the key
        if (!string.IsNullOrEmpty(ldConfig.SharedLicenseKey))
        {
            _sharedClient = new LdClient(ldConfig.SharedLicenseKey);
        }
        // if a ProjectLicenseKey is provided, create a project Launch Darkly client using the key
        if (!string.IsNullOrEmpty(ldConfig.ProjectLicenseKey))
        {
            _projectClient = new LdClient(ldConfig.ProjectLicenseKey);
        }
        
        LdConfig = ldConfig;
    }
    
    protected FeatureFlagsBase(LaunchDarklyConfig ldConfig, LdClient sharedClient, LdClient projectClient)
    {
        _ldContext = Context.Builder(LaunchDarklyContextKey).Build();
        _sharedClient = sharedClient;
        _projectClient = projectClient;
        LdConfig = ldConfig;
    }
    
    /// <summary>
    /// This method performs an evaluation on the value for the given feature flag. It does so by referring to the
    /// relevant (shared or project) Launch Darkly client. The client checks the in-memory cache for an existing value
    /// for the flag, if one does not exist it will make a request to the Launch Darkly service to get the current
    /// value of the flag.
    /// </summary>
    /// <param name="flag">the config for a feature flag we want to evaluate</param>
    /// <returns>the value of the feature flag, true or false</returns>
    protected bool Evaluate(LaunchDarklyFlagConfig flag)
    {
        return flag.FlagType switch
        {
            LaunchDarklyFlagConfig.FeatureFlagType.Shared when _sharedClient != null =>
                _sharedClient.BoolVariation(flag.FlagName, _ldContext, flag.FlagDefault),
            
            LaunchDarklyFlagConfig.FeatureFlagType.Project when _projectClient != null =>
                _projectClient.BoolVariation(flag.FlagName, _ldContext, flag.FlagDefault),
            
            // where the flag type does not resolve to shared or project, or the shared/project Launch Darkly client
            // has not been initialised, return the flags default value as configured in the appsettings.json
            _ => flag.FlagDefault
        };
    }
}