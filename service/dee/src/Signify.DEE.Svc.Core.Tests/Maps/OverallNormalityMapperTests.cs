using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Maps;
using System.Collections.Generic;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Maps;

public class OverallNormalityMapperTests
{
    [Theory]
    [MemberData(nameof(GetOverallNormalityTestData))]
    public void GetOverallNormalityTests(IEnumerable<string> normalityIndicators, string expectedNormality)
    {
        var subject = new OverallNormalityMapper(A.Dummy<ILogger<OverallNormalityMapper>>());

        var actual = subject.GetOverallNormality(normalityIndicators);

        Assert.Equal(expectedNormality, actual);
    }

    public static IEnumerable<object[]> GetOverallNormalityTestData()
    {
        const string normal = "N";
        const string abnormal = "A";
        const string undetermined = "U";

        yield return new object[]
        {
            new[] { abnormal },
            abnormal
        };

        yield return new object[]
        {
            new[] { normal },
            normal
        };

        yield return new object[]
        {
            new[] { undetermined },
            undetermined
        };

        yield return new object[]
        {
            new string[] { },
            undetermined
        };

        yield return new object[]
        {
            new[] { abnormal, normal },
            abnormal
        };

        yield return new object[]
        {
            new[] { abnormal, undetermined },
            abnormal
        };

        yield return new object[]
        {
            new[] { abnormal, normal, undetermined },
            abnormal
        };

        yield return new object[]
        {
            new[] { undetermined, normal, abnormal },
            abnormal
        };

        yield return new object[]
        {
            new[] { undetermined, normal },
            undetermined
        };

        yield return new object[]
        {
            new[] { normal, undetermined, normal },
            undetermined
        };

        #region Edge cases
        yield return new object[]
        {
            new[] { "invalid normality" },
            undetermined
        };

        yield return new object[]
        {
            new[] { abnormal, "invalid normality" },
            abnormal
        };

        yield return new object[]
        {
            new[] { normal, "invalid normality" },
            undetermined
        };
        #endregion Edge cases
    }
}