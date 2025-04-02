using Confluent.Kafka.Admin;
using Signify.FOBT.Svc.System.Tests.Core.Exceptions;
using Signify.FOBT.Svc.System.Tests.Core.Models.Database;
using Signify.QE.Core.Utilities;
using FOBTStatus = Signify.FOBT.Svc.System.Tests.Core.Models.Database.FOBTStatus;
using FOBTNotPerformed = Signify.FOBT.Svc.System.Tests.Core.Models.Database.FOBTNotPerformed;

namespace Signify.FOBT.Svc.System.Tests.Core.Actions;

public class DatabaseActions
{
    private readonly PostgresHandler _postgresHandler = new($"Host={Environment.GetEnvironmentVariable("FOBT_DB_HOST")};Username={Environment.GetEnvironmentVariable("FOBT_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("FOBT_DB_PASSWORD")};Database={Environment.GetEnvironmentVariable("SERVICE")}");

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

    protected async Task<Dictionary<int, string>> GetFOBTStatusCodes()
    {
        var result = new Dictionary<int,string>();
        var records =
            _postgresHandler.ReadDataAsync(
                $"SELECT * FROM \"FOBTStatusCode\"");
        await foreach (var record in records)
        {
            result.Add(Convert.ToInt32(record["FOBTStatusCodeId"]),Convert.ToString(record["StatusCode"]));
        }

        return result; 
    }
    protected  async Task<Models.Database.FOBT> GetFOBTByEvaluationId(int evaluationId, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Models.Database.FOBT>(
                    $"SELECT * FROM \"FOBT\" WHERE \"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(waitSeconds*1000);
        }
        throw new FOBTNotFoundException($"EvaluationId {evaluationId} not found in FOBT table");
        
    }
    
    protected async Task<bool> ValidateExamStatusCodesByExamId(int examId, List<int> expectedIds,int retryCount, int waitSeconds)
    {
        var idsNotPresent = new List<int>();
        for (var i = 0; i < retryCount; i++)
        {
            var examStatusRecords = _postgresHandler.ExecuteReadQueryAsync<FOBTStatus>(
                $"SELECT * FROM \"FOBTStatus\" WHERE \"FOBTId\" = {examId}");
            var statusCodeIds = await examStatusRecords.Select(record => record.FOBTStatusCodeId).ToListAsync();
            if (expectedIds.All(id=>statusCodeIds.Contains(id))) 
                return true;
            idsNotPresent = expectedIds.Except(statusCodeIds).ToList();
            Thread.Sleep(waitSeconds*1000);
        }
        
        throw new ExamStatusCodeNotFoundException($"ExamStatusCodeIds {string.Join(", ",idsNotPresent)} not found for ExamId {examId}");
    }
    
    protected async Task<FOBTNotPerformed> GetNotPerformedRecordByExamId(int examId, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var notPerformedRecords = await _postgresHandler.ExecuteReadQueryAsync<FOBTNotPerformed>(
                $"SELECT * FROM \"FOBTNotPerformed\" np " +
                    "INNER JOIN \"NotPerformedReason\" f ON f.\"NotPerformedReasonId\" = np.\"NotPerformedReasonId\" " +
                    $"WHERE np.\"FOBTId\" = '{examId}'").ToListAsync();
            
            if (notPerformedRecords.Count!=0)
                return notPerformedRecords[0];
            
            Thread.Sleep(waitSeconds*1000);
        }
        
        throw new NotPerformedNotFoundException($"ExamId {examId} not found in FOBTNotPerformed table");
    }
    
    protected async Task<Models.Database.BarcodeHistory> getBarcodeHistoryResultByFOBTId(int examId, int retryCount, int waitSeconds)
    {
        try
        {
            for (var i = 0; i < retryCount; i++)
            {
                var records =
                    await _postgresHandler.ExecuteReadQueryAsync<Models.Database.BarcodeHistory>(
                        $"SELECT * FROM \"FOBTBarcodeHistory\" WHERE \"FOBTId\" = '{examId}'").ToListAsync();
                if ( records.Count!=0)
                    return records[0];
                await Task.Delay(waitSeconds*1000);
            }
            return null;
        }
        catch (FOBTNotFoundException)
        {
            Console.WriteLine($"ExamId {examId} not found in FOBTBarcodeHistory table");
            return null;  
        }
        
    }
    protected async Task<Models.Database.LabResults> getLabResultsByFOBTId(int examId, int retryCount, int waitSeconds)
    {
        
            for (var i = 0; i < retryCount; i++)
            {
                var records =
                    await _postgresHandler.ExecuteReadQueryAsync<Models.Database.LabResults>(
                        $"SELECT * FROM \"LabResults\" WHERE \"FOBTId\" = '{examId}'").ToListAsync();
                if ( records.Count!=0)
                    return records[0];
                await Task.Delay(waitSeconds*1000);
            }
            throw new FOBTNotFoundException($"ExamId {examId} not found in LabResults table");
    }
    protected async Task<List<Models.Database.FOBTBilling>> getBillingResultsByFOBTId(int examId, int retryCount, int waitSeconds)
    {
        
            for (var i = 0; i < retryCount; i++)
            {
                var records =
                    await _postgresHandler.ExecuteReadQueryAsync<Models.Database.FOBTBilling>(
                        $"SELECT * FROM \"FOBTBilling\" WHERE \"FOBTId\" = '{examId}'").ToListAsync();
                if ( records.Count!=0)
                    return records;
                await Task.Delay(waitSeconds*1000);
            }
            throw new FOBTNotFoundException($"ExamId {examId} not found in FOBTBilling table");
    }
    public async Task<ProviderPay> GetProviderPayResultsByExamId(int id, int retryCount, int waitSeconds)
    {
        for (var i = 0; i < retryCount; i++)
        {
            var providerPayRecords = await _postgresHandler.ExecuteReadQueryAsync<ProviderPay>(
                $"SELECT * FROM \"ProviderPay\" pp "+
                $"WHERE pp.\"FOBTId\" = {id}").ToListAsync();

            if (providerPayRecords.Count != 0)
                return providerPayRecords[0];

            Thread.Sleep(waitSeconds * 1000);
        }

        throw new ProviderPayNotFoundException($"FOBTId {id} not found in ProviderPay table");
    }
    
    
    
}