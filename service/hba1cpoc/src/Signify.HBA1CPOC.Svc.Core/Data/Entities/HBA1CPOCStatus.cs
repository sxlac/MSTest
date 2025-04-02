using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class HBA1CPOCStatus
{
    public int HBA1CPOCStatusId { get; set; }

    //Foreign key 
    public virtual HBA1CPOCStatusCode HBA1CPOCStatusCode { get; set; }

    public int HBA1CPOCStatusCodeId { get; set; }

    //Foreign key
    public virtual HBA1CPOC HBA1CPOC { get; set; }
        
    public int HBA1CPOCId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public HBA1CPOCStatus()
    {

    }

    public HBA1CPOCStatus(int hba1CpocStatusId, HBA1CPOCStatusCode hba1CpocStatusCode, HBA1CPOC hba1cpoc, DateTimeOffset createdDateTime)
    {
        HBA1CPOCStatusId = hba1CpocStatusId;
        HBA1CPOCStatusCode = hba1CpocStatusCode;
        HBA1CPOC = hba1cpoc;
        CreatedDateTime = createdDateTime;
    }
        
    public override string ToString()
    {
        return $"{nameof(HBA1CPOCStatusId)}: {HBA1CPOCStatusId}, {nameof(HBA1CPOCStatusCode)}: {HBA1CPOCStatusCode}, {nameof(HBA1CPOCId)}: {HBA1CPOCId}, {nameof(HBA1CPOC)}: {HBA1CPOC}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
    }
}