using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Signify.A1C.Svc.Core.Data;

namespace Signify.A1C.Svc.Core.Tests
{
    /// <summary>
    /// This fixture can be used as a shared db context or as a backbone for individual per-test db contexts.
    /// Your test class should implement IClassFixture<MoA1CbFixture> to use it.
    /// Then, in each test either:
    /// 1)  var dbContext = _dbFixture._sharedDbContext;  (to use the shared context)
    /// or
    /// 2) var dbContext = new SqlDataContext(_dbFixture.GetDbOptions("{uniquename}"));  (to use a unique context).
    /// Shared contexts are faster but you must be careful that the test data inside the context cannot mess up another test.
    /// Tests that test for specific *counts* of data records will need their own context, as it can't be guaranteed that no other test data might accidentally qualify.
    /// Be sure to use the NextId function when assigning entity ids to avoid conflicts since the automated generation of ids
    /// is not guaranteed to be unique.
    /// As such, tests of code blocks that add new data records will circumvent the NextId feature and will need their own context.  
    /// 
    /// </summary>
    public class MockA1CDBFixture : IDisposable
    {
        public A1CDataContext Context { get; set; }
        public MockA1CDBFixture()
        {
            var dbOptions = GetDbOptions();
            PopulateFakeData(dbOptions);
        }

        private void PopulateFakeData(DbContextOptions<A1CDataContext> options)
        {
            Context = new A1CDataContext(options);
            var A1C = new List<Core.Data.Entities.A1C>() {  new Core.Data.Entities.A1C() {A1CId = 1,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateTime.UtcNow,DateOfService = DateTime.UtcNow,EvaluationId = 324356,FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"}
                ,new Core.Data.Entities.A1C() {A1CId = 2,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateTime.UtcNow,DateOfService = DateTime.UtcNow,EvaluationId = 324357,FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"}
                ,new Core.Data.Entities.A1C() {A1CId = 3,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateTime.UtcNow,DateOfService = DateTime.UtcNow,EvaluationId = 324358,FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"}
                ,new Core.Data.Entities.A1C() {A1CId = 4,AddressLineOne = "4420 Harpers Ferry Dr",AddressLineTwo = "Harpers Ferry Dr",ApplicationId ="Signify.Evaluation.Service",AppointmentId = 1000084715,CenseoId = "Adarsh1234",City = "Mysuru",ClientId = 14,CreatedDateTime = DateTimeOffset.UtcNow,DateOfBirth = DateTime.UtcNow,DateOfService = new DateTime(2021,10,10),EvaluationId = 324359,FirstName = "Adarsh",LastName = "H R",MemberId = 11990396,MemberPlanId = 21074285,NationalProviderIdentifier = "9230239051",ProviderId = 42879,ReceivedDateTime = DateTime.UtcNow,State = "Karnataka",UserName = "vastest1",ZipCode = "12345"}
            };
            if (!Context.A1C.Any())
            {
                Context.A1C.AddRange(A1C);
                Context.SaveChanges();
            }
        }

        private DbContextOptions<A1CDataContext> GetDbOptions()
        {
            return new DbContextOptionsBuilder<A1CDataContext>()
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
    }
}