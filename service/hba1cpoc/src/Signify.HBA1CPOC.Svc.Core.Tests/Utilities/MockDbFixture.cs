using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Utilities;

public sealed class MockDbFixture : IAsyncDisposable, IDisposable
{
    public Hba1CpocDataContext Context { get; }

    public MockDbFixture()
    {
        Context = new Hba1CpocDataContext(GetDbOptions());

        PopulateFakeData();
    }

    private void PopulateFakeData()
    {
        var exams = new List<Core.Data.Entities.HBA1CPOC>
        {
            new() {HBA1CPOCId = 1,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,A1CPercent = "6",CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow),DateOfService = DateTime.UtcNow,EvaluationId = 324356,ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow),FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"},
            new() {HBA1CPOCId = 2,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,A1CPercent = "7.8",CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow),DateOfService = DateTime.UtcNow,EvaluationId = 324357,ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow),FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"},
            new() {HBA1CPOCId = 3,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,A1CPercent = "8",CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow),DateOfService = DateTime.UtcNow,EvaluationId = 324358,ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow),FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"}
        };

        if (!Context.HBA1CPOC.Any())
        {
            Context.HBA1CPOC.AddRange(exams);
            Context.SaveChanges();
        }

        if (!Context.NotPerformedReason.Any())
        {
            var notPerformedReasons = new List<NotPerformedReason>
            {
                NotPerformedReason.MemberRecentlyCompleted,
                NotPerformedReason.ScheduledToComplete,
                NotPerformedReason.MemberApprehension,
                NotPerformedReason.NotInterested,
                NotPerformedReason.Other,
                NotPerformedReason.Technical,
                NotPerformedReason.EnvironmentalIssue,
                NotPerformedReason.NoSuppliesOrEquipment,
                NotPerformedReason.InsufficientTraining,
                NotPerformedReason.MemberPhysicallyUnable
            };

            Context.NotPerformedReason.AddRange(notPerformedReasons);
            Context.SaveChanges();
        }

        if (!Context.HBA1CPOCStatus.Any())
        {
            var statusCodes = new List<HBA1CPOCStatusCode>
            {
                HBA1CPOCStatusCode.HBA1CPOCPerformed,
                HBA1CPOCStatusCode.InventoryUpdateRequested,
                HBA1CPOCStatusCode.InventoryUpdateSuccess,
                HBA1CPOCStatusCode.InventoryUpdateFail,
                HBA1CPOCStatusCode.BillRequestSent,
                HBA1CPOCStatusCode.BillableEventRecieved,
                HBA1CPOCStatusCode.HBA1CPOCNotPerformed
            };

            Context.HBA1CPOCStatusCode.AddRange(statusCodes);
            Context.SaveChanges();
        }

        if (!Context.HBA1CPOCStatus.Any())
        {
            var statuses = new List<HBA1CPOCStatus>
            {
                new() { HBA1CPOCStatusId = 1, HBA1CPOCStatusCode = HBA1CPOCStatusCode.HBA1CPOCPerformed, HBA1CPOC = exams[0], CreatedDateTime = DateTime.UtcNow.AddMinutes(-1) },
                new() { HBA1CPOCStatusId = 2, HBA1CPOCStatusCode = HBA1CPOCStatusCode.HBA1CPOCPerformed, HBA1CPOC = exams[1], CreatedDateTime = DateTime.UtcNow.AddMinutes(-1) },
                new() { HBA1CPOCStatusId = 3, HBA1CPOCStatusCode = HBA1CPOCStatusCode.HBA1CPOCPerformed, HBA1CPOC = exams[2], CreatedDateTime = DateTime.UtcNow.AddMinutes(-1) }
            };

            Context.HBA1CPOCStatus.AddRange(statuses);
            Context.SaveChanges();
        }

        if (!Context.PDFToClient.Any())
        {
            var pdfToClientEntries = new List<PDFToClient>
            {
                new() { PDFDeliverId = 1, EventId = Guid.NewGuid().ToString(), EvaluationId = exams[0].EvaluationId.Value, DeliveryDateTime = DateTime.UtcNow, DeliveryCreatedDateTime = DateTime.UtcNow, BatchId = 12345, BatchName = string.Empty, HBA1CPOCId = exams[0].HBA1CPOCId },
                new() { PDFDeliverId = 2, EventId = Guid.NewGuid().ToString(), EvaluationId = exams[1].EvaluationId.Value, DeliveryDateTime = DateTime.UtcNow, DeliveryCreatedDateTime = DateTime.UtcNow, BatchId = 12346, BatchName = string.Empty, HBA1CPOCId = exams[1].HBA1CPOCId },
                new() { PDFDeliverId = 3, EventId = Guid.NewGuid().ToString(), EvaluationId = exams[2].EvaluationId.Value, DeliveryDateTime = DateTime.UtcNow, DeliveryCreatedDateTime = DateTime.UtcNow, BatchId = 12347, BatchName = string.Empty, HBA1CPOCId = exams[2].HBA1CPOCId }
            };

            Context.PDFToClient.AddRange(pdfToClientEntries);
            Context.SaveChanges();
        }

        if (!Context.HBA1CPOCRCMBilling.Any())
        {
            var rcmBillingEntries = new List<HBA1CPOCRCMBilling>
            {
                new() { Id = 1, BillId = "177141de-52f1-5514-8d68-ee0c3c5ee680", HBA1CPOCId = exams[0].HBA1CPOCId, CreatedDateTime = DateTime.UtcNow },
                new() { Id = 2, BillId = "29a645df-2419-5468-9132-b703ee84b00b", HBA1CPOCId = exams[1].HBA1CPOCId, CreatedDateTime = DateTime.UtcNow },
                new() { Id = 3, BillId = "d595b8c8-6864-5343-a87e-b765996962a3", HBA1CPOCId = exams[2].HBA1CPOCId, CreatedDateTime = DateTime.UtcNow }
            };

            Context.HBA1CPOCRCMBilling.AddRange(rcmBillingEntries);
            Context.SaveChanges();
        }
    }

    private static DbContextOptions<Hba1CpocDataContext> GetDbOptions() =>
        new DbContextOptionsBuilder<Hba1CpocDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    public void Dispose()
    {
        Context.Dispose();
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}