using Signify.HBA1CPOC.Svc.Core.Maps;
using Signify.HBA1CPOC.Svc.Core.Models;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Maps;

public class NormalityMapperTests
{
    private static NormalityMapper CreateSubject() => new();

    [Theory]
    [InlineData(Normality.Normal, "N")]
    [InlineData(Normality.Abnormal, "A")]
    [InlineData(Normality.Undetermined, "U")]
    public void Convert_Normality_To_String(Normality normality, string normalityCode)
    {
        var subject = CreateSubject();
        var actual = subject.Convert(normality, "", null);
        Assert.Equal(normalityCode, actual);
    }

    [Theory]
    [InlineData("N", Normality.Normal)]
    [InlineData("A", Normality.Abnormal)]
    [InlineData("U", Normality.Undetermined)]
    public void Convert_String_To_Normality(string normalityCode, Normality normality)
    {
        var subject = CreateSubject();
        var actual = subject.Convert(normalityCode, new Normality(), null);
        Assert.Equal(normality, actual);
    }
}