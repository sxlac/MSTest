using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using System.Threading.Tasks;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class A1CPocPerformedHandlerTests
{
    private readonly A1CPOCPerformedHandler _handler = new(A.Dummy<ILogger<A1CPOCPerformedHandler>>());

    [Fact]
    public Task Task_Completes()
    {
        // Arrange
        var request = new A1CPOCPerformed();

        // Act
        var test = _handler.Handle(request, new TestableInvokeHandlerContext());

        // Assert
        Assert.True(test.IsCompleted);
        return Task.CompletedTask;
    }
}