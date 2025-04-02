using Akka.Streams.Kafka.Extensions;
using Signify.Dps.Test.Utilities.Database.Actions;
using Signify.Dps.Test.Utilities.Database.Exceptions;
using Signify.Dps.Test.Utilities.Database.Models;
using Signify.QE.Core.Utilities;
using Signify.uACR.System.Tests.Core.Exceptions;
using Signify.uACR.System.Tests.Core.Models.Database;
using NotPerformedReason = Signify.uACR.System.Tests.Core.Models.Database.NotPerformedReason;

namespace Signify.uACR.System.Tests.Core.Actions;

public class DatabaseActions
{
    private readonly PostgresHandler _postgresHandler = new($"Host={Environment.GetEnvironmentVariable("UACR_DB_HOST")};Username={Environment.GetEnvironmentVariable("UACR_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("UACR_DB_PASSWORD")};Database={Environment.GetEnvironmentVariable("SERVICE")}");
    private readonly PostgresHandler _omsPostgresHandler = new($"Host={Environment.GetEnvironmentVariable("OMS_DB_HOST")};Username={Environment.GetEnvironmentVariable("OMS_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("OMS_DB_PASSWORD")};Database=oms");
    private readonly PostgresHandler _ilrPostgresHandler = new($"Host={Environment.GetEnvironmentVariable("ILR_DB_HOST")};Username={Environment.GetEnvironmentVariable("ILR_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("ILR_DB_PASSWORD")};Database=internallabresultapi");
    private readonly CoreDatabaseActions _coreDatabaseActions;
    private readonly int _retryCount = 15;
    private readonly int _retryDelay = 2000;

    public DatabaseActions()
    {
        _coreDatabaseActions = new CoreDatabaseActions(_postgresHandler);
    }
    
    public async Task<List<object[]>> GetResultsFromDatabase(string query)
    {
        return await _postgresHandler.ExecuteReadQueryAsync(query).ToListAsync();
    }

    protected async Task<Dictionary<string, Schema>> GetTableSchema(string table)
    {
        return await _coreDatabaseActions.GetTableSchema(table);
    }
    
    protected async Task<Dictionary<int,string>> GetExamStatusCodes()
    {
        var result = new Dictionary<int,string>();
        var records = await _postgresHandler.ExecuteReadQueryAsync<StatusCode>(
            $"SELECT * FROM \"ExamStatusCode\" ORDER BY \"ExamStatusCodeId\" ASC").ToListAsync();
        foreach (var record in records)
        {
            result.Add(Convert.ToInt32(record.ExamStatusCodeId),Convert.ToString(record.StatusName));
        }

        return result;
    }
    
    protected async Task <List<NotPerformedReason>> GetNotPerformedReasons()
    {
        return await _postgresHandler.ExecuteReadQueryAsync<NotPerformedReason>(
            "SELECT * FROM \"NotPerformedReason\" ORDER BY \"NotPerformedReasonId\" ASC").ToListAsync();
    }
    
    protected async Task<Exam> GetExamByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var examRecords=  await _postgresHandler.ExecuteReadQueryAsync<Exam>(
                $"SELECT * FROM \"Exam\" WHERE \"EvaluationId\" = {evaluationId}").ToListAsync();
            if (examRecords.Count!=0)
                return examRecords[0];
            await Task.Delay(_retryDelay);
        }
        throw new ExamNotFoundException($"EvaluationId {evaluationId} not found in Exam table");
    }
    
    protected async Task<BarcodeExam> GetBarcodeByExamId(int examId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var barcodeExamRecords = await _postgresHandler.ExecuteReadQueryAsync<BarcodeExam>(
                $"SELECT * FROM \"BarcodeExam\" WHERE \"ExamId\" = {examId}").ToListAsync();
            
            if (barcodeExamRecords.Count!=0)
                return barcodeExamRecords[0];
            
            await Task.Delay(_retryDelay);
        }
        
        throw new BarCodeNotFoundException($"ExamId {examId} not found in BarcodeExam table");
    }
    
    protected async Task<bool> ValidateExamStatusCodesByExamId(int examId, List<int> expectedIds)
    {
        var idsNotPresent = new List<int>();
        for (var i = 0; i < _retryCount; i++)
        {
            var examStatusRecords = _postgresHandler.ExecuteReadQueryAsync<ExamStatus>(
                $"SELECT * FROM \"ExamStatus\" WHERE \"ExamId\" = {examId}");
            var statusCodeIds = await examStatusRecords.Select(record => record.ExamStatusCodeId).ToListAsync();
            if (expectedIds.All(id=>statusCodeIds.Contains(id))) 
                return true;
            idsNotPresent = expectedIds.Except(statusCodeIds).ToList();
            await Task.Delay(_retryDelay);
        }
        
        throw new ExamStatusCodeNotFoundException($"ExamStatusCodeIds {string.Join(", ",idsNotPresent)} not found for ExamId {examId}");
    }
    
    protected async Task<bool> ValidateNotPerformedReason(int examId, int answerId, string reason)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var notPerformedReasonRecords = await _postgresHandler.ExecuteReadQueryAsync<NotPerformedReason>(
                "SELECT npr.* FROM \"ExamNotPerformed\" enp " +
                "JOIN \"NotPerformedReason\" npr ON enp.\"NotPerformedReasonId\" = npr.\"NotPerformedReasonId\" " +
                $"WHERE \"ExamId\" = {examId}").FirstAsync();
            if (notPerformedReasonRecords!=null)
                return notPerformedReasonRecords.AnswerId.Equals(answerId) && notPerformedReasonRecords.Reason.Equals(reason);
            
            await Task.Delay(_retryDelay);
        }
        
        throw new NotPerformedReasonNotFoundException($"NotPerformedReason {reason} or AnswerId {answerId} not found for ExamId {examId}");
    }
    
    protected async Task<LabResult> GetLabResultByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var labResultRecords = await _postgresHandler.ExecuteReadQueryAsync<LabResult>(
                "SELECT * FROM \"LabResult\" lr " +
                $"WHERE lr.\"EvaluationId\" = {evaluationId}").ToListAsync();
            if (!labResultRecords.IsEmpty())
                return labResultRecords.First();
            
            await Task.Delay(_retryDelay);
        }
        
        throw new LabResultNotFoundException($"LabResult not found for EvaluationId {evaluationId}");
    }
    
    protected async Task<PdfDeliveredToClient> GetPdfDeliveredByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var pdfDeliveredRecords = await _postgresHandler.ExecuteReadQueryAsync<PdfDeliveredToClient>(
                "SELECT * FROM \"PdfDeliveredToClient\" pdf " +
                $"WHERE pdf.\"EvaluationId\" = {evaluationId}").ToListAsync();
            if (!pdfDeliveredRecords.IsEmpty())
                return pdfDeliveredRecords.First();
            
            await Task.Delay(_retryDelay);
        }
        
        throw new PdfDeliveredToClientNotFoundException($"PdfDeliveredToClient not found for EvaluationId {evaluationId}");
    }
    
    protected async Task<BillRequest> GetBillRequestByExamId(int examId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var billRequestRecords = await _postgresHandler.ExecuteReadQueryAsync<BillRequest>(
                "SELECT * FROM \"BillRequest\" br " +
                $"WHERE br.\"ExamId\" = {examId}").ToListAsync();
            if (!billRequestRecords.IsEmpty())
                return billRequestRecords.First();
            
            await Task.Delay(_retryDelay);
        }
        
        throw new BillRequestNotFoundException($"BillRequest not found for ExamId {examId}");
    }
    
    protected async Task<ProviderPay> GetProviderPayByExamId(int examId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var billRequestRecords = await _postgresHandler.ExecuteReadQueryAsync<ProviderPay>(
                "SELECT * FROM \"ProviderPay\" pp " +
                $"WHERE pp.\"ExamId\" = {examId}").ToListAsync();
            if (!billRequestRecords.IsEmpty())
                return billRequestRecords.First();
            
            await Task.Delay(_retryDelay);
        }
        
        throw new ProviderPayNotFoundException($"ProviderPay not found for ExamId {examId}");
    }
    
    protected async Task<InternalLabResult> GetInternalLabResultByRequestId(string requestId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var labResultRecords = await _ilrPostgresHandler.ExecuteReadQueryAsync<InternalLabResult>(
                "SELECT * FROM \"LabResult\" lr " +
                $"WHERE lr.\"RequestId\" = '{requestId}'").ToListAsync();
            if (!labResultRecords.IsEmpty())
                return labResultRecords.First();
            
            await Task.Delay(_retryDelay);
        }
        
        throw new LabResultNotFoundException($"LabResult not found for RequestId {requestId}");
    }
    
}