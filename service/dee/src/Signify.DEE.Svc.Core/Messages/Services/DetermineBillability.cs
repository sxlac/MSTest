using Iris.Public.Types.Models;

namespace Signify.DEE.Svc.Core.Messages.Services;

public interface IDetermineBillability
{
    bool IsBillable(ResultGrading gradings, ResultImageDetails imageDetails);
}
    
public class DetermineBillability(IDetermineGradability determineGradability) : IDetermineBillability
{
    public bool IsBillable(ResultGrading gradings, ResultImageDetails imageDetails)
    {
        return determineGradability.IsGradable(gradings) &&
               imageDetails.LeftEyeOriginalCount > 0 &&
               imageDetails.RightEyeOriginalCount > 0;
    }
}