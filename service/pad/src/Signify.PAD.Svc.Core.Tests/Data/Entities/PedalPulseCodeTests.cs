using System.Linq;
using Xunit;
using PedalPulseCode = Signify.PAD.Svc.Core.Data.Entities.PedalPulseCode;

namespace Signify.PAD.Svc.Core.Tests.Data.Entities;

public class PedalPulseCodeTests
{
    [Fact]
    public void GetAllEnumerations_Returns_UniqueValues()
    {
        var all = PedalPulseCode.NotPerformed
            .GetAllEnumerations()
            .ToList();

        Assert.Equal(all.Count, all.GroupBy(sc => sc.PedalPulseCodeId).Count());
        Assert.Equal(all.Count, all.GroupBy(sc => sc.PedalPulse).Count());
    }

    [Fact]
    public void GetAllEnumerations_Returns_AllValues()
    {
        var all = PedalPulseCode.NotPerformed
            .GetAllEnumerations()
            .ToList();

        Assert.Contains(PedalPulseCode.Normal, all);
        Assert.Contains(PedalPulseCode.AbnormalLeft, all);
        Assert.Contains(PedalPulseCode.AbnormalRight, all);
        Assert.Contains(PedalPulseCode.AbnormalBilateral, all);
        Assert.Contains(PedalPulseCode.NotPerformed, all);
    }
}
