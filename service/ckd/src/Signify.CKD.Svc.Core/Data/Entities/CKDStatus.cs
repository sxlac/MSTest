using System;

namespace Signify.CKD.Svc.Core.Data.Entities;

public class CKDStatus
{
    public int CKDStatusId { get; set; }
    
    public int CKDId { get; set; }
   
    public virtual CKDStatusCode CKDStatusCode { get; set; }
    
    public int CKDStatusCodeId { get; set; }

    //Foreign key
    public virtual CKD CKD { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }
    
    public override string ToString()
    {
        return $"{nameof(CKDStatusId)}: {CKDStatusId}, {nameof(CKDStatusCode)}: {CKDStatusCode}, {nameof(CKD)}: {CKD}, {nameof(CKDId)}: {CKDId}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
    }
}