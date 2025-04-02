using Akka.Streams.Kafka.Extensions;
using Signify.Dps.Test.Utilities.Database.Exceptions;
using Signify.eGFR.System.Tests.Core.Exceptions;
using Signify.eGFR.System.Tests.Core.Models.Database;
using Signify.QE.Core.Utilities;
using ExamNotFoundException = Signify.eGFR.System.Tests.Core.Exceptions.ExamNotFoundException;
using ExamStatus = Signify.eGFR.System.Tests.Core.Models.Database.ExamStatus;
using ExamNotPerformed = Signify.eGFR.System.Tests.Core.Models.Database.ExamNotPerformed;
using ExamStatusCodeNotFoundException = Signify.eGFR.System.Tests.Core.Exceptions.ExamStatusCodeNotFoundException;
using ProviderPayNotFoundException = Signify.eGFR.System.Tests.Core.Exceptions.ProviderPayNotFoundException;

namespace Signify.eGFR.System.Tests.Core.Actions;

public class DatabaseActions
{
    private readonly PostgresHandler _postgresHandler = new($"Host={Environment.GetEnvironmentVariable("EGFR_DB_HOST")};Username={Environment.GetEnvironmentVariable("EGFR_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("EGFR_DB_PASSWORD")};Database={Environment.GetEnvironmentVariable("SERVICE")}");
    private readonly PostgresHandler _ilrPostgresHandler = new($"Host={Environment.GetEnvironmentVariable("ILR_DB_HOST")};Username={Environment.GetEnvironmentVariable("ILR_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("ILR_DB_PASSWORD")};Database=internallabresultapi");
    private const int RetryCount = 20;
    private const int RetryDelay = 3000;

    protected async Task<Dictionary<string, Schema>> GetTableSchema(string table)
    {
        var returnResult = new Dictionary<string,Schema>();
        var results = _postgresHandler.ReadDataAsync(
            $"SELECT * FROM information_schema.columns WHERE table_schema = \'public\' AND table_name = \'{table}'");
        await foreach (var result in results)
        {
            var schema = new Schema
            {
                IsNullable = (string)result["is_nullable"], 
                DataType = (string)result["udt_name"],
                ColumnName = (string)result["column_name"]
            };
            returnResult.Add((string)result["column_name"],schema);
        }
        return returnResult;
    }

    protected async Task<Dictionary<int, string>> GetExamStatusCodes()
    {
        var result = new Dictionary<int,string>();
        var records =
            _postgresHandler.ReadDataAsync(
                $"SELECT * FROM \"ExamStatusCode\"");
        await foreach (var record in records)
        {
            result.Add(Convert.ToInt32(record["ExamStatusCodeId"]),Convert.ToString(record["StatusName"]));
        }

        return result; 
    }

    protected  async Task<Exam> GetExamByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < RetryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Exam>(
                    $"SELECT * FROM \"Exam\" WHERE \"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(RetryDelay);
        }
        throw new ExamNotFoundException($"EvaluationId {evaluationId} not found in Exam table");
        
    }
    
    protected async Task<bool> ValidateExamStatusCodesByExamId(int examId, List<int> expectedIds)
    {
        var idsNotPresent = new List<int>();
        for (var i = 0; i < RetryCount; i++)
        {
            var examStatusRecords = _postgresHandler.ExecuteReadQueryAsync<ExamStatus>(
                $"SELECT * FROM \"ExamStatus\" WHERE \"ExamId\" = {examId}");
            var statusCodeIds = await examStatusRecords.Select(record => record.ExamStatusCodeId).ToListAsync();
            if (expectedIds.All(id=>statusCodeIds.Contains(id))) 
                return true;
            idsNotPresent = expectedIds.Except(statusCodeIds).ToList();
            Thread.Sleep(RetryDelay);
        }
        
        throw new ExamStatusCodeNotFoundException($"ExamStatusCodeIds {string.Join(", ",idsNotPresent)} not found for ExamId {examId}");
    }
    
    protected async Task<ExamNotPerformed> GetNotPerformedRecordByExamId(int examId)
    {
        for (var i = 0; i < RetryCount; i++)
        {
            var notPerformedRecords = await _postgresHandler.ExecuteReadQueryAsync<ExamNotPerformed>(
                $"SELECT * FROM \"ExamNotPerformed\" np " +
                "INNER JOIN \"NotPerformedReason\" f ON f.\"NotPerformedReasonId\" = np.\"NotPerformedReasonId\" " +
                $"WHERE np.\"ExamId\" = {examId}").ToListAsync();
            
            if (notPerformedRecords.Count!=0)
                return notPerformedRecords[0];
            
            Thread.Sleep(RetryDelay);
        }
        
        throw new NotPerformedNotFoundException($"ExamId {examId} not found in ExamNotPerformed table");
    }
    
    protected async Task<ProviderPay> GetProviderPayResultsByExamId(int id)
    {
        for (var i = 0; i < RetryCount; i++)
        {
            var providerPayRecords = await _postgresHandler.ExecuteReadQueryAsync<ProviderPay>(
                $"SELECT * FROM \"ProviderPay\" pp "+
                $"WHERE pp.\"ExamId\" = {id}").ToListAsync();

            if (providerPayRecords.Count != 0)
                return providerPayRecords[0];

            Thread.Sleep(RetryDelay);
        }

        throw new ProviderPayNotFoundException($"ExamId {id} not found in ProviderPay table");
    }
    
    protected async Task<PdfDeliveredToClient> GetPdfDeliveredByEvaluationId(int id)
    {
        for (var i = 0; i < RetryCount; i++)
        {
            var pdfDeliveredRecords = await _postgresHandler.ExecuteReadQueryAsync<PdfDeliveredToClient>(
                $"SELECT * FROM \"PdfDeliveredToClient\" pdf "+
                $"WHERE pdf.\"EvaluationId\" = {id}").ToListAsync();

            if (pdfDeliveredRecords.Count != 0)
                return pdfDeliveredRecords[0];

            Thread.Sleep(RetryDelay);
        }

        throw new PdfDeliveredToClientNotFoundException($"ExamId {id} not found in ProviderPay table");
    }

    protected async Task<List<BillRequestSent>> GetBillRequestByExamId(int examId)
    {

        for (var i = 0; i < RetryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<BillRequestSent>(
                    $"SELECT * FROM \"BillRequestSent\" WHERE \"ExamId\" = '{examId}'").ToListAsync();
            if (records.Count != 0)
                return records;
            await Task.Delay(RetryDelay);
        }

        throw new ExamNotFoundException($"ExamId {examId} not found in BillRequestSent table");
    }
    
    protected async Task<LabResult> GetLabResultByExamId(int examId)
    {
        for (var i = 0; i < RetryCount; i++)
        {
            var labResultRecords = await _postgresHandler.ExecuteReadQueryAsync<LabResult>(
                "SELECT * FROM \"LabResult\" lr " +
                $"WHERE lr.\"ExamId\" = {examId}").ToListAsync();
            if (labResultRecords.Count != 0)
                return labResultRecords[0];
            await Task.Delay(RetryDelay);
        }
        
        throw new LabResultNotFoundException($"LabResult not found for ExamId {examId}");
    }
    
    protected async Task<InternalLabResult> GetInternalLabResultByRequestId(string requestId)
    {
        for (var i = 0; i < RetryCount; i++)
        {
            var labResultRecords = await _ilrPostgresHandler.ExecuteReadQueryAsync<InternalLabResult>(
                "SELECT * FROM \"LabResult\" lr " +
                $"WHERE lr.\"RequestId\" = '{requestId}'").ToListAsync();
            if (!labResultRecords.IsEmpty())
                return labResultRecords.First();
            
            await Task.Delay(RetryDelay);
        }
        
        throw new LabResultNotFoundException($"LabResult not found for RequestId {requestId}");
    }
    
    protected async Task <List<NotPerformedReason>> GetNotPerformedReasons()
    {
        return await _postgresHandler.ExecuteReadQueryAsync<NotPerformedReason>(
            "SELECT * FROM \"NotPerformedReason\" ORDER BY \"NotPerformedReasonId\" ASC").ToListAsync();
    }

}