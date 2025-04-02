using FakeItEasy;
using FluentAssertions;
using Iris.Public.Types.Models;
using Signify.DEE.Svc.Core.Messages.Services;
using System.Collections.Generic;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Services;

public class DetermineBillableTest
{
    private readonly IDetermineGradability _determineGradability;

    public DetermineBillableTest()
    {
        _determineGradability = A.Fake<IDetermineGradability>();
    }

    [Theory]
    [MemberData(nameof(Handle_ImageDetails_TestData))]
    public void Determine_Billability_Check_Scenarios(ResultImageDetails imageDetails, bool gradabilityResult, bool expectedBillabilityResult)
    {
        //Arrange
        var request = new DetermineBillability(_determineGradability);
        var grading = new ResultGrading();
        A.CallTo(() => _determineGradability.IsGradable(grading)).Returns(gradabilityResult);
        //Act
        var isBillable = request.IsBillable(grading, imageDetails);
        //Assert
        isBillable.Should().Be(expectedBillabilityResult);
    }

    public static IEnumerable<object[]> Handle_ImageDetails_TestData()
    {
        static ResultImageDetails CreateImageDetails(int leftEyeOriginalCount, int rightEyeOriginalCount)
        {
            return new ResultImageDetails
            {
                LeftEyeOriginalCount = leftEyeOriginalCount,
                RightEyeOriginalCount = rightEyeOriginalCount
            };
        }

        yield return new object[]
        {
            CreateImageDetails(1,0), true, false
        };
        yield return new object[]
        {
            CreateImageDetails(0,1), true, false
        };
        yield return new object[]
        {
            CreateImageDetails(1,1), true, true
        };
        yield return new object[]
        {
            CreateImageDetails(0,0), true, false
        };
        yield return new object[]
        {
            CreateImageDetails(1,0), false, false
        };
        yield return new object[]
        {
            CreateImageDetails(0,1), false, false
        };
        yield return new object[]
        {
            CreateImageDetails(1,1), false, false
        };
        yield return new object[]
        {
            CreateImageDetails(0,0), false, false
        };
    }
}