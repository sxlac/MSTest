using System.Linq;
using System;
using Xunit;

using NormalityEntity = Signify.Spirometry.Core.Data.Entities.NormalityIndicator;
using NormalityModel = Signify.Spirometry.Core.Models.NormalityIndicator;

namespace Signify.Spirometry.Core.Tests.Data.Entities;

public class NormalityIndicatorTests
{
    [Fact]
    public void EntityEnumerationCount_Matches_ModelEnumerationCount()
    {
        // Arrange
        var expectedCount = Enum.GetValues<NormalityModel>().Length;

        // Act
        var actualCount = NormalityEntity.Undetermined.GetAllEnumerations().Count();

        // Assert
        Assert.Equal(expectedCount, actualCount);
    }
}