using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.System.Tests.Core.Exceptions;
using Signify.PAD.Svc.System.Tests.Core.Models.Database;
using Signify.QE.Core.Utilities;
using PADStatus = Signify.PAD.Svc.System.Tests.Core.Models.Database.PADStatus;
using ProviderPay = Signify.PAD.Svc.System.Tests.Core.Models.Database.ProviderPay;
using WaveformDocument = Signify.PAD.Svc.System.Tests.Core.Models.Database.WaveformDocument;

namespace Signify.PAD.Svc.System.Tests.Core.Actions;

public class DatabaseActions
{
    private readonly PostgresHandler _postgresHandler = new($"Host={Environment.GetEnvironmentVariable("PAD_DB_HOST")};Username={Environment.GetEnvironmentVariable("PAD_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("PAD_DB_PASSWORD")};Database={Environment.GetEnvironmentVariable("SERVICE")}");

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

    protected async Task<Dictionary<int, string>> GetPadStatusCodes()
    {
        var result = new Dictionary<int,string>();
        var records =
            _postgresHandler.ReadDataAsync(
                $"SELECT * FROM \"PADStatusCode\"");
        await foreach (var record in records)
        {
            result.Add(Convert.ToInt32(record["PADStatusCodeId"]),Convert.ToString(record["StatusCode"]));
        }

        return result; 
    }
    
    protected async Task<Dictionary<int, string>> GetLookupPADAnswer()
    {
        var result = new Dictionary<int,string>();
        var records =
            _postgresHandler.ReadDataAsync(
                $"SELECT * FROM \"LookupPADAnswer\"");
        await foreach (var record in records)
        {
            result.Add(Convert.ToInt32(record["PADAnswerId"]),Convert.ToString(record["PADAnswerValue"]));
        }

        return result; 
    }

    protected  async Task<Models.Database.PAD> GetPadByEvaluationId(int evaluationId, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Models.Database.PAD>(
                    $"SELECT * FROM \"PAD\" WHERE \"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(waitSeconds*1000);
        }
        throw new PadNotFoundException($"EvaluationId {evaluationId} not found in PAD table");
        
    }
    protected  async Task<Models.Database.AoeSymptomSupportResult> getAoESymptomSupportResultsByEvaluationId(int evaluationId, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Models.Database.AoeSymptomSupportResult>(
                    $"SELECT * FROM \"PAD\" np " +
                    "INNER JOIN \"AoeSymptomSupportResult\" p ON p.\"PADId\" = np.\"PADId\" " +
                    $"WHERE np.\"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(waitSeconds*1000);
        }
        throw new PadNotFoundException($"EvaluationId {evaluationId} not found in PAD table");
        
    }
    
    protected async Task<bool> ValidateExamStatusCodesByExamId(int examId, List<int> expectedIds,int retryCount, int waitSeconds)
    {
        var idsNotPresent = new List<int>();
        for (var i = 0; i < retryCount; i++)
        {
            var examStatusRecords = _postgresHandler.ExecuteReadQueryAsync<PADStatus>(
                $"SELECT * FROM \"PADStatus\" WHERE \"PADId\" = {examId}");
            var statusCodeIds = await examStatusRecords.Select(record => record.PADStatusCodeId).ToListAsync();
            if (expectedIds.All(id=>statusCodeIds.Contains(id))) 
                return true;
            idsNotPresent = expectedIds.Except(statusCodeIds).ToList();
            Thread.Sleep(waitSeconds*1000);
        }
        
        throw new ExamStatusCodeNotFoundException($"ExamStatusCodeIds {string.Join(", ",idsNotPresent)} not found for ExamId {examId}");
    }
    
    protected async Task<NotPerformedReason> GetNotPerformedRecordByExamId(int examId, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var notPerformedRecords = await _postgresHandler.ExecuteReadQueryAsync<NotPerformedReason>(
                $"SELECT * FROM \"NotPerformed\" WHERE \"PADId\" = {examId}").ToListAsync();
            
            if (notPerformedRecords.Count!=0)
                return notPerformedRecords[0];
            
            Thread.Sleep(waitSeconds*1000);
        }
        
        throw new NotPerformedNotFoundException($"ExamId {examId} not found in ExamNotPerformed table");
    }
    
    public async Task<ProviderPay> GetProviderPayResultsByExamId(int id, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var providerPayRecords = await _postgresHandler.ExecuteReadQueryAsync<ProviderPay>(
                $"SELECT * FROM \"ProviderPay\" pp "+
                $"WHERE pp.\"PADId\" = {id}").ToListAsync();

            if (providerPayRecords.Count != 0)
                return providerPayRecords[0];

            Thread.Sleep(waitSeconds * 1000);
        }

        throw new ProviderPayNotFoundException($"PADId {id} not found in ProviderPay table");
    }
    
    protected async Task<WaveformDocument> GetWaveformDocumentResultsByMemberPlanId(long memberPlanId, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var waveformDocumentRecords = await _postgresHandler.ExecuteReadQueryAsync<WaveformDocument>(
                $"SELECT * FROM \"WaveformDocument\" wd "+
                $"WHERE wd.\"MemberPlanId\" = {memberPlanId}").ToListAsync();

            if (waveformDocumentRecords.Count != 0)
                return waveformDocumentRecords[0];

            Thread.Sleep(waitSeconds * 1000);
        }

        throw new WaveformDocumentNotFoundException($"MemberPlanId {memberPlanId} not found in WaveformDocument table");
    }
    
    public async Task<PadRCMBilling> GetBillingResultsByEvaluationId(int id, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var billingRecords = await _postgresHandler.ExecuteReadQueryAsync<PadRCMBilling>(
                $"SELECT * FROM \"PADRCMBilling\" b " +
                $"INNER JOIN \"PAD\" p ON p.\"PADId\" = b.\"PADId\" " +
                $"WHERE p.\"EvaluationId\" = {id}").ToListAsync();
            
            if (billingRecords.Count!=0)
                return billingRecords[0];
            
            await Task.Delay(waitSeconds*1000);
        }
        
        throw new BillRequestNotFoundException($"Record {id} not found in BillRequest table");
    }
    
    protected async Task<PDFToClient> GetPdfDeliveryByEvaluationId(int evaluationId, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var pdfDeliveryRecords = await _postgresHandler.ExecuteReadQueryAsync<PDFToClient>(
                $"SELECT * FROM \"PDFToClient\" WHERE \"EvaluationId\" = {evaluationId}").ToListAsync();
                
            if (pdfDeliveryRecords.Count!=0)
                return pdfDeliveryRecords[0];
                
            await Task.Delay(waitSeconds*1000);
        }
            
        throw new Exception($"EvaluationId {evaluationId} not found in PDFToClient table");
    }
}