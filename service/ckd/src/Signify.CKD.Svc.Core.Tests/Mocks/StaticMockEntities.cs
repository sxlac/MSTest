using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using System;
using System.Collections.Generic;

namespace Signify.CKD.Svc.Core.Tests.Mocks.StaticEntity
{
    public static class StaticMockEntities
    {
        public static EvaluationFinalizedEvent EvaluationFinalizedEvent => new EvaluationFinalizedEvent
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
            Products = new List<Product> { new Product("HHRA"), new Product("CKD") }
        };

        public static MemberInfoRs MemberInfoRs => new MemberInfoRs
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

        public static InventoryUpdated InventoryUpdated => new InventoryUpdated
        {
            RequestId = Guid.NewGuid(),
            ItemNumber = "CKD",
            Result = new Result(),
            SerialNumber = "000000",
            Quantity = 1,
            ProviderId = -1,
            DateUpdated = new DateTime(),
            ExpirationDate = new DateTime()
        };

        public static EvalReceived EvalReceived => new EvalReceived
        {
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            DocumentPath = null,
            EvaluationId = 324359,
            EvaluationTypeId = 1,
            FormVersionId = 0,
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            UserName = "vastest1",
        };

        public static CreateOrUpdateCKD CreateOrUpdateCKD => new CreateOrUpdateCKD
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
            ExpirationDate = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            NationalProviderIdentifier = "1234567890",
            State = "karnataka"
        };

        public static Core.Data.Entities.CKD CKD => new Core.Data.Entities.CKD
        {
            CKDId = +10,
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
            ExpirationDate = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            MemberId = 11990396,
            MemberPlanId = 21074285,
            NationalProviderIdentifier = "9230239051",
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            State = "Karnataka",
            UserName = "vastest1",
            ZipCode = "12345",
            CKDAnswer = "Albumin: 80 - Creatinine: 0.1 ; High Abnormal"
        };

        public static Core.Data.Entities.CKD CreateCKD => new Core.Data.Entities.CKD
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
            ExpirationDate = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            NationalProviderIdentifier = "1234567890",
            State = "karnataka",
            UserName = "ADarsh",
            ZipCode = "12345"
        };

        public static CKDStatus CreateCKDStatus(int statusCodeId, string statusCode) => new CKDStatus
        {
            CKDStatusId = 1,
            CKD = CKD,
            CKDStatusCode = new CKDStatusCode(statusCodeId, statusCode),
            CreatedDateTime = DateTime.UtcNow            
        };

        public static CreateOrUpdatePDFToClient BuildCreateOrUpdatePDFToClient => new CreateOrUpdatePDFToClient
        { 
            PDFDeliverId = 1,
            EventId = Guid.NewGuid(),
            EvaluationId = 123456,
            DeliveryDateTime = DateTime.UtcNow,
            DeliveryCreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
            BatchId = 123456789,
            BatchName = string.Empty,
            CKDId = 1,           
        };

        public static PDFToClient CreatePdfToClient => new PDFToClient
        {
            PDFDeliverId = 1234,
            EvaluationId = 123456,
            DeliveryDateTime = DateTime.UtcNow.AddHours(-1),
            DeliveryCreatedDateTime = DateTime.UtcNow.AddHours(-2),
            BatchId = 1,
            CKDId = 1234
        };

        public static PdfDeliveredToClient BuildPdfDeliveredToClient()
        {
            return new PdfDeliveredToClient
            {
                EventId = Guid.NewGuid(),
                EvaluationId = 123456,
                ProductCodes = new List<string> { "FOBT", "CKD" },
                DeliveryDateTime = DateTime.UtcNow,
                CreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
                BatchId = 123456789,
                BatchName = string.Empty                
            };
        }
    }
}
