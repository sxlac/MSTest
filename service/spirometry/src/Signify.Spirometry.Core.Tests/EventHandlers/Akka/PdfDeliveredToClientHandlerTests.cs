using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Spirometry.Core.EventHandlers.Akka;
using Signify.Spirometry.Core.Filters;
using SpiroEvents;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Akka;

public class PdfDeliveredClientHandlerTests
{
    private readonly TestableEndpointInstance _endpoint = new();
    private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();

    private PdfDeliveredToClientHandler CreateSubject()
        => new(A.Dummy<ILogger<PdfDeliveredToClientHandler>>(),
            _endpoint, _productFilter);

    private void SetupFilter(bool shouldProcess)
    {
        A.CallTo(() => _productFilter.ShouldProcess(A<IEnumerable<string>>._))
            .Returns(shouldProcess);
    }

    [Fact]
    public async Task Handle_WithNonSpiroProductCodes_DoesNothing()
    {
        var request = new PdfDeliveredToClient();

        SetupFilter(false);

        var subject = CreateSubject();

        await subject.Handle(request, default);

        Assert.Empty(_endpoint.SentMessages);
    }

    [Fact]
    public async Task Handle_WithSpiroProductCodes_EnqueuesEvent()
    {
        var request = new PdfDeliveredToClient();

        SetupFilter(true);

        var subject = CreateSubject();

        await subject.Handle(request, default);

        Assert.Single(_endpoint.SentMessages);
    }
}