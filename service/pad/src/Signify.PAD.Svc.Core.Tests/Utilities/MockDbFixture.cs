using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Signify.PAD.Svc.Core.Tests.Utilities;

public sealed class MockDbFixture : IDisposable, IAsyncDisposable
{
    public PADDataContext Context { get; set; }

    public MockDbFixture()
    {
        var dbOptions = GetDbOptions();
        PopulateFakeData(dbOptions);
    }

    private void PopulateFakeData(DbContextOptions<PADDataContext> options)
    {
        Context = new PADDataContext(options);
        var pads = new List<Core.Data.Entities.PAD>
        {
            new()
            {
                PADId = 1, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr", ApplicationId = "Signify.Evaluation.Service",
                AppointmentId = 1000084715, RightScoreAnswerValue = "0.59", RightSeverityAnswerValue = "Significant", LeftScoreAnswerValue = "0.28",
                LeftSeverityAnswerValue = "Severe", CenseoId = "Adarsh1234", City = "Mysuru", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
                DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 324356, FirstName = "Adarsh", LastName = "H R",
                MemberId = 11990396, MemberPlanId = 21074285, NationalProviderIdentifier = "9230239051", ProviderId = 42879, ReceivedDateTime = DateTime.UtcNow,
                State = "Karnataka", UserName = "vastest1", ZipCode = "12345"
            },
            new()
            {
                PADId = 2, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr", ApplicationId = "Signify.Evaluation.Service",
                AppointmentId = 1000084715, RightScoreAnswerValue = "0.59", RightSeverityAnswerValue = "Significant", LeftScoreAnswerValue = "0.28",
                LeftSeverityAnswerValue = "Severe", CenseoId = "Adarsh1234", City = "Mysuru", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
                DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 324357, FirstName = "Adarsh", LastName = "H R",
                MemberId = 11990396, MemberPlanId = 21074286, NationalProviderIdentifier = "9230239051", ProviderId = 42879, ReceivedDateTime = DateTime.UtcNow,
                State = "Karnataka", UserName = "vastest1", ZipCode = "12345"
            },
            new()
            {
                PADId = 3, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr", ApplicationId = "Signify.Evaluation.Service",
                AppointmentId = 1000084715, RightScoreAnswerValue = "0.59", RightSeverityAnswerValue = "Significant", LeftScoreAnswerValue = "0.28",
                LeftSeverityAnswerValue = "Severe", CenseoId = "Adarsh1234", City = "Mysuru", ClientId = 14, CreatedDateTime = DateTimeOffset.UtcNow,
                DateOfBirth = DateTime.UtcNow, DateOfService = DateTime.UtcNow, EvaluationId = 324358, FirstName = "Adarsh", LastName = "H R",
                MemberId = 11990396, MemberPlanId = 122940331, NationalProviderIdentifier = "9230239051", ProviderId = 42879,
                ReceivedDateTime = DateTime.UtcNow, State = "Karnataka", UserName = "vastest1", ZipCode = "12345"
            }
        };
        if (!Context.PAD.Any())
        {
            Context.PAD.AddRange(pads);

            PopulateBillingRequests(pads.Where(each => each.PADId == 1));

            Context.PADStatusCode.AddRange(PADStatusCode.PadPerformed.GetAllEnumerations());

            Context.PADStatus.AddRange(
                new PADStatus { PAD = pads.First(each => each.PADId == 1), PADStatusCode = PADStatusCode.PadPerformed },
                new PADStatus { PAD = pads.First(each => each.PADId == 2), PADStatusCode = PADStatusCode.PadNotPerformed },
                new PADStatus { PAD = pads.First(each => each.PADId == 3), PADStatusCode = PADStatusCode.WaveformDocumentDownloaded },
                new PADStatus { PAD = pads.First(each => each.PADId == 3), PADStatusCode = PADStatusCode.WaveformDocumentUploaded }
            );

            Context.WaveformDocumentVendor.AddRange(new WaveformDocumentVendor { WaveformDocumentVendorId = 1, VendorName = "Semler Scientific" });

            Context.WaveformDocument.AddRange(new WaveformDocument
            {
                WaveformDocumentVendorId = 1, Filename = "WALKER_122940331_PAD_BL_080122.PDF", MemberPlanId = 122940331, CreatedDateTime = DateTime.Now,
                DateOfExam = DateTime.Now
            });

            Context.SaveChanges();
        }

        PopulatePdfToClient();
        PopulateProviderPay();
        PopulateSeverityLookup();
    }

    private void PopulatePdfToClient()
    {
        var pdf = new PDFToClient
        {
            PADId = 1,
            BatchId = 1,
            DeliveryCreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = 324357,
            PDFDeliverId = 1,
            EventId = "12345-test"
        };
        Context.PDFToClient.Add(pdf);
        Context.SaveChanges();
    }

    private void PopulateProviderPay()
    {
        var providerPay = new ProviderPay
        {
            ProviderPayId = 1,
            PaymentId = "1234ABCD",
            PADId = 1,
            CreatedDateTime = DateTime.Now
        };
        Context.ProviderPay.Add(providerPay);
        Context.SaveChanges();
    }

    private void PopulateSeverityLookup()
    {
        Context.SeverityLookup.AddRange(new SeverityLookup
        {
            SeverityLookupId = 1,
            MinScore = 1.00m,
            MaxScore = 1.40m,
            Severity = "Normal",
            NormalityIndicator = "N"
        }, new SeverityLookup
        {
            SeverityLookupId = 2,
            MinScore = 0.90m,
            MaxScore = 0.99m,
            Severity = "Borderline",
            NormalityIndicator = "N"
        }, new SeverityLookup
        {
            SeverityLookupId = 3,
            MinScore = 0.60m,
            MaxScore = 0.89m,
            Severity = "Mild",
            NormalityIndicator = "A"
        }, new SeverityLookup
        {
            SeverityLookupId = 4,
            MinScore = 0.30m,
            MaxScore = 0.59m,
            Severity = "Moderate",
            NormalityIndicator = "A"
        }, new SeverityLookup
        {
            SeverityLookupId = 5,
            MinScore = 0.00m,
            MaxScore = 0.29m,
            Severity = "Severe",
            NormalityIndicator = "A"
        });
        Context.SaveChanges();
    }

    private void PopulateBillingRequests(IEnumerable<Core.Data.Entities.PAD> pads)
    {
        Context.PADRCMBilling.AddRange(pads.Select(each => new PADRCMBilling { PAD = each, BillId = "0C58DF47-B8B7-48FF-BC4F-CC532B6DF653" }));
    }

    private DbContextOptions<PADDataContext> GetDbOptions()
        => new DbContextOptionsBuilder<PADDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    public void Dispose()
    {
        Context.Dispose();
    }

    public ValueTask DisposeAsync()
        => Context.DisposeAsync();
}