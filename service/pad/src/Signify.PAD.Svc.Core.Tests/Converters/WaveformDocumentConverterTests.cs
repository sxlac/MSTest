using FakeItEasy;
using Signify.PAD.Svc.Core.Converter;
using Signify.PAD.Svc.Core.Validators;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Converters;

public class WaveformDocumentConverterTests
{
    private readonly IStringValidator _strValidator = A.Fake<StringValidator>();

    private WaveformDocumentConverter CreateSubject()
        => new(_strValidator);

    [Theory]
    [InlineData("WALKER_122940330_PAD_BL_080122.PDF", 122940330)]
    [InlineData("WAYNE_132940331_PAD_BL_090222.PDF", 132940331)]
    [InlineData("KENT_142940332_PAD_BL_100322.PDF", 142940332)]
    [InlineData("WALKER_JR_122940330_PAD_BL_080122.PDF", 122940330)]
    [InlineData("_WALKER_162940330_PAD_BL_080122.PDF", 162940330)]
    [InlineData("_WALKER_172940330__PAD_BL_080122.PDF", 172940330)]
    [InlineData("WALKER_192940330_PAD_BL_080122.PDF_", 192940330)]
    [InlineData("WALKER_1929_PAD_BL_080122.PDF_", 1929)]
    [InlineData("MODE_-3_PAD_BL_021423.PDF", -3)]
    [InlineData("MODE_-3_PAD_BL_022823.PDF", -3)]
    [InlineData("MODE_-2_PAD_BL_030123.PDF", -2)]
    [InlineData("MODE_-2_PAD_BL_030223.PDF", -2)]
    public void Convert_MemberPlanId_ValidFilename(string filename, int expectedResult)
    {
        var subject = CreateSubject();

        var actual = subject.ConvertMemberPlanId(filename);

        Assert.Equal(expectedResult, actual);
    }

    [Theory]
    [InlineData("WALKER_B0H_PAD_BL_080122.PDF")]
    [InlineData("WAYNE_MARTHA_PAD_BL_090222.PDF")]
    [InlineData("KENT_C_PAD_BL_100322.PDF")]
    [InlineData("WALKER_19.2940330__PAD_BL_080122.PDF")]
    public void Convert_MemberPlanId_InvalidMemberPlanId(string filename)
    {
        var subject = CreateSubject();

        Assert.Throws<InvalidOperationException>(() => subject.ConvertMemberPlanId(filename));
    }

    [Theory]
    [InlineData("WALKER_122940330_PAD_BL_080122.PDF", "2022-8-1")]
    [InlineData("WAYNE_132940331_PAD_BL_090222.PDF", "2022-9-2")]
    [InlineData("KENT_142940332_PAD_BL_100322.PDF", "2022-10-3")]
    [InlineData("KENT_142940332_PAD_BL_100322.pdf", "2022-10-3")]
    public void Convert_DateOfExam_ValidFilename(string filename, DateTime expectedResult)
    {
        var subject = CreateSubject();

        var actual = subject.ConvertDateOfExam(filename);

        Assert.Equal(expectedResult, actual);
    }

    [Theory]
    [InlineData("WALKER_122940330_PAD_BL_08012022.PDF")]
    [InlineData("WAYNE_132940331_PAD_BL_310222.PDF")]
    [InlineData("KENT_142940332_PAD_BL_222222.PDF")]
    [InlineData("KENT_152940332_PAD_BL_222222PDF")]
    [InlineData("KENT_162940332_PAD_BL_22222.PDF")]
    [InlineData("KENT_172940332_PAD_BL_222222..PDF")]
    [InlineData("KENT_172940332_PAD_BL_222222.TXT")]
    [InlineData("KENT_172940332_PAD_BL_222222.txt")]
    [InlineData("KENT_172940332_PAD_BL_100322abc.PDF")]
    [InlineData("KENT_172940332_PAD_BL_100322abc.pdf")]
    [InlineData("KENT_172940332_PAD_BL_100322.abc.pdf")]
    public void Convert_DateOfExam_InvalidFilename_Date(string filename)
    {
        var subject = CreateSubject();

        Assert.Throws<InvalidOperationException>(() => subject.ConvertDateOfExam(filename));
    }
}