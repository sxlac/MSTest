using System;
using System.Collections.Generic;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using Signify.uACR.Core.Builders;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Models;
using Xunit;

namespace Signify.uACR.Core.Tests.Builders;

public class ExamModelBuilderTests
{
        private const long EvaluationId = 1;
        private const int FormVersionId = 900;
        

        //KED test performed question/answer ids
        private const int KedTestPerformedQuestionId = 100562;
        private const int KedTestPerformedAnswerYesId = 52456;
        private const int KedTestPerformedAnswerNoId = 52457;
        
        //KED reason not performed question/answer ids
        private const int KedReasonNotPerformedQuestionId = 100565;
        private const int KedReasonNotPerformedAnswerProviderId = 52461;
        private const int KedReasonNotPerformedAnswerMemberId = 52462;
        
        //KED reason provider unable question/answer ids
        private const int KedReasonProviderUnableToPerformQuestionId = 100566;
        private const int KedTechnicalIssueAnswerId = 52463;
        private const int KedEnvironmentalIssueAnswerId = 52464;
        private const int KedNoSuppliesOrEquipmentAnswerId = 52465;
        private const int KedInsufficientTrainingAnswerId = 52466;
        private const int KedMemberPhysicallyUnableAnswerId = 52940;
        
        //KED reason member refused question/answer ids
        private const int KedReasonMemberRefusedQuestionId = 100567;
        private const int KedScheduledToCompleteAnswerId = 52467;
        private const int KedMemberApprehensionAnswerId = 52468;
        private const int KedNotInterestedAnswerId = 52469;
        private const int KedMemberRecentlyCompletedAnswerId = 52941;
        
        //Test performed question/answer ids
        private const int TestPerformedQuestionId = 100563;
        private const int TestPerformedAnswerYesId = 52458;
        private const int TestPerformedAnswerNoId = 52459;
        
        //Reason not performed question/answer ids
        private const int ReasonNotPerformedQuestionId = 100568;
        private const int ReasonNotPerformedAnswerProviderId = 52470;
        private const int ReasonNotPerformedAnswerMemberId = 52471;

        //Reason provider unable question/answer ids
        private const int ReasonProviderUnableToPerformQuestionId = 100569;
        private const int TechnicalIssueAnswerId = 52472;
        private const int EnvironmentalIssueAnswerId = 52473;
        private const int NoSuppliesOrEquipmentAnswerId = 52474;
        private const int InsufficientTrainingAnswerId = 52475;
        private const int MemberPhysicallyUnableAnswerId = 52942;
        
        //Reason member refused question/answer ids
        private const int ReasonMemberRefusedQuestionId = 100570;
        private const int ScheduledToCompleteAnswerId = 52476;
        private const int MemberApprehensionAnswerId = 52477;
        private const int NotInterestedAnswerId = 52478;
        private const int MemberRecentlyCompletedAnswerId = 52943;
        
        //LetsGetChecked barcode values
        private const int KedAlphaQuestionId = 100573;
        private const int KedNumericalQuestionId = 100574;

        private static readonly ExamModelBuilder SingletonInstance = new(A.Dummy<ILogger<ExamModelBuilder>>());

        [Fact]
        public void ForEvaluation_ReturnsNewInstance()
        {
            var singletonInstance = SingletonInstance;

            var newInstance = singletonInstance.ForEvaluation(EvaluationId);

            Assert.NotEqual(singletonInstance, newInstance);
        }

        [Fact]
        public void WithAnswers_ReturnsNewInstance()
        {
            var singletonInstance = SingletonInstance;

            var newInstance = singletonInstance.WithAnswers([]);

            Assert.NotEqual(singletonInstance, newInstance);
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public void ForEvaluation_ValidatesRange(int evaluationId, bool isValid)
        {
            if (isValid)
            {
                Assert.NotNull(SingletonInstance.ForEvaluation(evaluationId));
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => SingletonInstance.ForEvaluation(evaluationId));
            }
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public void ForFormVersion_ValidatesRange(int formVersionId, bool isValid)
        {
            if (isValid)
            {
                Assert.NotNull(SingletonInstance.ForEvaluation(formVersionId));
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => SingletonInstance.ForEvaluation(formVersionId));
            }
        }
        
        [Fact]
        public void Build_WithoutAllRequiredInput_ThrowInvalidOpEx()
        {
            void AssertThrows(Action<ExamModelBuilder> action)
            {
                Assert.Throws<InvalidOperationException>(() => action(SingletonInstance));
            }

            AssertThrows(b => b
                .Build());
            AssertThrows(b => b
                .ForEvaluation(EvaluationId)
                .Build());
            AssertThrows(b => b
                .WithFormVersion(FormVersionId)
                .Build());
            AssertThrows(b => b
                .WithAnswers([])
                .Build());
        }

        [Fact]
        public void Build_WithoutTestPerformedQuestion_Throws()
        {
            Assert.Throws<RequiredEvaluationQuestionMissingException>(() =>
            {
                SingletonInstance
                    .ForEvaluation(EvaluationId)
                    .WithFormVersion(FormVersionId)
                    .WithAnswers([])
                    .Build();
            });
        }

        #region Exam Performed

        [Fact]
        public void Build_WithExamPerformedAnswers_ValidatesAllRequiredAnswers()
        {
            // These are in the order they are parsed
            var answers = new List<EvaluationAnswerModel>
            {
                new()
                {
                    QuestionId = KedTestPerformedQuestionId,
                    AnswerId = KedTestPerformedAnswerYesId
                },
                new()
                {
                    QuestionId = TestPerformedQuestionId,
                    AnswerId = TestPerformedAnswerYesId
                }
            };
                
            Assert.Throws<BarcodeNotFoundException>(() =>
            {
                SingletonInstance
                    .ForEvaluation(EvaluationId)
                    .WithFormVersion(FormVersionId)
                    .WithAnswers(answers)
                    .Build();
            });
        }

      

        [Theory]
        [MemberData(nameof(Build_WithExamPerformedAnswers_Barcodes_Correct_TestData))]
        public void Build_WithExamPerformedAnswers_Both_Barcodes_Correct_Test(IEnumerable<EvaluationAnswerModel> answers,
            ExamModel expectedResult)
        {
            var actual = SingletonInstance
                .ForEvaluation(EvaluationId)
                .WithFormVersion(FormVersionId)
                .WithAnswers(answers)
                .Build();

            Assert.Equal(expectedResult.ExamResult.Barcode, actual.ExamResult.Barcode);
        }
        
        public static IEnumerable<object[]> Build_WithExamPerformedAnswers_Barcodes_Correct_TestData()
        {
            
            const string alphaBarcode = "ABCEGF";
            const string numericalCode = "LGC-0000-0000-0000";

            var answers = new List<EvaluationAnswerModel>
            {
                KedTestPerformed,
                TestPerformed,
                new()
                {
                    QuestionId = KedAlphaQuestionId,
                    AnswerValue = alphaBarcode
                },
                new()
                {
                    QuestionId = KedNumericalQuestionId,
                    AnswerValue = numericalCode
                }
            };

            var expected = new ExamModel(EvaluationId,
                new RawExamResult
                {
                    EvaluationId = EvaluationId,
                    Barcode = numericalCode + "|" + alphaBarcode,
                });

            yield return [answers, expected];
        }
    
        [Fact]
        public void Build_WithExamPerformedAnswers_Both_Barcodes_Blank_Test()
        {
            //Barcodes set as empty string
            const string alphaBarcode = "";
            const string numericalBarcode = "";
            var answers = new List<EvaluationAnswerModel>
            {
                KedTestPerformed,
                TestPerformed,
                new()
                {
                    QuestionId = KedAlphaQuestionId,
                    AnswerValue = alphaBarcode
                },
                new()
                {
                    QuestionId = KedNumericalQuestionId,
                    AnswerValue = numericalBarcode
                }
            };
            
            Assert.Throws<BarcodeNotFoundException>(() =>
            {
                SingletonInstance
                    .ForEvaluation(EvaluationId)
                    .WithFormVersion(FormVersionId)
                    .WithAnswers(answers)
                    .Build();
            });
        }
    
        [Fact]
        public void Build_WithExamPerformedAnswers_Alpha_Barcode_Blank_Test()
        {
            //Alpha barcode blank
            const string alphaBarcode = "";
            const string numericalBarcode = "LGC-0000-0000-0000";
            var answers = new List<EvaluationAnswerModel>
            {
                KedTestPerformed,
                TestPerformed,
                new()
                {
                    QuestionId = KedAlphaQuestionId,
                    AnswerValue = alphaBarcode
                },
                new()
                {
                    QuestionId = KedNumericalQuestionId,
                    AnswerValue = numericalBarcode
                }
            };
            Assert.Throws<BarcodeNotFoundException>(() =>
            {
                SingletonInstance
                    .ForEvaluation(EvaluationId)
                    .WithFormVersion(FormVersionId)
                    .WithAnswers(answers)
                    .Build();
            });
        }
        [Fact]
        public void Build_WithExamPerformedAnswers_Alpha_Barcode_Invalid_Test()
        {
            //Alpha barcode invalid
            const string numericalBarcode = "LGC-0000-0000-0000";
            const string alphaBarcode  = "ABCDE";
            var answers = new List<EvaluationAnswerModel>
            {
                KedTestPerformed,
                TestPerformed,
                new()
                {
                    QuestionId = KedAlphaQuestionId,
                    AnswerValue = alphaBarcode
                },
                new()
                {
                    QuestionId = KedNumericalQuestionId,
                    AnswerValue = numericalBarcode
                }
            };
            
            var actual = SingletonInstance
                .ForEvaluation(EvaluationId)
                .WithFormVersion(FormVersionId)
                .WithAnswers(answers)
                .Build();
            
            Assert.Equal(numericalBarcode + "|" + alphaBarcode, actual.ExamResult.Barcode);
        }

        [Fact]
        public void Build_WithExamPerformedAnswers_Numeric_Barcode_Blank_Test()
        {
            //Numeric barcode blank
            const string alphaBarcode = "ABCDEF";
            const string numericalBarcode = "";
            var answers = new List<EvaluationAnswerModel>
            {
                KedTestPerformed,
                TestPerformed,
                new()
                {
                    QuestionId = KedAlphaQuestionId,
                    AnswerValue = alphaBarcode
                },
                new()
                {
                    QuestionId = KedNumericalQuestionId,
                    AnswerValue = numericalBarcode
                }
            };
            
            Assert.Throws<BarcodeNotFoundException>(() =>
            {
                SingletonInstance
                    .ForEvaluation(EvaluationId)
                    .WithFormVersion(FormVersionId)
                    .WithAnswers(answers)
                    .Build();
            });
        }
        
        [Fact]
        public void Build_WithExamPerformedAnswers_Numeric_Barcode_Invalid_Test()
        {
            //Numeric barcode invalid
            const string numericalBarcode = "LG-0000-0000-0000";
            const string alphaBarcode = "ABCDEF";
            var answers = new List<EvaluationAnswerModel>
            {
                KedTestPerformed,
                TestPerformed,
                new()
                {
                    QuestionId = KedAlphaQuestionId,
                    AnswerValue = alphaBarcode
                },
                new()
                {
                    QuestionId = KedNumericalQuestionId,
                    AnswerValue = numericalBarcode
                }
            };
            
            var actual = SingletonInstance
                .ForEvaluation(EvaluationId)
                .WithFormVersion(FormVersionId)
                .WithAnswers(answers)
                .Build();
            
            Assert.Equal(numericalBarcode + "|" + alphaBarcode, actual.ExamResult.Barcode);
        }
        
        private static readonly EvaluationAnswerModel TestPerformed = new()
        {
            QuestionId = TestPerformedQuestionId, AnswerId = TestPerformedAnswerYesId
        };
        private static readonly EvaluationAnswerModel KedTestPerformed = new()
        {
            QuestionId = KedTestPerformedQuestionId, AnswerId = KedTestPerformedAnswerYesId
        };
        #endregion Exam Performed

        #region Exam Not Performed

        [Theory]
        [MemberData(nameof(Build_WithExamNotPerformedAnswers_ValidatesAllRequiredAnswers_TestData))]
        public void Build_WithExamNotPerformedAnswers_ValidatesAllRequiredAnswers(
            IEnumerable<EvaluationAnswerModel> answers, int? expectedMissingQuestionId)
        {
            try
            {
                SingletonInstance
                    .ForEvaluation(EvaluationId)
                    .WithAnswers(answers)
                    .Build();

                // If there's an expected missing Q, it would have thrown and won't get here
                Assert.Null(expectedMissingQuestionId);
            }
            catch (RequiredEvaluationQuestionMissingException ex)
            {
                Assert.Equal(expectedMissingQuestionId, ex.QuestionId);
            }
            catch
            {
                // Ensures no other type of exception was thrown
                Assert.False(true);
            }
        }

        public static IEnumerable<object[]> Build_WithExamNotPerformedAnswers_ValidatesAllRequiredAnswers_TestData()
        {
            yield return
            [
                new[]
                {
                    new EvaluationAnswerModel
                    {
                        QuestionId = KedTestPerformedQuestionId,
                        AnswerId = KedTestPerformedAnswerYesId
                    }
                },
                TestPerformedQuestionId
            ];

            yield return
            [
                new[]
                {
                    new EvaluationAnswerModel
                    {
                        QuestionId = KedTestPerformedQuestionId,
                        AnswerId = KedTestPerformedAnswerYesId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = TestPerformedQuestionId,
                        AnswerId = TestPerformedAnswerNoId
                    }
                },
                ReasonNotPerformedQuestionId
            ];

            yield return
            [
                new[]
                {
                    new EvaluationAnswerModel
                    {
                        QuestionId = KedTestPerformedQuestionId,
                        AnswerId = KedTestPerformedAnswerYesId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = TestPerformedQuestionId,
                        AnswerId = TestPerformedAnswerNoId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = ReasonNotPerformedQuestionId,
                        AnswerId = ReasonNotPerformedAnswerMemberId
                    }
                },
                ReasonMemberRefusedQuestionId
            ];

            yield return
            [
                new[]
                {
                    new EvaluationAnswerModel
                    {
                        QuestionId = KedTestPerformedQuestionId,
                        AnswerId = KedTestPerformedAnswerYesId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = TestPerformedQuestionId,
                        AnswerId = TestPerformedAnswerNoId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = ReasonNotPerformedQuestionId,
                        AnswerId = ReasonNotPerformedAnswerProviderId
                    }
                },
                ReasonProviderUnableToPerformQuestionId
            ];

            // Validate that if all required answers are supplied, no exception is thrown
            yield return
            [
                new[]
                {
                    new EvaluationAnswerModel
                    {
                        QuestionId = KedTestPerformedQuestionId,
                        AnswerId = KedTestPerformedAnswerYesId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = TestPerformedQuestionId,
                        AnswerId = TestPerformedAnswerNoId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = ReasonNotPerformedQuestionId,
                        AnswerId = ReasonNotPerformedAnswerMemberId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = ReasonMemberRefusedQuestionId,
                        AnswerId = NotInterestedAnswerId
                    }
                },
                null // No missing questions; should validate successfully
            ];

            // Validate that if all required answers are supplied, no exception is thrown
            yield return
            [
                new[]
                {
                    new EvaluationAnswerModel
                    {
                        QuestionId = KedTestPerformedQuestionId,
                        AnswerId = KedTestPerformedAnswerYesId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = TestPerformedQuestionId,
                        AnswerId = TestPerformedAnswerNoId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = ReasonNotPerformedQuestionId,
                        AnswerId = ReasonNotPerformedAnswerProviderId
                    },
                    new EvaluationAnswerModel
                    {
                        QuestionId = ReasonProviderUnableToPerformQuestionId,
                        AnswerId = TechnicalIssueAnswerId
                    }
                },
                null // No missing questions; should validate successfully
            ];
        }

                [Theory]
        [MemberData(nameof(Build_WithKEDExamNotPerformedAnswers_Tests_TestData))]
        public void Build_WithKEDExamNotPerformedAnswers_Tests(IEnumerable<EvaluationAnswerModel> answers,
            ExamModel expectedResult)
        {
            var actual = SingletonInstance
                .ForEvaluation(EvaluationId)
                .WithAnswers(answers)
                .Build();

            Assert.Equal(expectedResult.EvaluationId, actual.EvaluationId);
            Assert.Equal(expectedResult.NotPerformedReason, actual.NotPerformedReason);
            Assert.Equal(expectedResult.ExamPerformed, actual.ExamPerformed);
        }
        
        public static IEnumerable<object[]> Build_WithKEDExamNotPerformedAnswers_Tests_TestData()
        {
            var answers = new List<EvaluationAnswerModel>
            {
                KedTestNotPerformed,
                KedMemberRefused,
                new()
                {
                    QuestionId = KedReasonMemberRefusedQuestionId,
                    AnswerId = KedScheduledToCompleteAnswerId
                }
            };

            var expected = new ExamModel(EvaluationId, NotPerformedReason.ScheduledToComplete, null);

            yield return [answers, expected];

            answers =
            [
                KedTestNotPerformed,
                KedMemberRefused,
                new()
                {
                    QuestionId = KedReasonMemberRefusedQuestionId,
                    AnswerId = KedMemberApprehensionAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.MemberApprehension, null);

            yield return [answers, expected];

            answers =
            [
                KedTestNotPerformed,
                KedMemberRefused,
                new()
                {
                    QuestionId = KedReasonMemberRefusedQuestionId,
                    AnswerId = KedNotInterestedAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.NotInterested, null);

            yield return [answers, expected];
            
            answers =
            [
                KedTestNotPerformed,
                KedMemberRefused,
                new()
                {
                    QuestionId = KedReasonMemberRefusedQuestionId,
                    AnswerId = KedMemberRecentlyCompletedAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.MemberRecentlyCompleted, null);

            yield return [answers, expected];

            answers =
            [
                KedTestNotPerformed,
                KedUnableToPerform,
                new()
                {
                    QuestionId = KedReasonProviderUnableToPerformQuestionId,
                    AnswerId = KedTechnicalIssueAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.TechnicalIssue, null);

            yield return [answers, expected];

            answers =
            [
                KedTestNotPerformed,
                KedUnableToPerform,
                new()
                {
                    QuestionId = KedReasonProviderUnableToPerformQuestionId,
                    AnswerId = KedEnvironmentalIssueAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.EnvironmentalIssue, null);

            yield return [answers, expected];

            answers =
            [
                KedTestNotPerformed,
                KedUnableToPerform,
                new()
                {
                    QuestionId = KedReasonProviderUnableToPerformQuestionId,
                    AnswerId = KedNoSuppliesOrEquipmentAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.NoSuppliesOrEquipment, null);

            yield return [answers, expected];

            answers =
            [
                KedTestNotPerformed,
                KedUnableToPerform,
                new()
                {
                    QuestionId = KedReasonProviderUnableToPerformQuestionId,
                    AnswerId = KedInsufficientTrainingAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.InsufficientTraining, null);

            yield return [answers, expected];
            
            answers =
            [
                KedTestNotPerformed,
                KedUnableToPerform,
                new()
                {
                    QuestionId = KedReasonProviderUnableToPerformQuestionId,
                    AnswerId = KedMemberPhysicallyUnableAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.MemberPhysicallyUnable, null);

            yield return [answers, expected];
            
        }
        
        private static readonly EvaluationAnswerModel KedTestNotPerformed = new()
        {
            QuestionId = KedTestPerformedQuestionId, AnswerId = KedTestPerformedAnswerNoId
        };

        private static readonly EvaluationAnswerModel KedMemberRefused = new()
        {
            QuestionId = KedReasonNotPerformedQuestionId, AnswerId = KedReasonNotPerformedAnswerMemberId
        };

        private static readonly EvaluationAnswerModel KedUnableToPerform = new()
        {
            QuestionId = KedReasonNotPerformedQuestionId, AnswerId = KedReasonNotPerformedAnswerProviderId
        };
        
        [Theory]
        [MemberData(nameof(Build_WithExamNotPerformedAnswers_Tests_TestData))]
        public void Build_WithExamNotPerformedAnswers_Tests(IEnumerable<EvaluationAnswerModel> answers,
            ExamModel expectedResult)
        {
            var actual = SingletonInstance
                .ForEvaluation(EvaluationId)
                .WithAnswers(answers)
                .Build();

            Assert.Equal(expectedResult.EvaluationId, actual.EvaluationId);
            Assert.Equal(expectedResult.NotPerformedReason, actual.NotPerformedReason);
            Assert.Equal(expectedResult.ExamPerformed, actual.ExamPerformed);
        }
        
        public static IEnumerable<object[]> Build_WithExamNotPerformedAnswers_Tests_TestData()
        {
            var answers = new List<EvaluationAnswerModel>
            {
                KedTestPerformed,
                TestNotPerformed,
                MemberRefused,
                new()
                {
                    QuestionId = ReasonMemberRefusedQuestionId,
                    AnswerId = ScheduledToCompleteAnswerId
                }
            };

            var expected = new ExamModel(EvaluationId, NotPerformedReason.ScheduledToComplete, null);

            yield return [answers, expected];

            answers =
            [
                KedTestPerformed,
                TestNotPerformed,
                MemberRefused,
                new()
                {
                    QuestionId = ReasonMemberRefusedQuestionId,
                    AnswerId = MemberApprehensionAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.MemberApprehension, null);

            yield return [answers, expected];

            answers =
            [
                KedTestPerformed,
                TestNotPerformed,
                MemberRefused,
                new()
                {
                    QuestionId = ReasonMemberRefusedQuestionId,
                    AnswerId = NotInterestedAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.NotInterested, null);

            yield return [answers, expected];

            answers =
            [
                KedTestPerformed,
                TestNotPerformed,
                MemberRefused,
                new()
                {
                    QuestionId = ReasonMemberRefusedQuestionId,
                    AnswerId = MemberRecentlyCompletedAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.MemberRecentlyCompleted, null);

            yield return [answers, expected];
            
            answers =
            [
                KedTestPerformed,
                TestNotPerformed,
                UnableToPerform,
                new()
                {
                    QuestionId = ReasonProviderUnableToPerformQuestionId,
                    AnswerId = TechnicalIssueAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.TechnicalIssue, null);

            yield return [answers, expected];

            answers =
            [
                KedTestPerformed,
                TestNotPerformed,
                UnableToPerform,
                new()
                {
                    QuestionId = ReasonProviderUnableToPerformQuestionId,
                    AnswerId = EnvironmentalIssueAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.EnvironmentalIssue, null);

            yield return [answers, expected];

            answers =
            [
                KedTestPerformed,
                TestNotPerformed,
                UnableToPerform,
                new()
                {
                    QuestionId = ReasonProviderUnableToPerformQuestionId,
                    AnswerId = NoSuppliesOrEquipmentAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.NoSuppliesOrEquipment, null);

            yield return [answers, expected];

            answers =
            [
                KedTestPerformed,
                TestNotPerformed,
                UnableToPerform,
                new()
                {
                    QuestionId = ReasonProviderUnableToPerformQuestionId,
                    AnswerId = InsufficientTrainingAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.InsufficientTraining, null);

            yield return [answers, expected];
            
            answers =
            [
                KedTestPerformed,
                TestNotPerformed,
                UnableToPerform,
                new()
                {
                    QuestionId = ReasonProviderUnableToPerformQuestionId,
                    AnswerId = MemberPhysicallyUnableAnswerId
                }
            ];

            expected = new ExamModel(EvaluationId, NotPerformedReason.MemberPhysicallyUnable, null);

            yield return [answers, expected];
            
        }

        private static readonly EvaluationAnswerModel TestNotPerformed = new()
        {
            QuestionId = TestPerformedQuestionId, AnswerId = TestPerformedAnswerNoId
        };

        private static readonly EvaluationAnswerModel MemberRefused = new()
        {
            QuestionId = ReasonNotPerformedQuestionId, AnswerId = ReasonNotPerformedAnswerMemberId
        };

        private static readonly EvaluationAnswerModel UnableToPerform = new()
        {
            QuestionId = ReasonNotPerformedQuestionId, AnswerId = ReasonNotPerformedAnswerProviderId
        };
        
        #endregion Exam Not Performed
    } 