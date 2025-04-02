using FakeItEasy;
using Microsoft.Extensions.Logging;
using Refit;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Configs;
using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class UploadWaveformDocumentTests
{
    private const string ClientId = nameof(ClientId);

    private readonly IEvaluationApi _evaluationApi;

    public UploadWaveformDocumentTests()
    {
        _evaluationApi = A.Fake<IEvaluationApi>();
    }

    private UploadWaveformDocumentHandler CreateSubject(MockFileSystem mockFileSystem)
        => new(A.Fake<ILogger<UploadWaveformDocumentHandler>>(), new OktaConfig { ClientId = ClientId }, _evaluationApi, mockFileSystem);

    [Theory]
    [MemberData(nameof(FileStream_TestData))]
    public async Task UploadWaveformDocumentEvaluation_ValidateEntityCreation_DocumentIdMatches(string fileName, string filePath)
    {
        // Arrange
        var request = new UploadWaveformDocument(1, fileName, filePath);

        var mockFileSystem = new MockFileSystem();
        var mockInputFile = new MockFileData("Mock Data");
        mockFileSystem.AddFile(filePath, mockInputFile);
        var subject = CreateSubject(mockFileSystem);

        A.CallTo(() => _evaluationApi.GetEvaluationDocumentDetails(A<int>._, A<string>._))
            .Returns(new ApiResponse<IList<EvaluationDocumentModel>>(new HttpResponseMessage(HttpStatusCode.NoContent), default, new RefitSettings()));

        A.CallTo(() => _evaluationApi.CreateEvaluationDocument(A<int>._, A<ByteArrayPart>._, A<EvaluationRequest>._))
            .Returns(new ApiResponse<EvaluationDocumentModel>(new HttpResponseMessage(HttpStatusCode.OK), default, new RefitSettings()));

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.True(result.IsSuccess);
        A.CallTo(() => _evaluationApi.CreateEvaluationDocument(1, A<ByteArrayPart>._, A<EvaluationRequest>.That.Matches(r =>
                r.ApplicationId == "Signify.PAD.Svc" && r.DocumentType == "PadWaveform" && r.UserName == ClientId)))
            .MustHaveHappened();
    }

    [Theory]
    [MemberData(nameof(FileStream_TestData))]
    public async Task UploadWaveformDocumentEvaluation_CreatingAdditionalEvalDocument_DoesntCreateNewEvalDocument(string fileName, string filePath)
    {
        // Arrange
        var request = new UploadWaveformDocument(1, fileName, filePath);

        var mockFileSystem = new MockFileSystem();
        var mockInputFile = new MockFileData("Mock Data");
        mockFileSystem.AddFile(filePath, mockInputFile);
        var subject = CreateSubject(mockFileSystem);

        var apiResponse = new ApiResponse<IList<EvaluationDocumentModel>>(new HttpResponseMessage { ReasonPhrase = "OK" },
            new List<EvaluationDocumentModel>
            {
                new EvaluationDocumentModel
                {
                    DocumentType = "PadWaveform"
                }
            }, new RefitSettings());
        A.CallTo(() => _evaluationApi.GetEvaluationDocumentDetails(A<int>._, "PadWaveform")).Returns(apiResponse);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.False(result.IsSuccess);
        A.CallTo(() => _evaluationApi.CreateEvaluationDocument(1, A<ByteArrayPart>._, A<EvaluationRequest>.That.Matches(r =>
                r.ApplicationId == "Signify.PAD.Svc" && r.DocumentType == "PadWaveform" && r.UserName == ClientId)))
            .MustNotHaveHappened();
    }

    public static IEnumerable<object[]> FileStream_TestData()
    {
        var fileName = "WALKER_122940331_PAD_BL_080122.PDF";
        var filePath = "/temp/" + fileName;

        yield return new object[] { fileName, filePath };
    }
}