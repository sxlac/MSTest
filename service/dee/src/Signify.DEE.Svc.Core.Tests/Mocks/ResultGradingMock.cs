using Iris.Public.Types.Models;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Tests.Mocks;

public static class ResultGradingMock
{
    public static ResultGrading BuildResultGrading()
    {
        return new ResultGrading
        {
            OS = new ResultEyeSideGrading
            {
                Gradable = true,
                UngradableReasons = new List<string>(),
                Findings = new List<ResultFinding>()
            },
            OD = new ResultEyeSideGrading
            {
                Gradable = true,
                UngradableReasons = new List<string>(),
                Findings = new List<ResultFinding>()
            }
        };
    }

    public static ResultGrading BuildResultGrading(bool? osGradable, List<string> osUngradableReasons, bool? odGradable, List<string> odUngradableReasons)
    {
        return new ResultGrading
        {
            OS = osGradable == null ? null : new ResultEyeSideGrading
            {
                Gradable = osGradable,
                UngradableReasons = osUngradableReasons,
                Findings = new List<ResultFinding>()
            },
            OD = odGradable == null ? null : new ResultEyeSideGrading
            {
                Gradable = odGradable,
                UngradableReasons = odUngradableReasons,
                Findings = new List<ResultFinding>()
            }
        };
    }

    public static ResultEyeSideGrading BuildResultEyeSideGrading(bool gradable, List<string> nonGradableReasons)
    {
        return new ResultEyeSideGrading
        {
            Gradable = gradable,
            UngradableReasons = nonGradableReasons,
            Findings = new List<ResultFinding>()
        };
    }
}