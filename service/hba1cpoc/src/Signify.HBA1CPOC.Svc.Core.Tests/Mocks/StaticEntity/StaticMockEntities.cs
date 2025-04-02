using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Events;
using System;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;

public static class StaticMockEntities
{
    public static EvaluationFinalizedEvent EvaluationFinalizedEvent => new()
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
        Products = [new Product("HHRA"), new Product("HBA1CPOC")]
    };

    public static MemberInfoRs MemberInfoRs => new()
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

    public static InventoryUpdated InventoryUpdated => new()
    {
        RequestId = Guid.NewGuid(),
        ItemNumber = "HBA1CPOC",
        Result = new Result(),
        SerialNumber = "000000",
        Quantity = 1,
        ProviderId = -1,
        DateUpdated = new DateTime(),
        ExpirationDate = new DateTime()
    };

    public static EvalReceived EvalReceived => new()
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
        IsLabPerformed = true
    };

    public static CreateOrUpdateHBA1CPOC CreateOrUpdateHba1Cpoc => new()
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
        A1CPercent = "6",
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow),
        ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow),
        FirstName = "Adarsh",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka"
    };

    public static Core.Data.Entities.HBA1CPOC Hba1Cpoc => new()
    {
        HBA1CPOCId = +10,
        AddressLineOne = "4420 Harpers Ferry Dr",
        AddressLineTwo = "Harpers Ferry Dr",
        ApplicationId = "Signify.Evaluation.Service",
        AppointmentId = 1000084715,
        A1CPercent = "6",
        CenseoId = "Adarsh1234",
        City = "Mysuru",
        ClientId = 14,
        CreatedDateTime = DateTimeOffset.UtcNow,
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-47)),
        DateOfService = DateTime.UtcNow,
        EvaluationId = 324356,
        ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow),
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

    public static Core.Data.Entities.HBA1CPOC CreateHba1Cpoc => new()
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
        A1CPercent = "8",
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-66)),
        ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow),
        FirstName = "Adarsh",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka",
        UserName = "ADarsh",
        ZipCode = "12345"
    };

    public static CreateHbA1CPoc CreateHbA1cPoc => new()
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
        UserName = "ranjeet",
        Products = [new Product("HHRA"), new Product("HBA1CPOC")]
    };

    public static HBA1CPOCStatus CreateHbA1cPocStatus(int statusCodeId, string statusCode) => new()
    {
        HBA1CPOCStatusId = 1,
        HBA1CPOCStatusCode = new HBA1CPOCStatusCode(statusCodeId, statusCode),
        CreatedDateTime = DateTime.UtcNow
    };

    public static CreateOrUpdatePDFToClient BuildCreateOrUpdatePDFToClient => new()
    {
        PDFDeliverId = 1,
        EventId = Guid.NewGuid(),
        EvaluationId = 123456789,
        DeliveryDateTime = DateTime.UtcNow,
        DeliveryCreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
        BatchId = 9876543210,
        BatchName = string.Empty,
        HBA1CPOCId = 1
    };

    public static PDFToClient CreatePdfToClient => new()
    {
        PDFDeliverId = 1234,
        EvaluationId = 123456,
        DeliveryDateTime = DateTime.UtcNow.AddHours(-1),
        DeliveryCreatedDateTime = DateTime.UtcNow.AddHours(-2),
        BatchId = 1,
        HBA1CPOCId = 1234
    };
}