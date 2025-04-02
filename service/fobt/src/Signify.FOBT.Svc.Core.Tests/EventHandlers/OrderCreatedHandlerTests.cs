using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class OrderCreatedHandlerTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ProductCode_Tests(bool shouldProcess)
    {
        // Arrange
        var request = new BarcodeUpdate
        {
            ProductCode = "product code"
        };

        var filter = A.Fake<IProductFilter>();
        A.CallTo(() => filter.ShouldProcess(A<IEnumerable<string>>._))
            .Returns(shouldProcess);

        var session = new TestableMessageSession();

        // Act
        var subject = new OrderCreatedHandler(A.Dummy<ILogger<OrderCreatedHandler>>(),
            session, filter);

        await subject.Handle(request, default);

        // Assert
        A.CallTo(() => filter.ShouldProcess(A<IEnumerable<string>>.That.Matches(p =>
                p.First() == request.ProductCode)))
            .MustHaveHappened();

        Assert.Equal(shouldProcess ? 1 : 0, session.SentMessages.Length);
        Assert.Empty(session.PublishedMessages);
    }
}