using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Signify.PAD.Svc.Core.Data.Entities;

public class WaveformDocument
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int WaveformDocumentId { get; set; }
    //Foreign key
    public virtual WaveformDocumentVendor WaveformDocumentVendor { get; set; }
    public int WaveformDocumentVendorId { get; set; }
    public string Filename { get; set; }
    public int MemberPlanId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime DateOfExam { get; set; }
}
