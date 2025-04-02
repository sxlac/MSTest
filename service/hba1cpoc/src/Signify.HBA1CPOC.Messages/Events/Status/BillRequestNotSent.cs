namespace Signify.HBA1CPOC.Messages.Events.Status
{
    public class BillRequestNotSent : BaseStatusMessage
    {
        public string BillingProductCode { get; set; }        
    }
}