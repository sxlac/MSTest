using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class HBA1CPOCRCMBilling
{        
    public int Id { get; set; }

    public string BillId { get; set; }

    //Foreign key
    public virtual HBA1CPOC HBA1CPOC { get; set; }
        
    public int HBA1CPOCId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public bool? Accepted { get; set; }
        
    public DateTimeOffset? AcceptedAt { get; set; }
        
    public HBA1CPOCRCMBilling()
    {

    }

    public HBA1CPOCRCMBilling(int id, string billId, HBA1CPOC hba1cpoc, DateTimeOffset createdDateTime)
    {
        Id = id;
        BillId = billId;
        HBA1CPOC = hba1cpoc;
        CreatedDateTime = createdDateTime;
    }


    public override string ToString()
    {
        return $"{nameof(Id)}: {Id}, {nameof(BillId)}: {BillId}, {nameof(HBA1CPOCId)}: {HBA1CPOCId}, {nameof(HBA1CPOC)}: {HBA1CPOC}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
    }
}