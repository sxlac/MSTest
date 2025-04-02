using System.Collections.Generic;
using Iris.Public.Types.Models;

namespace Signify.DEE.Svc.Core.Messages.Models;

public abstract class BusinessRuleAnswers
{
    public IList<int> StatusCodes { get; set; }
    public ResultGrading Gradings { get; set; }
    public ResultImageDetails ImageDetails { get; set; }
    public bool HasEnucleation { get; set; }
}