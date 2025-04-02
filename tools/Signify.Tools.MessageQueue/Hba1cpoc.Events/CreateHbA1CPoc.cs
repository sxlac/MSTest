using NServiceBus;

namespace Hba1cpoc.Events
{
    public class CreateHbA1CPoc : ICommand
    {
        public Guid Id { get; set; }
        public int EvaluationId { get; set; }
        public int EvaluationTypeId { get; set; }
        public int FormVersionId { get; set; }
        public int? ProviderId { get; set; }
        public string UserName { get; set; }
        public int AppointmentId { get; set; }
        public string ApplicationId { get; set; }
        public int MemberPlanId { get; set; }
        public int MemberId { get; set; }
        public int ClientId { get; set; }
        public string DocumentPath { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTime ReceivedDateTime { get; set; }
        public DateTime? DateOfService { get; set; }
        public List<Hba1cpoc.Events.Product> Products { get; set; }
        public Hba1cpoc.Events.Location Location { get; set; }

        public CreateHbA1CPoc()
        {
        }
    }

    public sealed class Product
    {
        public string ProductCode { get; set; }

        public Product(string productCode)
        {
            ProductCode = productCode;
        }

        private bool Equals(Product other)
        {
            return ProductCode == other.ProductCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((Product)obj);
        }

        public override int GetHashCode()
        {
            return (ProductCode != null ? ProductCode.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return $"{nameof(ProductCode)}: {ProductCode}";
        }
    }


    public sealed class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Location(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        private bool Equals(Location other)
        {
            return Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((Location)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Latitude.GetHashCode() * 397) ^ Longitude.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{nameof(Latitude)}: {Latitude}, {nameof(Longitude)}: {Longitude}";
        }
    }
}
