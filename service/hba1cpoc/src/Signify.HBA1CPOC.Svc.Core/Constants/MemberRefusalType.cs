using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Constants;

[ExcludeFromCodeCoverage]
public static class MemberRefusalType
{
    public const int MemberRecentlyCompleted = 33074;

    public const int ScheduledToComplete = 33075;

    public const int MemberApprehension = 33076;

    public const int NotInterested = 33077;

    public const int Other = 33078;
}