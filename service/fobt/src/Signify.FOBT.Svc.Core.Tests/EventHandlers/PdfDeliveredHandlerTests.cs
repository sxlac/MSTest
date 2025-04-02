using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class PdfDeliveredHandlerTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ProductFilter_Tests(bool shouldProcess)
    {
        var message = new PdfDeliveredToClient
        {
            ProductCodes = []
        };

        var productFilter = A.Fake<IProductFilter>();
        A.CallTo(() => productFilter.ShouldProcess(A<IEnumerable<string>>._))
            .Returns(shouldProcess);

        var session = A.Fake<IMessageSession>();
            
        var publishObservability = A.Fake<IPublishObservability>();
            
        var subject = new PdfDeliveredHandler(A.Dummy<ILogger<PdfDeliveredHandler>>(), session, productFilter, publishObservability);

        await subject.Handle(message, default);

        A.CallTo(session)
            .MustHaveHappened(shouldProcess ? 1 : 0, Times.Exactly);
    }
}