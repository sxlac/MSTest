using Signify.Spirometry.Core.Exceptions;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class HoldNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        // Arrange
        var cdiHoldId = Guid.NewGuid();

        // Act
        var ex = new HoldNotFoundException(cdiHoldId);

        // Assert
        Assert.Equal(cdiHoldId, ex.CdiHoldId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new HoldNotFoundException(Guid.Empty);

        Assert.Equal("Unable to find a hold with CdiHoldId=00000000-0000-0000-0000-000000000000", ex.Message);
    }
}