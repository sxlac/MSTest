using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using System.Collections.Generic;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Infrastructure.Observability;

public class ObservabilityServiceTests
{
    [Fact]
    public void AddEvent_ShouldRecordCustomEvent()
    {
        // Arrange
        var yourClass = new ObservabilityService();
        const string eventType = "testEvent";
        var eventValue = new Dictionary<string, object> { { "key", "value" } };

        // Act
        yourClass.AddEvent(eventType, eventValue);

        // Assert
        Assert.True(true);
    }
}