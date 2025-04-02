using FluentAssertions;
using Iris.Public.Types.Models;
using Signify.DEE.Svc.Core.Messages.Services;
using System.Collections.Generic;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Services;

public class DetermineGradableTest
{
    [Theory]
    [MemberData(nameof(Handle_Gradings_TestData))]
    public void Determine_Gradability_Check_Scenarios(ResultGrading gradings, bool expectedgradability)
    {
        //Arrange
        var request = new DetermineGradability();
        //Act
        var actual = request.IsGradable(gradings);
        //Assert
        actual.Should().Be(expectedgradability);
    }

    public static IEnumerable<object[]> Handle_Gradings_TestData()
    {
        static ResultGrading CreateGrading(bool hasLeftEyeFinding, bool hasRightEyeFinding)
        {
            var leftEyeFindingList = new List<ResultFinding>();
            var rightEyeFindingList = new List<ResultFinding>();

            if (hasLeftEyeFinding) leftEyeFindingList.Add(new ResultFinding() { Finding = "Diabetic Retinopathy", Result = "None" });
            if (hasRightEyeFinding) rightEyeFindingList.Add(new ResultFinding() { Finding = "Diabetic Retinopathy", Result = "None" });

            var gradings = new ResultGrading()
            {
                OD = new ResultEyeSideGrading
                {
                    Findings = leftEyeFindingList
                },
                OS = new ResultEyeSideGrading
                {
                    Findings = rightEyeFindingList
                }
            };

            return gradings;
        }

        yield return new object[]
        {
            CreateGrading(true,true),true
        };
        yield return new object[]
        {
            CreateGrading(true,false),true
        };
        yield return new object[]
        {
            CreateGrading(false,true),true
        };
        yield return new object[]
        {
            CreateGrading(false,false),false
        };
    }
}