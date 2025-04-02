using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.QE.Core.Exceptions;

namespace Signify.HBA1CPOC.System.Tests.Core.Actions;

public class ProviderPayActions: BaseTestActions
{
    public async Task<ProviderPayRequestSent> GetProviderPayRequestSentEvent(int evaluationId)
    {
        try
        {
            return await CoreKafkaActions.GetA1CpocProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
    }
    
    public async Task<ProviderPayableEventReceived> GetProviderPayableEventReceivedEvent(int evaluationId)
    {
        try
        {
            return await CoreKafkaActions.GetA1CpocProviderPayableEventReceivedEvent<ProviderPayableEventReceived>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
    }

    public async Task<ProviderNonPayableEventReceived> GetProviderNonPayableEventReceivedEvent(int evaluationId)
    {   
        try
        {
            return await CoreKafkaActions.GetA1CpocProviderNonPayableEventReceivedEvent<ProviderNonPayableEventReceived>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
    }
}