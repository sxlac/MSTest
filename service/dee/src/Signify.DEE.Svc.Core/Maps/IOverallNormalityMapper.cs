using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Maps;

/// <summary>
/// Interface to determine overall normality given a collection of normality indicators
/// </summary>
public interface IOverallNormalityMapper
{
    public string GetOverallNormality(IEnumerable<string> normalityIndicators);
}