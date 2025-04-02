using Iris.Public.Grading;
using Iris.Public.Types.Constants;
using Iris.Public.Types.Models;
using Iris.Public.Types.Models.Public._2._3._1;
using Signify.DEE.Svc.System.Tests.Core.Constants;
using Signify.Dps.Test.Utilities.CoreApi.Actions;
using Signify.Dps.Test.Utilities.Kafka.Actions;
using Signify.EvaluationsApi.Core.Models;
using static Signify.DEE.Svc.System.Tests.Core.Constants.TestConstants;

namespace Signify.DEE.Svc.System.Tests.Core.Actions;

public class BaseTestActions : DatabaseActions
{
    protected readonly CoreApiActions CoreApiActions  = new(CoreApiConfigs, Provider.ProviderId, Product, FormVersionId, LoggingHttpMessageHandler);
    public static CoreKafkaActions CoreKafkaActions;
    
    protected static Dictionary<int, string> GeneratePerformedAnswers()
        => new()
        {
            {Answers.DeePerformedAnswerId, "1"}, // Is Ked Test Performed
            {Answers.FirstNameAnswerId, "1"}, // Patient Firstname
            {Answers.LastNameAnswerId, "barCode"}, // Patient Lastname
            {Answers.GenderAnswerId, "M"}, // Patient Gender
            {Answers.StateAnswerId, "TX"}, // State
            {Answers.DosAnswerId, DateTime.Now.ToString("O")} // Date of Service
            
        };

    protected static List<EvaluationAnswer> GetEvaluationAnswerListWithImages(Dictionary<int, string> answersDict)
    {
        var image1 = File.ReadAllText("../../../../Signify.DEE.Svc.System.Tests.Core/Data/Images/normalEval/image-1.txt");
        var image2 = File.ReadAllText("../../../../Signify.DEE.Svc.System.Tests.Core/Data/Images/normalEval/image-2.txt");

        var answersList = answersDict
            .Select(i => new EvaluationAnswer { AnswerId = i.Key, AnswerValue = i.Value }).ToList();

        answersList.Add(new EvaluationAnswer { AnswerId = Answers.ImageAnswerId, AnswerValue = image1, AnswerRowId = new Guid("5AA52E97-D999-4093-BF1B-7AE171C2DFBC") });
        answersList.Add(new EvaluationAnswer { AnswerId = Answers.ImageAnswerId, AnswerValue = image2, AnswerRowId = new Guid("B5C78B69-1A5C-40F6-B53A-306F0E1A54C6") });

        return answersList;
    }
    
    protected GradingRequest CreateGradingRequest(string examLocalId)
    {
        var gradingRequest = new GradingRequest
        {
            ClientGuid = "",
            LocalId = examLocalId,
            Site = new RequestSite
            {
                LocalId = ""
            },
            OS = new ResultEyeSideGrading
            {
                Findings = new List<ResultFinding>
                {
                    new()
                    {
                        Finding = GradingConstants.FindingConstants.DiabeticRetinopathy,
                        Result = GradingConstants.ResultConstants.None
                    },
                    new()
                    {
                        Finding= GradingConstants.FindingConstants.MacularEdema,
                        Result= GradingConstants.ResultConstants.None
                    },
                    new()
                    {				
                        Finding= GradingConstants.FindingConstants.WetAMD,					
                        Result= GradingConstants.ResultConstants.NoObservable
                    },
                    new()
                    {
                        Finding =GradingConstants.FindingConstants.DryAMD,
                        Result= GradingConstants.ResultConstants.NoObservable
                    }
                }
            },
            OD = new ResultEyeSideGrading
            {
                Findings = new List<ResultFinding>
                {
                    new()
                    {
                        Finding = GradingConstants.FindingConstants.DiabeticRetinopathy,
                        Result = GradingConstants.ResultConstants.Mild
                    },
                    new()
                    {
                        Finding= GradingConstants.FindingConstants.MacularEdema,
                        Result= GradingConstants.ResultConstants.None
                    },
                    new()
                    {
                        Finding= GradingConstants.FindingConstants.WetAMD,
                        Result = GradingConstants.ResultConstants.NoObservable
                    },
                    new()
                    {
                        Finding = GradingConstants.FindingConstants.DryAMD,
                        Result = GradingConstants.ResultConstants.NoObservable
                    }
                }
            }
        };

        return gradingRequest;
    }
    
    protected GradingRequest CreateUngradableGradingRequest(string examLocalId)
    {
        var gradingRequest = new GradingRequest
        {
            ClientGuid = "",
            LocalId = examLocalId,
            Site = new RequestSite
            {
                LocalId = ""
            },
            OS = new ResultEyeSideGrading
            {
                Findings = new List<ResultFinding>
                {
                    new()
                    {
                        Finding = GradingConstants.FindingConstants.NotGradable,
                    }
                },
                UngradableReasons = new List<string> { GradingConstants.UngradableConstants.NoViewtoRetina }
            },
            OD = new ResultEyeSideGrading
            {
                Findings = new List<ResultFinding>
                {
                    new()
                    {
                        Finding = GradingConstants.FindingConstants.NotGradable,
                    }
                },
                UngradableReasons = new List<string> { GradingConstants.UngradableConstants.ImagenotofaRetina }
            }
        };

        return gradingRequest;
    }
    
    public async Task SubmitGrading(string examLocalId)
    {
        var submissionService = new GradingSubmissionService("");
        var gradingRequest = CreateGradingRequest(examLocalId); 
        
        await submissionService.SubmitRequest(gradingRequest);

    }
    
}