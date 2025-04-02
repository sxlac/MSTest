using System.Linq;
using System;
using Xunit;

using StatusEntity = Signify.Spirometry.Core.Data.Entities.StatusCode;
using StatusModel = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Tests.Data.Entities;

public class StatusCodeTests
{
    [Fact]
    public void EntityEnumerationCount_Matches_ModelEnumerationCount()
    {
        // Arrange
        var expectedCount = Enum.GetValues<StatusModel>().Length;

        // Act
        var actualCount = StatusEntity.SpirometryExamPerformed.GetAllEnumerations().Count();

        // Assert
        Assert.Equal(expectedCount, actualCount);
    }
}