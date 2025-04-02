using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Parsers;
using System.Collections.Generic;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Parsers;

public class ResultsParserTests
{
    [Theory]
    [MemberData(nameof(Parse_Test_Data))]
    public void Parse_Tests(string rawValue, ResultsModel expectedResult)
    {
        var subject = new ResultsParser();

        var actual = subject.Parse(rawValue);

        Assert.Equal(expectedResult.RawValue, actual.RawValue);
        Assert.Equal(expectedResult.ParsedValue, actual.ParsedValue);
        Assert.Equal(expectedResult.ValueRange, actual.ValueRange);
        Assert.Equal(expectedResult.Normality, actual.Normality);
        Assert.Equal(expectedResult.Exception, actual.Exception);
    }

    public static IEnumerable<object[]> Parse_Test_Data()
    {
        #region "<4" and ">13"
        yield return
        [
            "<4", new ResultsModel
            {
                RawValue = "<4",
                ParsedValue = 4,
                ValueRange = ResultValueRange.LessThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        yield return
        [
            "<4.0", new ResultsModel
            {
                RawValue = "<4.0",
                ParsedValue = 4,
                ValueRange = ResultValueRange.LessThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        yield return
        [
            " <4    ", new ResultsModel
            {
                RawValue = " <4    ",
                ParsedValue = 4,
                ValueRange = ResultValueRange.LessThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];

        yield return
        [
            "<4%", new ResultsModel
            {
                RawValue = "<4%",
                ParsedValue = 4,
                ValueRange = ResultValueRange.LessThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        yield return
        [
            "<4 %", new ResultsModel
            {
                RawValue = "<4 %",
                ParsedValue = 4,
                ValueRange = ResultValueRange.LessThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        yield return
        [
            ">13", new ResultsModel
            {
                RawValue = ">13",
                ParsedValue = 13,
                ValueRange = ResultValueRange.GreaterThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        yield return
        [
            "> 13", new ResultsModel
            {
                RawValue = "> 13",
                ParsedValue = 13,
                ValueRange = ResultValueRange.GreaterThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        yield return
        [
            " > 13.0  ", new ResultsModel
            {
                RawValue = " > 13.0  ",
                ParsedValue = 13,
                ValueRange = ResultValueRange.GreaterThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        yield return
        [
            ">13.0", new ResultsModel
            {
                RawValue = ">13.0",
                ParsedValue = 13,
                ValueRange = ResultValueRange.GreaterThan,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        #endregion "<4" and ">13"

        #region Malformed
        yield return
        [
            "<1", new ResultsModel
            {
                RawValue = "<1",
                ParsedValue = null,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Undetermined,
                Exception = "Result malformed"
            }
        ];
        yield return
        [
            "<4.00", new ResultsModel
            {
                RawValue = "<4.00",
                ParsedValue = null,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Undetermined,
                Exception = "Result malformed"
            }
        ];
        yield return
        [
            ">13.00", new ResultsModel
            {
                RawValue = ">13.00",
                ParsedValue = null,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Undetermined,
                Exception = "Result malformed"
            }
        ];

        yield return
        [
            "<<4", new ResultsModel
            {
                RawValue = "<<4",
                ParsedValue = null,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Undetermined,
                Exception = "Result malformed"
            }
        ];

        yield return
        [
            null, new ResultsModel
            {
                RawValue = null,
                ParsedValue = null,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Undetermined,
                Exception = "Result malformed"
            }
        ];

        yield return
        [
            "", new ResultsModel
            {
                RawValue = "",
                ParsedValue = null,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Undetermined,
                Exception = "Result malformed"
            }
        ];

        yield return
        [
            "abc", new ResultsModel
            {
                RawValue = "abc",
                ParsedValue = null,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Undetermined,
                Exception = "Result malformed"
            }
        ];
        #endregion Malformed

        #region Out of Range
        yield return
        [
            "-5", new ResultsModel
            {
                RawValue = "-5",
                ParsedValue = -5,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Abnormal,
                Exception = "Result out of range (low)"
            }
        ];

        yield return
        [
            "-1", new ResultsModel
            {
                RawValue = "-1",
                ParsedValue = -1,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Abnormal,
                Exception = "Result out of range (low)"
            }
        ];

        yield return
        [
            "0", new ResultsModel
            {
                RawValue = "0",
                ParsedValue = 0,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Abnormal,
                Exception = "Result out of range (low)"
            }
        ];

        yield return
        [
            "1", new ResultsModel
            {
                RawValue = "1",
                ParsedValue = 1,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Abnormal,
                Exception = "Result out of range (low)"
            }
        ];

        yield return
        [
            "20", new ResultsModel
            {
                RawValue = "20",
                ParsedValue = 20,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Abnormal,
                Exception = "Result out of range (high)"
            }
        ];
        #endregion Out of Range

        #region Valid Abnormal
        yield return
        [
            "7", new ResultsModel
            {
                RawValue = "7",
                ParsedValue = 7,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];

        yield return
        [
            "7.1", new ResultsModel
            {
                RawValue = "7.1",
                ParsedValue = 7.1m,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];

        yield return
        [
            "10", new ResultsModel
            {
                RawValue = "10",
                ParsedValue = 10,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Abnormal,
                Exception = null
            }
        ];
        #endregion Valid Abnormal

        #region Normal
        yield return
        [
            "4", new ResultsModel
            {
                RawValue = "4",
                ParsedValue = 4,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Normal,
                Exception = null
            }
        ];

        yield return
        [
            "4.1", new ResultsModel
            {
                RawValue = "4.1",
                ParsedValue = 4.1m,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Normal,
                Exception = null
            }
        ];

        yield return
        [
            "4.10", new ResultsModel
            {
                RawValue = "4.10",
                ParsedValue = 4.1m,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Normal,
                Exception = null
            }
        ];

        yield return
        [
            "04.0", new ResultsModel
            {
                RawValue = "04.0",
                ParsedValue = 4,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Normal,
                Exception = null
            }
        ];

        yield return
        [
            "4%", new ResultsModel
            {
                RawValue = "4%",
                ParsedValue = 4,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Normal,
                Exception = null
            }
        ];

        yield return
        [
            "5.5", new ResultsModel
            {
                RawValue = "5.5",
                ParsedValue = 5.5m,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Normal,
                Exception = null
            }
        ];

        yield return
        [
            "6.9", new ResultsModel
            {
                RawValue = "6.9",
                ParsedValue = 6.9m,
                ValueRange = ResultValueRange.Exactly,
                Normality = Normality.Normal,
                Exception = null
            }
        ];
        #endregion Normal
    }
}