using FakeItEasy;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Converter;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Maps;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Maps;

public class WaveformDocumentMapperTests
{
    [Fact]
    public void Convert_WithNullDestination_CreatesNewDestination()
    {
        var source = new ProcessPendingWaveform
        {
            Vendor = new WaveformDocumentVendor()
        };

        var subject = new WaveformDocumentMapper(A.Dummy<IWaveformDocumentConverter>());

        var actual = subject.Convert(source, null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_WithNotNullDestination_ReturnsSameObjectReference()
    {
        var source = new ProcessPendingWaveform
        {
            Vendor = new WaveformDocumentVendor()
        };

        var expectedResult = new WaveformDocument();

        var subject = new WaveformDocumentMapper(A.Dummy<IWaveformDocumentConverter>());

        var actual = subject.Convert(source, expectedResult, default);

        Assert.Same(expectedResult, actual);
    }

    [Fact]
    public void Convert_HappyPath_Test()
    {
        // Arrange
        const int memberPlanId = 1;
        const int vendorId = 2;
        const string filename = "filename";
        var dateOfExam = DateTime.UtcNow;

        var source = new ProcessPendingWaveform
        {
            Filename = filename,
            Vendor = new WaveformDocumentVendor
            {
                WaveformDocumentVendorId = vendorId
            }
        };

        var converter = A.Fake<IWaveformDocumentConverter>();

        A.CallTo(() => converter.ConvertMemberPlanId(A<string>._))
            .Returns(memberPlanId);

        A.CallTo(() => converter.ConvertDateOfExam(A<string>._))
            .Returns(dateOfExam);

        // Act
        var subject = new WaveformDocumentMapper(converter);

        var result = subject.Convert(source, default, default);

        // Assert
        A.CallTo(() => converter.ConvertMemberPlanId(A<string>.That.Matches(s => s == filename)))
            .MustHaveHappened();
        A.CallTo(() => converter.ConvertDateOfExam(A<string>.That.Matches(s => s == filename)))
            .MustHaveHappened();

        Assert.Equal(vendorId, result.WaveformDocumentVendorId);
        Assert.Equal(filename, result.Filename);
        Assert.Equal(memberPlanId, result.MemberPlanId);
        Assert.Equal(dateOfExam, result.DateOfExam);
    }
}