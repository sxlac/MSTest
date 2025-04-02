using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Tests.Mocks.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Utilities;

public sealed class MockDbFixture : IDisposable, IAsyncDisposable
{
    public FOBTDataContext Context { get; set; }

    public MockDbFixture()
    {
        var dbOptions = GetDbOptions();
        PopulateFakeData(dbOptions);
    }

    private void PopulateFakeData(DbContextOptions<FOBTDataContext> options)
    {
        Context = new FOBTDataContext(options);
        var isDirty = false;

        var fobtOne = new Core.Data.Entities.FOBT
        {
            FOBTId = 1, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr", ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084715, CenseoId = "TestName1234", City = "Mysuru", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 324356, FirstName = "TestName", LastName = "H R",
            MemberId = 11990396, MemberPlanId = 21074285, NationalProviderIdentifier = "9230239051", ProviderId = 42879, ReceivedDateTime = DateTime.UtcNow,
            State = "Karnataka", UserName = "vastest1", ZipCode = "12345", OrderCorrelationId = new Guid("e5bb25b9-3a01-4f28-b4dc-14408c902078")
        };
        var fobtTwo = new Core.Data.Entities.FOBT
        {
            FOBTId = 2, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr", ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084715, CenseoId = "TestName1234", City = "Mysuru", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 324357, FirstName = "TestName", LastName = "H R",
            MemberId = 11990396, MemberPlanId = 21074285, NationalProviderIdentifier = "9230239051", ProviderId = 42879, ReceivedDateTime = DateTime.UtcNow,
            State = "Karnataka", UserName = "vastest1", ZipCode = "12345"
        };
        var fobtThree = new Core.Data.Entities.FOBT
        {
            FOBTId = 3, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr", ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084715, CenseoId = "TestName1234", City = "Mysuru", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 324358, FirstName = "TestName", LastName = "H R",
            MemberId = 11990396, MemberPlanId = 21074285, NationalProviderIdentifier = "9230239051", ProviderId = 42879, ReceivedDateTime = DateTime.UtcNow,
            State = "Karnataka", UserName = "vastest1", ZipCode = "12345"
        };
        var mockFobt = FobtEntityMock.BuildFobt();

        if (!Context.FOBT.Any())
        {
            var ckd = new List<Core.Data.Entities.FOBT>
            {
                fobtOne, fobtTwo, fobtThree, mockFobt
            };

            Context.FOBT.AddRange(ckd);
            isDirty = true;
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
            isDirty = true;
        }

        if (!Context.FOBTStatusCode.Any())
        {
            Context.FOBTStatusCode.AddRange(FOBTStatusCode.All);
            isDirty = true;
        }

        if (!Context.FOBTStatus.Any())
        {
            var fobtStatusValues = new List<FOBTStatus>
            {
                new() { FOBTStatusId = 1, FOBTStatusCode = FOBTStatusCode.OrderUpdated, FOBT = fobtOne, CreatedDateTime = DateTime.UtcNow },
                new() { FOBTStatusId = 2, FOBTStatusCode = FOBTStatusCode.ClientPDFDelivered, FOBT = fobtTwo, CreatedDateTime = DateTime.UtcNow },
                new() { FOBTStatusId = 3, FOBTStatusCode = FOBTStatusCode.FOBTNotPerformed, FOBT = fobtThree, CreatedDateTime = DateTime.UtcNow },
            };

            Context.FOBTStatus.AddRange(fobtStatusValues);
            isDirty = true;
        }

        if (!Context.LabResults.Any())
        {
            var labResultsValues = new List<LabResults>
            {
                new()
                {
                    LabResultId = 1, OrderCorrelationId = new Guid("b65e62ed-fd2c-4b7f-b183-0e70973c1fe6"), FOBTId = fobtOne.FOBTId, Barcode = "12345678901234", LabResult = "Positive",
                    ProductCode = "FOBT", AbnormalIndicator = "N", CreatedDateTime = DateTime.UtcNow, ReleaseDate = DateTime.UtcNow,
                    CollectionDate = DateTime.UtcNow.AddMinutes(-10), ServiceDate = DateTime.UtcNow.AddMinutes(-10)
                },
                new()
                {
                    LabResultId = 2, OrderCorrelationId = new Guid("29b841cf-bf37-48ae-8423-f82e3b38b348"), FOBTId = fobtTwo.FOBTId, Barcode = "99945678901234", LabResult = "Negative",
                    ProductCode = "FOBT", AbnormalIndicator = "A", CreatedDateTime = DateTime.UtcNow, ReleaseDate = DateTime.UtcNow,
                    CollectionDate = DateTime.UtcNow.AddMinutes(-10), ServiceDate = DateTime.UtcNow.AddMinutes(-10)
                },
                new()
                {
                    LabResultId = 3, OrderCorrelationId = new Guid("a0bd6609-7b59-4929-888d-8552d94d282c"), FOBTId = fobtThree.FOBTId, Barcode = "88845678901234", LabResult = "Positive",
                    ProductCode = "FOBT", AbnormalIndicator = "N", CreatedDateTime = DateTime.UtcNow, ReleaseDate = DateTime.UtcNow,
                    CollectionDate = DateTime.UtcNow.AddMinutes(-10), ServiceDate = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            Context.LabResults.AddRange(labResultsValues);
            isDirty = true;
        }

        if (!Context.FOBTBilling.Any())
        {
            var fobtBillingValues = new List<FOBTBilling>
            {
                new()
                {
                    Id = 1, BillId = "C41D95C7-5F89-4BD6-8EFD-A26E3A3B52CA", BillingProductCode = ApplicationConstants.BILLING_PRODUCT_CODE_LEFT,
                    FOBTId = fobtOne.FOBTId, CreatedDateTime = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    Id = 2, BillId = "8EEE5D8A-7531-47EE-922E-BB1FEED0089C", BillingProductCode = ApplicationConstants.BILLING_PRODUCT_CODE_LEFT,
                    FOBTId = fobtTwo.FOBTId, CreatedDateTime = DateTime.UtcNow.AddDays(-1)
                }
            };

            Context.FOBTBilling.AddRange(fobtBillingValues);
            isDirty = true;
        }


        if (!Context.PDFToClient.Any())
        {
            var pdfToClientValues = new List<PDFToClient>
            {
                new() { EvaluationId = 1, FOBTId = 1, PDFDeliverId = 1 },
                new() { EvaluationId = 2, FOBTId = 2, PDFDeliverId = 2 }
            };

            Context.PDFToClient.AddRange(pdfToClientValues);
            isDirty = true;
        }

        if (!Context.FOBTBarcodeHistory.Any())
        {
            var fobtBarcodeHistoryValues = new List<FOBTBarcodeHistory>
            {
                FobtBarcodeHistoryMock.BuildFobtBarcodeHistory(fobtOne)
            };

            Context.FOBTBarcodeHistory.AddRange(fobtBarcodeHistoryValues);
            isDirty = true;
        }

        if (!Context.ProviderPay.Any())
        {
            var providerPayValues = new List<ProviderPay>
            {
                ProviderPayMock.BuildProviderPay(fobtOne)
            };

            Context.ProviderPay.AddRange(providerPayValues);
            isDirty = true;
        }

        if (isDirty)
        {
            Context.SaveChanges();
        }
    }

    private DbContextOptions<FOBTDataContext> GetDbOptions()
    {
        return new DbContextOptionsBuilder<FOBTDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    public void Dispose() =>
        Context.Dispose();

    public ValueTask DisposeAsync() =>
        Context.DisposeAsync();
}