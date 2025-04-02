using AutoMapper;
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

public class EvaluationFinalizedHandlerTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ProductFilter_Tests(bool shouldProcess)
    {
        // Arrange
        var evaluationFinalizedEvent = Mocks.Models.EvaluationFinalizedEventMock.BuildEvaluationFinalizedEvent(new List<Product>()
        {
            new Product("HHRA"),
            new Product("HBA1CPOC")
        });

        var productFilter = A.Fake<IProductFilter>();
        A.CallTo(() => productFilter.ShouldProcess(A<IEnumerable<Product>>._))
            .Returns(shouldProcess);

        var session = A.Fake<IMessageSession>();

        var publishObservability = A.Fake<IPublishObservability>();
            
        var subject = new EvaluationFinalizedHandler(A.Dummy<ILogger<EvaluationFinalizedHandler>>(),
            session, A.Dummy<IMapper>(), productFilter, publishObservability);
            
        // Act
        await subject.Handle(evaluationFinalizedEvent, default);

        // Assert
        A.CallTo(session)
            .MustHaveHappened(shouldProcess ? 1 : 0, Times.Exactly);
    }
}