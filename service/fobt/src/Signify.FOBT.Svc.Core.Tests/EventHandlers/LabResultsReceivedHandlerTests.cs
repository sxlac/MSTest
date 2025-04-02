using FakeItEasy;
using FobtNsbEvents;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Filters;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class LabResultsReceivedHandlerTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ProductCode_Tests(bool matchProductCode)
    {
        var message = new HomeAccessResultsReceived();

        var productFilter = A.Fake<IProductFilter>();
        A.CallTo(() => productFilter.ShouldProcess(A<IEnumerable<string>>._))
            .Returns(matchProductCode);

        var endpoint = new TestableEndpointInstance();

        var subject = new LabResultsReceivedHandler(A.Dummy<ILogger<LabResultsReceivedHandler>>(),
            endpoint, productFilter);

        await subject.Handle(message, default);

        if (matchProductCode)
            Assert.Single(endpoint.SentMessages);
        else
            Assert.Empty(endpoint.SentMessages);
    }
}