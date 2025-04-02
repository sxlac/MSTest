using Signify.PAD.Svc.Core.BusinessRules;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Maps;
using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Maps;

public class ResultsReceivedConverterTests
{
    [Fact]
    public void Convert_ReturnsSameInstanceAsUpdated()
    {
        var destination = new ResultsReceived
        {
            EvaluationId = 1
        };

        var subject = new ResultsReceivedConverter(new BillAndPayRules());

        var actual = subject.Convert(new EvaluationAnswers(), destination, null);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_WithNullDestination_ReturnsNotNull()
    {
        var subject = new ResultsReceivedConverter(new BillAndPayRules());

        var actual = subject.Convert(new EvaluationAnswers(), null, null);

        Assert.NotNull(actual);
    }

    [Theory]
    [MemberData(nameof(Convert_Tests_Data))]
    public void Convert_Tests(EvaluationAnswers answers, ResultsReceived expected)
    {
        var subject = new ResultsReceivedConverter(new BillAndPayRules());

        var actual = subject.Convert(answers, null, null);

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> Convert_Tests_Data()
    {
        yield return
        [
            new EvaluationAnswers
            {
                LeftScore = "1.02m",
                LeftNormalityIndicator = "N",
                LeftSeverity = "Normal",
                RightScore = null,
                RightNormalityIndicator = "U",
                RightException = "Result value malformed"
            },
            new ResultsReceived
            {
                Determination = "N",
                IsBillable = true,
                Results = new List<SideResultInfo>
                {
                    new()
                    {
                        Side = "L",
                        Result = "1.02m",
                        AbnormalIndicator = "N",
                        Severity = "Normal",
                    },
                    new()
                    {
                        Side = "R",
                        AbnormalIndicator = "U",
                        Exception = "Result value malformed"
                    }
                }
            }
        ];

        yield return
        [
            new EvaluationAnswers
            {
                LeftScore = null,
                LeftNormalityIndicator = "U",
                LeftException = "Result not supplied",
                RightScore = null,
                RightNormalityIndicator = "U",
                RightException = "Result value malformed"
            },
            new ResultsReceived
            {
                Determination = "U",
                IsBillable = false,
                Results = new List<SideResultInfo>
                {
                    new()
                    {
                        Side = "L",
                        AbnormalIndicator = "U",
                        Exception = "Result not supplied"
                    },
                    new()
                    {
                        Side = "R",
                        AbnormalIndicator = "U",
                        Exception = "Result value malformed"
                    }
                }
            }
        ];

        yield return
        [
            new EvaluationAnswers
            {
                LeftScore = "1.02m",
                LeftNormalityIndicator = "N",
                LeftSeverity = "Normal",
                RightScore = "0.95m",
                RightNormalityIndicator = "N",
                RightSeverity = "Borderline",
            },
            new ResultsReceived
            {
                Determination = "N",
                IsBillable = true,
                Results = new List<SideResultInfo>
                {
                    new()
                    {
                        Side = "L",
                        Result = "1.02m",
                        AbnormalIndicator = "N",
                        Severity = "Normal"
                    },
                    new()
                    {
                        Side = "R",
                        Result = "0.95m",
                        AbnormalIndicator = "N",
                        Severity = "Borderline"
                    }
                }
            }
        ];

        yield return
        [
            new EvaluationAnswers
            {
                LeftScore = "1.02m",
                LeftNormalityIndicator = "N",
                LeftSeverity = "Normal",
                RightScore = "0.50m",
                RightNormalityIndicator = "A",
                RightSeverity = "Moderate",
            },
            new ResultsReceived
            {
                Determination = "A",
                IsBillable = true,
                Results = new List<SideResultInfo>
                {
                    new()
                    {
                        Side = "L",
                        Result = "1.02m",
                        AbnormalIndicator = "N",
                        Severity = "Normal"
                    },
                    new()
                    {
                        Side = "R",
                        Result = "0.50m",
                        AbnormalIndicator = "A",
                        Severity = "Moderate"
                    }
                }
            }
        ];

        yield return
        [
            new EvaluationAnswers
            {
                LeftNormalityIndicator = "U",
                LeftException = "Result not supplied",
                RightScore = "0.50m",
                RightNormalityIndicator = "A",
                RightSeverity = "Moderate",
            },
            new ResultsReceived
            {
                Determination = "A",
                IsBillable = true,
                Results = new List<SideResultInfo>
                {
                    new()
                    {
                        Side = "L",
                        AbnormalIndicator = "U",
                        Exception = "Result not supplied"
                    },
                    new()
                    {
                        Side = "R",
                        Result = "0.50m",
                        AbnormalIndicator = "A",
                        Severity = "Moderate"
                    }
                }
            }
        ];

        yield return
        [
            new EvaluationAnswers
            {
                LeftScore = "500",
                LeftNormalityIndicator = "U",
                LeftException = "Result value out of range",
                RightScore = null,
                RightNormalityIndicator = "U",
                RightException = "Result value malformed"
            },
            new ResultsReceived
            {
                Determination = "U",
                IsBillable = false,
                Results = new List<SideResultInfo>
                {
                    new()
                    {
                        Side = "L",
                        Result = "500",
                        AbnormalIndicator = "U",
                        Exception = "Result value out of range"
                    },
                    new()
                    {
                        Side = "R",
                        AbnormalIndicator = "U",
                        Exception = "Result value malformed"
                    }
                }
            }
        ];
    }
}