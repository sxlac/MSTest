using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Signify.CKD.Svc.Core.Data;
using System.Linq;
using Signify.CKD.Svc.Core.Data.Entities;
using System.Threading.Tasks;

namespace Signify.CKD.Svc.Core.Tests.Utilities;

public sealed class MockDbFixture : IAsyncDisposable, IDisposable
{
    public CKDDataContext Context { get; private set; }

    public MockDbFixture()
    {
        var dbOptions = GetDbOptions();
        PopulateFakeData(dbOptions);
    }

    private void PopulateFakeData(DbContextOptions<CKDDataContext> options)
    {
        Context = new CKDDataContext(options);
        var ckd = new List<Core.Data.Entities.CKD> {  new() {CKDId = 1,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,CKDAnswer = "Albumin 80 - Creatinine 0 1",CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateTime.UtcNow,DateOfService = DateTime.UtcNow,EvaluationId = 324356,ExpirationDate = DateTime.UtcNow,FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"}
            ,new() {CKDId = 2,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,CKDAnswer = "Albumin 80 - Creatinine 0 1",CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateTime.UtcNow,DateOfService = new DateTime(2021, 11, 1),EvaluationId = 324357,ExpirationDate = DateTime.UtcNow,FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"}
            ,new() {CKDId = 3,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,CKDAnswer = "Albumin 80 - Creatinine 0 1",CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateTime.UtcNow,DateOfService = DateTime.UtcNow,EvaluationId = 324358,ExpirationDate = DateTime.UtcNow,FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"}
        };

        var ckdfrcmbilling = new List<CKDRCMBilling>
        {
            new CKDRCMBilling {Id = 1, BillId = "1", CreatedDateTime = DateTimeOffset.Now}
        };

        var lookupAnswers = new List<LookupCKDAnswer>
        {
            new LookupCKDAnswer{ CKDAnswerId =20964,CKDAnswerValue="answer dummay1"}
        };

        if (!Context.CKD.Any())
        {
            Context.CKD.AddRange(ckd);
            Context.CKDStatusCode.AddRange(CKDStatusCode.All);

            Context.CKDStatus.AddRange(
                new CKDStatus { CKD = ckd.First(each => each.CKDId == 1), CKDStatusCode = CKDStatusCode.CKDPerformed },
                new CKDStatus { CKD = ckd.First(each => each.CKDId == 2), CKDStatusCode = CKDStatusCode.CKDPerformed }
            );
            Context.SaveChanges();
        }

        if (!Context.LookupCKDAnswer.Any())
        {
            Context.LookupCKDAnswer.AddRange(lookupAnswers);
            Context.SaveChanges();
        }

        if (!Context.CKDRCMBilling.Any())
        {
            Context.CKDRCMBilling.AddRange(ckdfrcmbilling);
            Context.SaveChanges();
        }
    }

    private DbContextOptions<CKDDataContext> GetDbOptions()
    {
        return new DbContextOptionsBuilder<CKDDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    public void Dispose()
    {
        Context.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return Context.DisposeAsync();
    }
}