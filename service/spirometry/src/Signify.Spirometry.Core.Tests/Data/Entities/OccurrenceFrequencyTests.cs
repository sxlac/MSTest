using System.Linq;
using System;
using Xunit;

using OccurrenceFrequencyEntity = Signify.Spirometry.Core.Data.Entities.OccurrenceFrequency;
using OccurrenceFrequencyModel = Signify.Spirometry.Core.Models.OccurrenceFrequency;

namespace Signify.Spirometry.Core.Tests.Data.Entities;

public class OccurrenceFrequencyTests
{
    [Fact]
    public void EntityEnumerationCount_Matches_ModelEnumerationCount()
    {
        // Arrange
        var expectedCount = Enum.GetValues<OccurrenceFrequencyModel>().Length;

        // Act
        var actualCount = OccurrenceFrequencyEntity.Never.GetAllEnumerations().Count();

        // Assert
        Assert.Equal(expectedCount, actualCount);
    }
}