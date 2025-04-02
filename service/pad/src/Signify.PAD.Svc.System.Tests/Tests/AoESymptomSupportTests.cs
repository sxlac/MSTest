using Signify.EvaluationsApi.Core.Values;
using Signify.PAD.Svc.System.Tests.Core.Constants;
using Signify.PAD.Svc.System.Tests.Core.Models.Kafka;

namespace Signify.PAD.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]

public class AoESymptomSupportTests : PerformedActions
{
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetAoETestData))]
    public async Task ANC_T1055_AoESymptom(Dictionary<int, string> answersDict, bool hasClinicalSupport, bool hasSymptomsForAoe, int lateralityCodeId, int pedalpulseCodeId)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();


        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
        // Database PAD table
        var pad = await getAoESymptomSupportResultsByEvaluationId(evaluation.EvaluationId, 15, 3);
        pad.LeftScoreAnswerValue.Should().Be(answersDict[Answers.LeftResultAnswerId]);
        pad.RightScoreAnswerValue.Should().Be(answersDict[Answers.RightResultAnswerId]);
        pad.FootPainRestingElevatedLateralityCodeId.Should().Be(lateralityCodeId);
        pad.PedalPulseCodeId.Should().Be(pedalpulseCodeId);
        pad.HasClinicalSupportForAoeWithRestingLegPain.Should().Be(hasClinicalSupport);
        pad.HasSymptomsForAoeWithRestingLegPain.Should().Be(hasSymptomsForAoe);
        pad.ReasonAoeWithRestingLegPainNotConfirmed.Should().Be(answersDict[Answers.ReasonAoEWithRestingLegPainNotConfirmed]);
        
        pad.FootPainDisappearsWalkingOrDangling
            .Should()
            .Be(answersDict.ContainsKey(Answers.footpaindisappearbywalkingTrue)?true:false);
        pad.FootPainDisappearsOtc
            .Should()
            .Be(answersDict.ContainsKey(Answers.footpaindisappearbyotcTrue)?true:false);
        pad.AoeWithRestingLegPainConfirmed
            .Should()
            .Be(answersDict.ContainsKey(Answers.AoeWithRestingLegPainConfirmedTrue)?true:false);
        
        // Validate AoEResult message published to pad_clinical_support Kafka topic
         var aoeResult = await CoreKafkaActions.GetAoEResultEvent<GetAoEResultEvent>(evaluation.EvaluationId, "AoeResult");

         aoeResult.EvaluationId.Should().Be(evaluation.EvaluationId);
         aoeResult.ProductCode.Should().Be(Product);
         // aoeResult.ReceivedDate.Date.Should().Be(pad.CreatedDateTime.Date);
         aoeResult.ClinicalSupport[0].SupportType.Should().Be("PainInLegs");
         var legsPainValues = new Dictionary<int, string>
         {
             { Answers.footpainrestingLeft, "Left" },
             { Answers.footpainrestingRight, "Right" },
             { Answers.footpainrestingBoth, "Both" },
             { Answers.footpainrestingNo, "Neither" }
         };
         foreach (var leg in legsPainValues.Keys)
         {
             if (answersDict.ContainsKey(leg))
             {
                 aoeResult.ClinicalSupport[0].SupportValue.Should()
                     .Be(legsPainValues[leg]);
             }
         } 

         aoeResult.ClinicalSupport[1].SupportType.Should().Be("FootPainDisappearsWalkingOrDangling");
         aoeResult.ClinicalSupport[1].SupportValue
             .Should()
             .Be(answersDict.ContainsKey(Answers.footpaindisappearbywalkingTrue)? "true" : "false");
         
         aoeResult.ClinicalSupport[2].SupportType.Should().Be("FootPainDisappearsWithMeds");
         aoeResult.ClinicalSupport[2].SupportValue
             .Should()
             .Be(answersDict.ContainsKey(Answers.footpaindisappearbyotcTrue)? "true" : "false");
         
         aoeResult.ClinicalSupport[3].SupportType.Should().Be("PedalPulseCode");
         var pedalPulseValues = new Dictionary<int, string>
         {
             { Answers.pedalpulseAbNormalLeft, "Abnormal-Left" },
             { Answers.pedalpulseAbNormalRight, "Abnormal-Right" },
             { Answers.pedalpulseAbNormalBoth, "Abnormal-Bilateral" },
             { Answers.pedalpulseNormal, "Normal" },
             { Answers.pedalpulseNotPerformed, "Not Performed" }
         };
         foreach (var code in pedalPulseValues.Keys)
         {
             if (answersDict.ContainsKey(code))
             {
                 aoeResult.ClinicalSupport[3].SupportValue.Should()
                     .Be(pedalPulseValues[code]);
             }
         }
         
         aoeResult.ClinicalSupport[4].SupportType.Should().Be("HasSymptomsForAoeWithRestingLegPain");
         aoeResult.ClinicalSupport[4].SupportValue.Should().BeEquivalentTo(pad.HasSymptomsForAoeWithRestingLegPain.ToString());
         aoeResult.ClinicalSupport[5].SupportType.Should().Be("HasClinicalSupportForAoeWithRestingLegPain");
         aoeResult.ClinicalSupport[5].SupportValue.Should().BeEquivalentTo(pad.HasClinicalSupportForAoeWithRestingLegPain.ToString());

         aoeResult.ClinicalSupport[6].SupportType.Should().Be("AoeWithRestingLegPainConfirmed");
         aoeResult.ClinicalSupport[6].SupportValue
             .Should()
             .Be(answersDict.ContainsKey(Answers.AoeWithRestingLegPainConfirmedTrue)? "true" : "false");
         
         aoeResult.ClinicalSupport[7].SupportType.Should().Be("ReasonAoeWithRestingLegPainNotConfirmed");
         aoeResult.ClinicalSupport[7].SupportValue.Should().Be(pad.ReasonAoeWithRestingLegPainNotConfirmed);
    }
    private static IEnumerable<object[]> GetAoETestData
    {
         get
            {
                return new[]
                {
                    new object[]
                    {
                        new Dictionary<int, string> 
                        { 
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.LeftResultAnswerId, "0" },
                            { Answers.RightResultAnswerId, "1.4" },
                            { Answers.footpainrestingRight, "Right" },
                            { Answers.pedalpulseAbNormalLeft, "Abnormal-Left" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.footpaindisappearbywalkingTrue, "true" },
                            { Answers.footpaindisappearbyotcFalse, "false" },
                            { Answers.AoeWithRestingLegPainConfirmedTrue, "true" },
                            { Answers.ReasonAoEWithRestingLegPainNotConfirmed, "" }
                        },
                            Answers.hasClinicalSupport, 
                            Answers.hasSymptomsForAoe, 
                            Answers.lateralityCodeId=3, 
                            Answers.pedalpulseCodeId=2 
                    },
                    [
                        new Dictionary<int, string> 
                        { 
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.LeftResultAnswerId, "0.9" },
                            { Answers.RightResultAnswerId, "0.59" },
                            { Answers.footpainrestingLeft, "Left" },
                            { Answers.pedalpulseAbNormalRight, "Abnormal-Right" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.footpaindisappearbywalkingTrue, "true" },
                            { Answers.footpaindisappearbyotcFalse, "false" },
                            { Answers.AoeWithRestingLegPainConfirmedTrue, "true" },
                            { Answers.ReasonAoEWithRestingLegPainNotConfirmed, "" }
                        },
                            Answers.hasClinicalSupport, 
                            Answers.hasSymptomsForAoe, 
                            Answers.lateralityCodeId=2, 
                            Answers.pedalpulseCodeId=3 
                    ],
                    [
                        new Dictionary<int, string> 
                        { 
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.LeftResultAnswerId, "0.9" },
                            { Answers.RightResultAnswerId, "0.59" },
                            { Answers.footpainrestingBoth, "Both" },
                            { Answers.pedalpulseAbNormalBoth, "Abnormal-Bilateral" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.footpaindisappearbywalkingTrue, "true" },
                            { Answers.footpaindisappearbyotcFalse, "false" },
                            { Answers.AoeWithRestingLegPainConfirmedTrue, "true" },
                            { Answers.ReasonAoEWithRestingLegPainNotConfirmed, "" }
                        },
                            Answers.hasClinicalSupport, 
                            Answers.hasSymptomsForAoe, 
                            Answers.lateralityCodeId=4, 
                            Answers.pedalpulseCodeId=4 
                    ],
                    [
                        new Dictionary<int, string> 
                        { 
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.LeftResultAnswerId, "0" },
                            { Answers.RightResultAnswerId, "0.99" },
                            { Answers.footpainrestingNo, "No" },
                            { Answers.pedalpulseAbNormalRight, "Abnormal-Right" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.footpaindisappearbywalkingTrue, "true" },
                            { Answers.footpaindisappearbyotcFalse, "false" },
                            { Answers.AoeWithRestingLegPainConfirmedTrue, "true" },
                            { Answers.ReasonAoEWithRestingLegPainNotConfirmed, "" }
                        },
                            Answers.hasClinicalSupport = false, 
                            Answers.hasSymptomsForAoe = false,
                            Answers.lateralityCodeId=1, 
                            Answers.pedalpulseCodeId=3 
                    ],
                    [
                        new Dictionary<int, string> 
                        { 
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.LeftResultAnswerId, "0" },
                            { Answers.RightResultAnswerId, "0.99" },
                            { Answers.footpainrestingRight, "Right" },
                            { Answers.pedalpulseNormal, "Normal" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.footpaindisappearbywalkingTrue, "true" },
                            { Answers.footpaindisappearbyotcFalse, "false" },
                            { Answers.AoeWithRestingLegPainConfirmedTrue, "true" },
                            { Answers.ReasonAoEWithRestingLegPainNotConfirmed, "" }
                        },
                            Answers.hasClinicalSupport = false, 
                            Answers.hasSymptomsForAoe = false,
                            Answers.lateralityCodeId=3, 
                            Answers.pedalpulseCodeId=1 
                    ],
                    [
                        new Dictionary<int, string> 
                        { 
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.LeftResultAnswerId, "0" },
                            { Answers.RightResultAnswerId, "0.99" },
                            { Answers.footpainrestingRight, "Right" },
                            { Answers.pedalpulseNotPerformed, "Not Performed" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.footpaindisappearbywalkingTrue, "true" },
                            { Answers.footpaindisappearbyotcFalse, "false" },
                            { Answers.AoeWithRestingLegPainConfirmedTrue, "true" },
                            { Answers.ReasonAoEWithRestingLegPainNotConfirmed, "" }
                        },
                            Answers.hasClinicalSupport = false, 
                            Answers.hasSymptomsForAoe = false,
                            Answers.lateralityCodeId=3, 
                            Answers.pedalpulseCodeId=5 
                    ],
                    [
                        new Dictionary<int, string> 
                        { 
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.LeftResultAnswerId, "0.89" },
                            { Answers.RightResultAnswerId, "1.4" },
                            { Answers.footpainrestingBoth, "Both" },
                            { Answers.pedalpulseAbNormalBoth, "Abnormal-Bilateral" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.footpaindisappearbywalkingTrue, "true" },
                            { Answers.footpaindisappearbyotcFalse, "false" },
                            { Answers.AoeWithRestingLegPainConfirmedFalse, "false" },
                            { Answers.ReasonAoEWithRestingLegPainNotConfirmed, "testing note" }
                        },
                            Answers.hasClinicalSupport = false, 
                            Answers.hasSymptomsForAoe = true, 
                            Answers.lateralityCodeId=4, 
                            Answers.pedalpulseCodeId=4 
                    ]
                };
            }
    }

    
    
    
    
    
    
    
    
    
}