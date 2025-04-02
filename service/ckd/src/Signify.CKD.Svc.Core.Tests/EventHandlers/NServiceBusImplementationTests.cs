using NServiceBus;
using Signify.CKD.Svc.Core.EventHandlers;
using System.Linq;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers
{
    public class NServiceBusImplementationTest
    {
        [Fact]
        public void AllNsbHandlers_HandleMessages_OfTypeIMessage()
        {
            var handlerType = typeof(IHandleMessages<>);
            var iMessageType = typeof(IMessage);

            // Find all types in the assembly that implement NServiceBus.IHandleMessages<>
            // Ensure all generic types of this interface (ie T of IHandleMessages<T>) implements NServiceBus.IMessage
            foreach (var type in typeof(EvaluationFinalizedHandler).Assembly.GetTypes())
            {
                var iHandleMessagesTypes = type.GetInterfaces().Where(each => each.Namespace == handlerType.Namespace && each.Name == handlerType.Name);
                foreach (var t in iHandleMessagesTypes)
                {
                    var args = t.GetGenericArguments()[0];
                    Assert.Contains(iMessageType, args.GetInterfaces());
                }
            }
        }
    }
}
