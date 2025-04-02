using Signify.DEE.Svc.System.Tests.Core.Models.Database;

namespace Signify.DEE.Svc.System.Tests.Core.Constants;

public static class LateralityCodes
{
    public static readonly LateralityCode Od = new() { LateralityCodeId = 1, Name = "OD", Description = "Right, Oculu" };
    public static readonly LateralityCode Os = new() { LateralityCodeId = 2, Name = "OS", Description = "Left, Oculus Sinster" };
    public static readonly LateralityCode Ou = new() { LateralityCodeId = 3, Name = "OU", Description = "Both, Oculus Uterque" };
    public static readonly LateralityCode Unknown = new() { LateralityCodeId = 4, Name = "Unknown", Description = "Unknown" };
}