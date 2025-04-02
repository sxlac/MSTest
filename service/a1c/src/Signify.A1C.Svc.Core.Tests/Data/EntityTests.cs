using System;
using Signify.A1C.Svc.Core.Data.Entities;
using Signify.A1C.Svc.Core.Events;
using Signify.A1C.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Data
{
    public class EntityTests
    {
        public class A1CTests
        {
            [Fact]
            public void Equals_A1C_Instances_Should_Be_Equal()
            {
                var datetime = DateTimeOffset.UtcNow;
                var A1C1 = CreateA1C(datetime);
                var A1C2 = CreateA1C(datetime);
                GenericEqualityTests.TestEqualObjects(A1C1, A1C2);
            }
        }

        public class A1CStatusTests
        {
            [Fact]
            public void Equals_A1CStatus_Instances_Should_Be_Equal()
            {
                var datetime = DateTimeOffset.UtcNow;
                var A1C1 = CreateA1C(datetime);
                var A1CStatus1 = CreateA1CStatus(A1C1);
                var A1CStatus2 = CreateA1CStatus(A1C1);
                GenericEqualityTests.TestEqualObjects(A1CStatus1, A1CStatus2);
            }

            private A1CStatus CreateA1CStatus(Core.Data.Entities.A1C A1C)
            {
                return new A1CStatus()
                { A1CStatusCode = A1CStatusCode.A1CPerformed, A1C = A1C, CreatedDateTime = A1C.CreatedDateTime };
            }
        }

        public class A1CStatusCodeTests
        {
            [Fact]
            public void Equals_A1CStatusCode_Instances_Should_Be_Equal()
            {
                var A1CStatusCode1 = A1CStatusCode.A1CPerformed;
                var A1CStatusCode2 = A1CStatusCode.A1CPerformed;
                GenericEqualityTests.TestEqualObjects(A1CStatusCode1, A1CStatusCode2);
            }
        }

        public static Core.Data.Entities.A1C CreateA1C(DateTimeOffset datetime)
        {
            return new Core.Data.Entities.A1C() { A1CId = 4, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr", ApplicationId = "Signify.Evaluation.Service", AppointmentId = 1000084715, CenseoId = "Adarsh1234", City = "Mysuru", ClientId = 14, CreatedDateTime = datetime, DateOfBirth = datetime.UtcDateTime, DateOfService = datetime.UtcDateTime, EvaluationId = 324356, FirstName = "Adarsh", LastName = "H R", MemberId = 11990396, MemberPlanId = 21074285, NationalProviderIdentifier = "9230239051", ProviderId = 42879, ReceivedDateTime = datetime.UtcDateTime, State = "Karnataka", UserName = "vastest1", ZipCode = "12345" };
        }

        public class EvaluationFinalizedEventTests
        {
            [Fact]
            public void Equals_EvaluationFinalizedEvent_Instances_Should_Be_Equal()
            {
                var datetime = DateTimeOffset.UtcNow;
                var id = new Guid();
                var event1 = CreateEvaluationFinalizedEvent(id, datetime);
                var event2 = CreateEvaluationFinalizedEvent(id, datetime);
                GenericEqualityTests.TestEqualObjects(event1, event2);
            }

            private EvaluationFinalizedEvent CreateEvaluationFinalizedEvent(Guid id, DateTimeOffset datetime)
            {
                return new EvaluationFinalizedEvent()
                { Id = id, CreatedDateTime = datetime, ReceivedDateTime = datetime.UtcDateTime };
            }
        }

        public class ProductTests
        {
            [Fact]
            public void Equals_Product_Instances_Should_Be_Equal()
            {
                var code = "ProductC1";
                var product1 = CreateProduct(code);
                var product2 = CreateProduct(code);
                GenericEqualityTests.TestEqualObjects(product1, product2);
            }

            private Product CreateProduct(string productCode)
            {
                return new Product(productCode);
            }
        }

        public class LocationTests
        {
            [Fact]
            public void Equals_Location_Instances_Should_Be_Equal()
            {
                 var location1 = CreateLocation();
                var location2 = CreateLocation();
                GenericEqualityTests.TestEqualObjects(location1, location2);
            }

            private Location CreateLocation()
            {
                double lat = 10.1678;
                double lon = 7.1678;
                return new Location(lat, lon);
            }
        }

        public class SagaResultTests
        {
            [Fact]
            public void Equals_Result_Instances_Should_Be_Equal()
            {
                var result1 = CreateResult(string.Empty);
                var result2 = CreateResult(string.Empty);
                GenericEqualityTests.TestEqualObjects(result1, result2);
            }

            private Core.Sagas.Result CreateResult(string msg)
            {
                return new Core.Sagas.Result() { IsSuccess = true, ErrorMessage = msg };
            }
        }
    }
}
