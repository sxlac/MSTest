using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Data;

public class EntityTests
{
    public class  CKDTests
    {
        [Fact]
        public void Equals_CKD_Instances_Should_Be_Equal()
        {
            var datetime = DateTimeOffset.UtcNow;
            var ckd1 = CreateCKD(datetime, 4);
            var ckd2 = CreateCKD(datetime, 4);
            GenericEqualityTests.TestEqualObjects(ckd1, ckd2);
        }
    }

    public class CKDStatusCodeTests
    {
        [Fact]
        public void Equals_CKDStatusCode_Instances_Should_Be_Equal()
        {
            var ckdStatusCode1 = CKDStatusCode.CKDPerformed;
            var ckdStatusCode2 = CKDStatusCode.CKDPerformed;
            GenericEqualityTests.TestEqualObjects(ckdStatusCode1, ckdStatusCode2);
        }
    }

    private static Core.Data.Entities.CKD CreateCKD(DateTimeOffset datetime, int ckdId)
    {
        return new Core.Data.Entities.CKD() { CKDId = ckdId, AddressLineOne = "4420 Harpers Ferry Dr", AddressLineTwo = "Harpers Ferry Dr", ApplicationId = "Signify.Evaluation.Service", AppointmentId = 1000084715, CKDAnswer = "Albumin 80 - Creatinine 0 1", CenseoId = "Adarsh1234", City = "Mysuru", ClientId = 14, CreatedDateTime = datetime, DateOfBirth = datetime.UtcDateTime, DateOfService = datetime.UtcDateTime, EvaluationId = 324356, ExpirationDate = datetime.UtcDateTime, FirstName = "Adarsh", LastName = "H R", MemberId = 11990396, MemberPlanId = 21074285, NationalProviderIdentifier = "9230239051", ProviderId = 42879, ReceivedDateTime = datetime.UtcDateTime, State = "Karnataka", UserName = "vastest1", ZipCode = "12345" };
    }
}