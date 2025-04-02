using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Models;
using System.Linq;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Data.Entities;

public class PadStatusCodeTests
{
    [Fact]
    public void GetAllEnumerations_Returns_UniqueValues()
    {
        var all = PADStatusCode.PadPerformed
            .GetAllEnumerations()
            .ToList();

        Assert.Equal(all.Count, all.GroupBy(sc => sc.PADStatusCodeId).Count());
        Assert.Equal(all.Count, all.GroupBy(sc => sc.StatusCode).Count());
    }

    [Fact]
    public void GetAllEnumerations_Returns_SameNumberOfElementsAsModel()
    {
        Assert.Equal(Enum.GetValues<StatusCodes>().Length, PADStatusCode.PadPerformed.GetAllEnumerations().Count());
    }

    [Fact]
    public void GetAllEnumerations_AllStatusCodeIds_MatchEnumIds()
    {
        var hs = PADStatusCode.PadPerformed.GetAllEnumerations()
            .Select(sc => sc.PADStatusCodeId)
            .ToHashSet();

        foreach (var sc in Enum.GetValues<StatusCodes>())
        {
            Assert.Contains((int)sc, hs);
        }
    }
}
