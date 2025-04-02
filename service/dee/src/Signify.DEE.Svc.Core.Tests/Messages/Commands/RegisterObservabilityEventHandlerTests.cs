using FakeItEasy;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands
{
    public class RegisterObservabilityEventHandlerTests
    {
        readonly IPublishObservability publishObservability = A.Fake<IPublishObservability>();

        private readonly RegisterObservabilityEventHandler handler;
        public RegisterObservabilityEventHandlerTests()
        {
            handler = new RegisterObservabilityEventHandler(publishObservability);
        }

        [Fact]
        public async Task WhenHandlerCalled_ShouldRegisterEvent()
        {
            RegisterObservabilityEvent evt = new RegisterObservabilityEvent()
            {
                EvaluationId = 5
            };

            await handler.Handle(evt, CancellationToken.None);

            A.CallTo(() => publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedOnceExactly();
        }
    }
}
