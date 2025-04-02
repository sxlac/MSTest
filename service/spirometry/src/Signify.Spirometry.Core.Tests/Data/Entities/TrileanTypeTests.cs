using System.Linq;
using System;
using Xunit;

using TrileanTypeEntity = Signify.Spirometry.Core.Data.Entities.TrileanType;
using TrileanTypeModel = Signify.Spirometry.Core.Models.TrileanType;

namespace Signify.Spirometry.Core.Tests.Data.Entities;

public class TrileanTypeTests
{
    [Fact]
    public void EntityEnumerationCount_Matches_ModelEnumerationCount()
    {
        // Arrange
        var expectedCount = Enum.GetValues<TrileanTypeModel>().Length;

        // Act
        var actualCount = TrileanTypeEntity.Unknown.GetAllEnumerations().Count();

        // Assert
        Assert.Equal(expectedCount, actualCount);
    }
}