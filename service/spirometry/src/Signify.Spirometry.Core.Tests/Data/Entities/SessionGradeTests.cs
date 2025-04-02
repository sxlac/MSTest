using System.Linq;
using System;
using Xunit;

using SessionGradeEntity = Signify.Spirometry.Core.Data.Entities.SessionGrade;
using SessionGradeModel = Signify.Spirometry.Core.Models.SessionGrade;

namespace Signify.Spirometry.Core.Tests.Data.Entities;

public class SessionGradeTests
{
    [Fact]
    public void EntityEnumerationCount_Matches_ModelEnumerationCount()
    {
        // Arrange
        var expectedCount = Enum.GetValues<SessionGradeModel>().Length;

        // Act
        var actualCount = SessionGradeEntity.A.GetAllEnumerations().Count();

        // Assert
        Assert.Equal(expectedCount, actualCount);
    }
}