using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Refit;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Configs;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class CreateResultPdfCommandHandlerTest
{
    private readonly ILogger<CreateResultPdfCommandHandler> _log;
    private readonly IEvaluationApi _evaluationApi;
    private readonly DataContext _context;
    private readonly IrisDocumentInfoConfig _config;
    private CreateResultPdfCommandHandler _handler;
    private readonly FakeApplicationTime _applicationTime = new();

    public CreateResultPdfCommandHandlerTest()
    {
        _log = A.Fake<ILogger<CreateResultPdfCommandHandler>>();
        _evaluationApi = A.Fake<IEvaluationApi>();
        _context = A.Fake<DataContext>();
        _config = A.Fake<IrisDocumentInfoConfig>();
        _handler = new CreateResultPdfCommandHandler(_log, _evaluationApi, _config, _context, _applicationTime);
    }

    [Fact]
    public async Task Should_Throw_InvalidDataException_When_Exam_Is_Null()
    {
        // Arrange
        var exam = new Exam { CreatedDateTime = _applicationTime.LocalNow(), Gradeable = true };
        var fakeDbSet = FakeDbSet(exam);
        _handler = new CreateResultPdfCommandHandler(_log, _evaluationApi, _config, _context, _applicationTime);
        var request = new CreateExamResultPdf(1, default);
        A.CallTo(() => _context.Exams).Returns(fakeDbSet);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(async () => await _handler.Handle(request, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_ArgumentNullException_When_EvaluationID_Is_Null()
    {
        // Arrange
        var exam = new Exam { ExamId = 1 };
        var fakeDbSet = FakeDbSet(exam);
        _handler = new CreateResultPdfCommandHandler(_log, _evaluationApi, _config, _context, _applicationTime);
        var request = new CreateExamResultPdf(1, default);
        A.CallTo(() => _context.Exams).Returns(fakeDbSet);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _handler.Handle(request, CancellationToken.None));
    }

    [Fact]
    public Task Should_Not_Throw_Exception_When_DeeResult_Document_Already_Exist()
    {
        // Arrange
        var exam = new Exam { ExamId = 1, EvaluationId = 12 };
        var fakeDbSet = FakeDbSet(exam);
        _handler = new CreateResultPdfCommandHandler(_log, _evaluationApi, _config, _context, _applicationTime);
        var request = new CreateExamResultPdf(1, null);
        A.CallTo(() => _context.Exams).Returns(fakeDbSet);
        A.CallTo(() => _evaluationApi.GetEvaluationDocument(A<long>._)).Returns(GetApiResponse());

        // Act
        var result = Record.ExceptionAsync(() => _handler.Handle(request, CancellationToken.None));

        //Assert
        Assert.Null(result.Exception);
        return Task.FromResult(Task.CompletedTask);
    }

    [Fact]
    public Task Should_Not_Throw_Exception_When_EvaluationDocument_Is_Created()
    {
        // Arrange
        var exam = new Exam { ExamId = 1, EvaluationId = 12 };
        var fakeDbSet = FakeDbSet(exam);
        var byt = new byte[] { 23, 32 };
        var evt = new EvaluationDocumentModel { CreatedDateTime = _applicationTime.UtcNow(), DocumentType = "Test", EvaluationId = 12 };
        var evtRs = new List<EvaluationDocumentModel> { evt };
        var rm = new HttpResponseMessage();
        var rs = new ApiResponse<List<EvaluationDocumentModel>>(rm, evtRs, new RefitSettings());
        var res = new ApiResponse<EvaluationDocumentModel>(rm, evt, new RefitSettings());
        _handler = new CreateResultPdfCommandHandler(_log, _evaluationApi, _config, _context, _applicationTime);
        var request = new CreateExamResultPdf(1, byt);
        A.CallTo(() => _context.Exams).Returns(fakeDbSet);
        A.CallTo(() => _evaluationApi.GetEvaluationDocument(A<long>._)).Returns(rs);
        A.CallTo(() => _evaluationApi.CreateEvaluationDocument(A<long>._, A<StreamPart>._, A<CreateEvaluationDocumentRequest>._)).Returns(res);

        // Act
        var result = Record.ExceptionAsync(() => _handler.Handle(request, CancellationToken.None));

        //Assert
        Assert.Null(result.Exception);
        return Task.FromResult(Task.CompletedTask);
    }

    [Fact]
    public Task ShouldThrowException_WhenEvaluationDocumentResponseIsError()
    {
        // Arrange
        var exam = new Exam { ExamId = 1, EvaluationId = 12 };
        var fakeDbSet = FakeDbSet(exam);
        var byt = new byte[] { 23, 32 };
        var evt = new EvaluationDocumentModel { CreatedDateTime = _applicationTime.UtcNow(), DocumentType = "Test", EvaluationId = 12 };
        var evtRs = new List<EvaluationDocumentModel> { evt };
        var rm = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.InternalServerError};
        var rs = new ApiResponse<List<EvaluationDocumentModel>>(rm, evtRs, new RefitSettings());
        var res = new ApiResponse<EvaluationDocumentModel>(rm, evt, new RefitSettings());
        _handler = new CreateResultPdfCommandHandler(_log, _evaluationApi, _config, _context, _applicationTime);
        var request = new CreateExamResultPdf(1, byt);
        A.CallTo(() => _context.Exams).Returns(fakeDbSet);
        A.CallTo(() => _evaluationApi.GetEvaluationDocument(A<long>._)).Returns(rs);
        A.CallTo(() => _evaluationApi.CreateEvaluationDocument(A<long>._, A<StreamPart>._, A<CreateEvaluationDocumentRequest>._)).Returns(res);

        // Act
        Task<NullReferenceException> task = Assert.ThrowsAsync<NullReferenceException>(async () => await _handler.Handle(request, CancellationToken.None));
        return task;
    }

    private static DbSet<Exam> FakeDbSet(Exam exams)
    {
        var fakeIQueryable = new List<Exam> { exams }.AsQueryable();
        var fakeDbSet = A.Fake<DbSet<Exam>>((d => d.Implements(typeof(IQueryable<Exam>))));
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).GetEnumerator()).Returns(fakeIQueryable.GetEnumerator());
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Provider).Returns(fakeIQueryable.Provider);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).Expression).Returns(fakeIQueryable.Expression);
        A.CallTo(() => ((IQueryable<Exam>)fakeDbSet).ElementType).Returns(fakeIQueryable.ElementType);
        return fakeDbSet;
    }

    /// <summary>
    /// Provides the mock response
    /// </summary>
    /// <returns></returns>
    private ApiResponse<List<EvaluationDocumentModel>> GetApiResponse()
    {
        var evtRs = new List<EvaluationDocumentModel> { new() { CreatedDateTime = _applicationTime.UtcNow(), DocumentType = "DeeResult", EvaluationId = 12 } };
        var rm = new HttpResponseMessage();
        var rs = new ApiResponse<List<EvaluationDocumentModel>>(rm, evtRs, new RefitSettings());
        return rs;
    }
}