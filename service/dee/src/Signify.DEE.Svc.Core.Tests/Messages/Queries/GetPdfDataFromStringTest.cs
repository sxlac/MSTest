using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetPdfDataFromStringTests
{
    private readonly GetPdfDataFromStringHandler _handler;

    public GetPdfDataFromStringTests()
    {
        var logger = A.Fake<ILogger<GetPdfDataFromStringHandler>>();
        var config = A.Fake<IConfiguration>();
        _handler = new GetPdfDataFromStringHandler(logger, config);
    }

    /* Commenting out this test for now. Need a way to better unit test IronPdf. Till then commenting out this test.
    [Fact]
    public async Task Should_Retrieve_ByteArray_if_pdf_valid()
    {
        // Arrange
        var pdfString = ContentHelper.GetBase64PdfString();

        //Act
        var getPdfFromString = new GetPdfDataFromString() { EvaluationId = 1, PdfData = pdfString };
        var result = await _handler.Handle(getPdfFromString, CancellationToken.None);

        // Assert
        //result.Should().BeOfType<byte[]>(); -- passes individually but fails when all tests are run. Need to see how to unit test IronPdf.
    }
    */

    [Fact]
    public async Task Should_Retrieve_null_if_pdf_string_empty()
    {
        // Arrange
        var pdfString = String.Empty;
        var getPdfFromString = new GetPdfDataFromString() { EvaluationId = 1, PdfData = pdfString };

        //Act
        var result = await _handler.Handle(getPdfFromString, CancellationToken.None);
        
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_Retrieve_null_if_pdf_string_invalid()
    {
        // Arrange
        var pdfString = "Bad data";
        var getPdfFromString = new GetPdfDataFromString() { EvaluationId = 1, PdfData = pdfString };

        //Act
        var result = await _handler.Handle(getPdfFromString, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}