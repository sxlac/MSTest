using System;
using System.ComponentModel.DataAnnotations;

namespace Signify.A1C.Svc.Core.Data.Entities
{
    public sealed class  LabResults
    {
        [Key]
        public int LabResultId { get; set; }
        public int A1CId { get; set; }
        public DateTime ReceivedDateTime { get; set; }
        public Guid OrderCorrelationId { get; set; }
        public string Barcode { get; set; }
		public string LabResult { get; set; }
        public string ProductCode { get; set; }
        public string AbnormalIndicator { get; set; }
		public string Exception { get; set; }
		public DateTime? CollectionDate { get; set; }
		public DateTime? ServiceDate { get; set; }
		public DateTime? ReleaseDate { get; set; }

        private bool Equals(LabResults other)
        {
            return LabResultId == other.LabResultId && A1CId == other.A1CId && ReceivedDateTime.Equals(other.ReceivedDateTime) && Barcode == other.Barcode && LabResult == other.LabResult && AbnormalIndicator == other.AbnormalIndicator && Exception == other.Exception && CollectionDate.Equals(other.CollectionDate) && ServiceDate.Equals(other.ServiceDate) && ReleaseDate.Equals(other.ReleaseDate);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((LabResults)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = LabResultId.GetHashCode();
                hashCode = (hashCode * 397) ^ A1CId.GetHashCode();
                hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ OrderCorrelationId.GetHashCode();
                hashCode = (hashCode * 397) ^ Barcode.GetHashCode();
				hashCode = (hashCode * 397) ^ LabResult.GetHashCode();
				hashCode = (hashCode * 397) ^ AbnormalIndicator.GetHashCode();
				hashCode = (hashCode * 397) ^ Exception.GetHashCode();
                hashCode = (hashCode * 397) ^ CollectionDate.GetHashCode();
				hashCode = (hashCode * 397) ^ ServiceDate.GetHashCode();
				hashCode = (hashCode * 397) ^ ReleaseDate.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(LabResultId)}: {LabResultId}, {nameof(A1CId)}: {A1CId},{nameof(OrderCorrelationId)}: {OrderCorrelationId}, {nameof(Barcode)}: {Barcode},{nameof(LabResult)}: {LabResult}, {nameof(AbnormalIndicator)}: {AbnormalIndicator}, {nameof(Exception)}: {Exception}, {nameof(CollectionDate)}: {CollectionDate}, {nameof(ServiceDate)}: {ServiceDate}, {nameof(ReleaseDate)}: {ReleaseDate}";
        }
    }
}