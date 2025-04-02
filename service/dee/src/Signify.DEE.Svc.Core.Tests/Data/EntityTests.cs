using FluentAssertions;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Data;

public class EntityTests
{
    private static readonly FakeApplicationTime ApplicationTime = new();

    public class ExamStatusCodeTests
    {
        [Fact]
        public void Equals_ExamStatusCode_Instances_Should_Be_Equal()
        {
            var examStatusCode1 = ExamStatusCode.Performed;
            var examStatusCode2 = ExamStatusCode.Performed;
            GenericEqualityTests.TestEqualObjects(examStatusCode1, examStatusCode2);
        }
    }

    public class LocationTests
    {
        [Fact]
        public void Equals_Location_Instances_Should_Be_Equal()
        {
            var entity1 = CreateLocation();
            var entity2 = CreateLocation();
            GenericEqualityTests.TestEqualObjects(entity1, entity2);
        }

        private Location CreateLocation()
        {
            return new Location(10.00, 8.10);
        }
    }

    public class ProductTests
    {
        [Fact]
        public void Equals_Product_Instances_Should_Be_Equal()
        {
            var entity1 = CreateProduct();
            var entity2 = CreateProduct();
            GenericEqualityTests.TestEqualObjects(entity1, entity2);
        }

        private Product CreateProduct()
        {
            return new Product("DEE");
        }
    }

    public class MemberModelTest
    {
        [Fact]
        public void Equals_MemberModel_Instances_Should_Be_Equal()
        {
            var entity1 = CreateMember();
            var entity2 = CreateMember();
            GenericEqualityTests.TestEqualObjects(entity1, entity2);
        }

        private static MemberModel CreateMember()
        {
            return new MemberModel { MemberPlanId = 1, City = "NY", FirstName = "Sam", Client = "Test" };
        }

        public class ProviderModelTest
        {
            [Fact]
            public void Equals_ProviderModel_Instances_Should_Be_Equal()
            {
                var entity1 = CreateProvider();
                var entity2 = CreateProvider();
                GenericEqualityTests.TestEqualObjects(entity1, entity2);
            }

            private static ProviderModel CreateProvider()
            {
                return new ProviderModel { FirstName = "Sam", LastName = "Tarley", NationalProviderIdentifier = "US", ProviderId = 5 };
            }
        }

        public class IrisPatientModelTest
        {
            [Fact]
            public void Equals_IrisPatientModel_Instances_Should_Be_Equal()
            {
                var entity1 = CreateIrisPatient();
                var entity2 = CreateIrisPatient();
                GenericEqualityTests.TestEqualObjects(entity1, entity2);
            }

            private static IrisPatientModel CreateIrisPatient()
            {
                return new IrisPatientModel { FirstName = "Sam", LastName = "Tarley", PatientId = 1, Gender = "Male" };
            }
        }

        public class ExamStatusModelTest
        {
            [Fact]
            public void Equals_ExamStatusModel_Instances_Should_Be_Equal()
            {
                var entity1 = CreateExamStatus();
                var entity2 = CreateExamStatus();
                GenericEqualityTests.TestEqualObjects(entity1, entity2);
            }

            private static ExamStatusModel CreateExamStatus()
            {
                return new ExamStatusModel { ExamId = 1, Status = "Performed" };
            }
        }

        public class EventRequestModelTests
        {
            [Fact]
            public void Equals_EventRequest_Instances_Should_Be_Equal()
            {
                var entity1 = CreateEventRequest();
                var entity2 = CreateEventRequest();
                GenericEqualityTests.TestEqualObjects(entity1, entity2);
            }

            private static EventRequestModel CreateEventRequest()
            {
                return new EventRequestModel { Start = ApplicationTime.UtcNow(), End = ApplicationTime.UtcNow().AddYears(1) };
            }
        }

        public class ExamGraderModelTests
        {
            [Fact]
            public void Equals_ExamGrader_Instances_Should_Be_Equal()
            {
                var entity1 = CreateExamGrader();
                var entity2 = CreateExamGrader();
                GenericEqualityTests.TestEqualObjects(entity1, entity2);
            }

            private static ExamGraderModel CreateExamGrader()
            {
                return new ExamGraderModel { FirstName = "Sam", LastName = "Tarley", NPI = "DEE", Taxonomy = "SomeTa" };
            }
        }

        public class ExamImageModelTests
        {
            [Fact]
            public void Equals_ExamImage_Instances_Should_Be_Equal()
            {
                var entity1 = CreateExamImage();
                var entity2 = CreateExamImage();
                GenericEqualityTests.TestEqualObjects(entity1, entity2);
            }

            private static ExamImageModel CreateExamImage()
            {
                return new ExamImageModel { ExamId = 1, ImageId = 21, ImageQuality = "High", ImageType = "Jpeg", Laterality = "Lateral", Gradable = true, NotGradableReasons = default };
            }
        }

        public class EvaluationObjectiveTests
        {
            [Theory]
            [InlineData("Comprehensive")]
            [InlineData("COMPREHENSIVE")]
            [InlineData("ComPreHenSive")]
            public void ComprehensiveObjective_ReturnsDEEBillingProductCode(string comp)
            {
                var billingCode = EvaluationObjective.GetProductBillingCode(comp);
                billingCode.Should().Be("DEE");
            }

            [Theory]
            [InlineData("Focused")]
            [InlineData("FOCUSED")]
            [InlineData("foCusED")]
            public void FocusedObjective_ReturnsDEEDVFBillingProductCode(string focused)
            {
                var billingCode = EvaluationObjective.GetProductBillingCode(focused);
                billingCode.Should().Be("DEE-DFV");
            }
        }
    }
}