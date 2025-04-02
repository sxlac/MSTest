using Iris.Public.Types.Models;
using System.Linq;

namespace Signify.DEE.Svc.Core.Messages.Services;

public interface IDetermineGradability
{
    bool IsGradable(ResultGrading gradings);
}
    
public class DetermineGradability : IDetermineGradability
{
    public bool IsGradable(ResultGrading gradings)
    {
        if (gradings?.OD?.Findings != null && gradings.OD.Findings.Any()) return true;
        if (gradings?.OS?.Findings != null && gradings.OS.Findings.Any()) return true;
        return false;
    }
}