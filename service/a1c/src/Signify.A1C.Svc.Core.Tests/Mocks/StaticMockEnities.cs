using Signify.A1C.Svc.Core.ApiClient.Response;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Events;
using System;
using System.Collections.Generic;

namespace Signify.A1C.Svc.Core.Tests.Mocks.StaticEntity
{
    public static class StaticMockEnities
    {
        public static EvaluationFinalizedEvent EvaluationFinalizedEvent => new EvaluationFinalizedEvent()
        {
            Id = new Guid(),
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            DocumentPath = null,
            EvaluationId = 324359,
            EvaluationTypeId = 1,
            FormVersionId = 0,
            Location = new Location(32.925496267, 32.925496267),
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            UserName = "vastest1",
            Products = new List<Product>() { new Product("HHRA"), new Product("A1CPOC") }
        };

        public static MemberInfoRs MemberInfoRs => new MemberInfoRs()
        {
            AddressLineOne = "Raghavendra nagara",
            AddressLineTwo = "mysuru",
            CenseoId = "ADarsh1234",
            City = "Mysuru",
            DateOfBirth = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            State = "karnataka",
            ZipCode = "12345",
            Client = "14",
            MiddleName = ""
        };
        public static InventoryUpdated InventoryUpdated = new InventoryUpdated
        (
            new Guid(),
            "A1C",
            new Result(),
            "000000",
            1,
            -1,
            new DateTime(),
            new DateTime()
        );
        public static CreateOrUpdateA1C CreateOrUpdateHba1Cpoc => new CreateOrUpdateA1C()
        {
            AddressLineOne = "Raghavendra nagara",
            AddressLineTwo = "mysuru",
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            EvaluationId = 324359,
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            CenseoId = "ADarsh1234",
            City = "Mysuru",
            DateOfBirth = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            NationalProviderIdentifier = "1234567890",
            State = "karnataka"
        };

        public static Core.Data.Entities.A1C ReturnA1C => new Core.Data.Entities.A1C()
        {
            A1CId = +10,
            AddressLineOne = "4420 Harpers Ferry Dr",
            AddressLineTwo = "Harpers Ferry Dr",
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084715,
            CenseoId = "Adarsh1234",
            City = "Mysuru",
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfBirth = DateTime.UtcNow,
            DateOfService = DateTime.UtcNow,
            EvaluationId = 324356,
            FirstName = "Adarsh",
            LastName = "H R",
            MemberId = 11990396,
            MemberPlanId = 21074285,
            NationalProviderIdentifier = "9230239051",
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            State = "Karnataka",
            UserName = "vastest1",
            ZipCode = "12345"
        };

        public static Core.Data.Entities.A1C CreateA1C => new Core.Data.Entities.A1C()
        {
            AddressLineOne = "Raghavendra nagara",
            AddressLineTwo = "mysuru",
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            EvaluationId = 324359,
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            CenseoId = "ADarsh1234",
            City = "Mysuru",
            DateOfBirth = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            NationalProviderIdentifier = "1234567890",
            State = "karnataka",
            UserName = "ADarsh",
            ZipCode = "12345"
        };

    }
}
