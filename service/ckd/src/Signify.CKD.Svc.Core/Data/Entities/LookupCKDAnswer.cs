using System.ComponentModel.DataAnnotations;

namespace Signify.CKD.Svc.Core.Data.Entities
{
    public class LookupCKDAnswer
    {
        [Key]
        public int CKDAnswerId { get; set; }
        public string CKDAnswerValue { get; set; }
        public int Albumin { get; set; }
        public decimal Creatinine { get; set; }
        public string Acr { get; set; }
        public string NormalityIndicator { get; set; }
        public string Severity { get; set; }
    }
}
