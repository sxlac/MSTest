using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Signify.A1C.Svc.Core.Data;
using Signify.A1C.Svc.Core.Queries;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Queries
{
    public class QueryA1CTests : IClassFixture<MockA1CDBFixture>
    {
        private readonly MockA1CDBFixture _moA1CbFixture;

        public QueryA1CTests(MockA1CDBFixture moA1CbFixture)
        {
            _moA1CbFixture = moA1CbFixture;
        }

        private DbContextOptions<A1CDataContext> GetDbOptions(string testQueryA1C)
        {
            return new DbContextOptionsBuilder<A1CDataContext>()
                .UseInMemoryDatabase(databaseName: testQueryA1C)
                .Options;
        }

        [Theory]
        [InlineData("1239", 1234)]
        public async Task QueryA1C_ReturnsOneResult_Successful_BarcodeExists(string barcode, int appointmentId)
        {
            //Arrange
            //var dboptions = GetDbOptions("QueryA1CTestBarCode");
            //PopulateFakeData(dboptions);
            var query = new QueryA1C() { Barcode = barcode, AppointmentId = appointmentId };

            //using var context = new A1CDataContext(dboptions);
            //var handler = new QueryA1CHandler(context);
            var handler = new QueryA1CHandler(_moA1CbFixture.Context);


            //Act
            var result = await handler.Handle(query, CancellationToken.None);

            //Assert
            Assert.NotNull(result);

            result.Status.Equals(QueryA1CStatus.BarcodeExists);
        }

        [Theory]
        [InlineData("12349", 12344)]
        public async Task QueryA1C_Returns_NotFound(string barcode, int appointmentId)
        {
            //Arrange
            //var dboptions = GetDbOptions("QueryA1CTest");
            //PopulateFakeData(dboptions);
            var query = new QueryA1C() { Barcode = barcode, AppointmentId = appointmentId };

            //using var context = new A1CDataContext(dboptions);
            //var handler = new QueryA1CHandler(context);
            var handler = new QueryA1CHandler(_moA1CbFixture.Context);

            //Act
            var result = await handler.Handle(query, CancellationToken.None);

            //Assert
            Assert.NotNull(result);

            result.Status.Equals(QueryA1CStatus.NotFound);
        }


        //private void PopulateFakeData(DbContextOptions<A1CDataContext> options)
        //{
        //    using var context = new A1CDataContext(options);
        //    context.A1C.Add(new Core.Data.Entities.A1C(1222, 1234, 1235, 1236, "test", 1237, 1238, DateTime.Now, DateTime.Now, DateTime.Now, "1239", 1239, "testuser", "Signify.Services", "testuser1", "", "", null, "", "", "", "", "", ""));
        //    context.A1C.Add(new Core.Data.Entities.A1C(1223, 1234, 1235, 1236, "test", 1237, 1238, DateTime.Now, DateTime.Now, DateTime.Now, "1239", 1239, "testuser", "Signify.Services", "testuser1", "", "", null, "", "", "", "", "", ""));
        //    context.A1C.Add(new Core.Data.Entities.A1C(1224, 1234, 1235, 1236, "test", 1237, 1238, DateTime.Now, DateTime.Now, DateTime.Now, "1239", 1239, "testuser", "Signify.Services", "testuser1", "", "", null, "", "", "", "", "", ""));
        //    context.SaveChanges();
        //}
    }

}

