using Signify.Dps.Test.Utilities.Database.Actions;
using Signify.Dps.Test.Utilities.Database.Models;
using Signify.QE.Core.Utilities;
using Signify.Spirometry.Svc.System.Tests.Core.Exceptions;
using Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

namespace Signify.Spirometry.Svc.System.Tests.Core.Actions;


public class DatabaseActions
{
    private readonly PostgresHandler _postgresHandler = new($"Host={Environment.GetEnvironmentVariable("SPIROMETRY_DB_HOST")};Username={Environment.GetEnvironmentVariable("SPIROMETRY_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("SPIROMETRY_DB_PASSWORD")};Database={Environment.GetEnvironmentVariable("SERVICE")}");
    private readonly CoreDatabaseActions _coreDatabaseActions;
    private readonly int _retryCount = 15;
    private readonly int _retryDelay = 2000;

    public DatabaseActions()
    {
        _coreDatabaseActions = new CoreDatabaseActions(_postgresHandler);
    }
    
    protected async Task<Dictionary<string, Schema>> GetTableSchema(string table)
    {
        return await _coreDatabaseActions.GetTableSchema(table);
    }
    protected async Task<Dictionary<int, string>> GetSpiroStatusCodes()
    {
        var result = new Dictionary<int,string>();
        var records =
            _postgresHandler.ReadDataAsync(
                $"SELECT * FROM \"StatusCode\"");
        await foreach (var record in records)
        {
            result.Add(Convert.ToInt32(record["StatusCodeId"]),Convert.ToString(record["Name"]));
        }

        return result; 
    }
    
    protected  async Task<Models.Database.SpiroResults> GetSpiroResultsByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Models.Database.SpiroResults>(
                    $"SELECT * FROM \"SpirometryExamResults\" ser " +
                    "INNER JOIN \"SpirometryExam\" se ON ser.\"SpirometryExamId\" = se.\"SpirometryExamId\" " +
                    "INNER JOIN \"NormalityIndicator\" ni ON ser.\"NormalityIndicatorId\" = ni.\"NormalityIndicatorId\" " +
                    "INNER JOIN \"OccurrenceFrequency\" oc ON oc.\"OccurrenceFrequencyId\" = ser.\"CoughMucusOccurrenceFrequencyId\" " +
                    "LEFT JOIN \"OccurrenceFrequency\" occ ON occ.\"OccurrenceFrequencyId\" = ser.\"NoisyChestOccurrenceFrequencyId\" " +
                    $"WHERE se.\"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(_retryDelay);
        }
        throw new SpiroNotFoundException($"EvaluationId {evaluationId} not found in SpirometryExamResults table");
        
    } 
    protected  async Task<Models.Database.SpiroBilling> getBillingResultByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Models.Database.SpiroBilling>(
                    $"SELECT * FROM \"BillRequestSent\" brs " +
                    "INNER JOIN \"SpirometryExam\" se ON se.\"SpirometryExamId\" = brs.\"SpirometryExamId\" " +
                    $"WHERE se.\"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(_retryDelay);
        }
        throw new SpiroNotFoundException($"EvaluationId {evaluationId} not found in Spirometry BillRequestSent table");
        
    } 
    protected  async Task<Models.Database.SpiroExam> getSpiroExamByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Models.Database.SpiroExam>(
                    $"SELECT * FROM \"SpirometryExam\" WHERE \"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(_retryDelay);
        }
        throw new SpiroNotFoundException($"EvaluationId {evaluationId} not found in Spirometry Exam table");
        
    } 
    protected async Task<ExamNotPerformed> GetNotPerformedRecordByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var notPerformedRecords = await _postgresHandler.ExecuteReadQueryAsync<ExamNotPerformed>(
                $"SELECT * FROM \"SpirometryExam\" se " +
                "INNER JOIN \"ExamNotPerformed\" f ON f.\"SpirometryExamId\" = se.\"SpirometryExamId\" " +
                "INNER JOIN \"NotPerformedReason\" nr ON nr.\"NotPerformedReasonId\" = f.\"NotPerformedReasonId\" " +
                $"WHERE se.\"EvaluationId\" = '{evaluationId}'").ToListAsync();
            
            if (notPerformedRecords.Count!=0)
                return notPerformedRecords[0];
            
            await Task.Delay(_retryDelay);
        }
        
        throw new NotPerformedNotFoundException($"ExamId {evaluationId} not found in ExamNotPerformed table");
    }
    protected async Task<ProviderPay> GetProviderPayByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var providerPayRecords = await _postgresHandler.ExecuteReadQueryAsync<ProviderPay>(
                $"SELECT * FROM \"SpirometryExam\" se " +
                "INNER JOIN \"ProviderPay\" f ON f.\"SpirometryExamId\" = se.\"SpirometryExamId\" " +
                $"WHERE se.\"EvaluationId\" = '{evaluationId}'").ToListAsync();
            
            if (providerPayRecords.Count!=0)
                return providerPayRecords[0];
            
            await Task.Delay(_retryDelay);
        }
        
        throw new ProviderPayNotFoundException($"EvaluationId {evaluationId} not found in ProviderPay table");
    }
    protected async Task<EvaluationSaga> getEvalSagaByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var evaluationSaga = await _postgresHandler.ExecuteReadQueryAsync<EvaluationSaga>(
                $"SELECT * FROM \"Spirometry_EvaluationSaga\" se " +
                $"WHERE se.\"Correlation_EvaluationId\" = '{evaluationId}'").ToListAsync();
            
            if (evaluationSaga.Count!=0)
                return evaluationSaga[0];
            
            await Task.Delay(_retryDelay);
        }
        
        throw new ProviderPayNotFoundException($"EvaluationId {evaluationId} not found in Spirometry_EvaluationSaga table");
    }
    
    protected async Task<bool> ValidateExamStatusCodesByExamId(int examId, List<int> expectedIds,int retryCount, int waitSeconds)
    {
        var idsNotPresent = new List<int>();
        for (var i = 0; i < retryCount; i++)
        {
            var examStatusRecords = _postgresHandler.ExecuteReadQueryAsync<ExamStatus>(
                $"SELECT * FROM \"ExamStatus\" WHERE \"SpirometryExamId\" = {examId}");
            var statusCodeIds = await examStatusRecords.Select(record => record.StatusCodeId).ToListAsync();
            if (expectedIds.All(id=>statusCodeIds.Contains(id))) 
                return true;
            idsNotPresent = expectedIds.Except(statusCodeIds).ToList();
            Thread.Sleep(waitSeconds*1000);
        }
        
        throw new ExamStatusCodeNotFoundException($"ExamStatusCodeIds {string.Join(", ",idsNotPresent)} not found for ExamId {examId}");
    }
    
    protected  async Task<Models.Database.SpiroResults> GetSpiroFrequencyResultsByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Models.Database.SpiroResults>(
                    $"SELECT ser.*, se.*, ni.*, oc.\"Frequency\" AS \"CoughMucusOccurrenceFrequencyValue\" ,occ.\"Frequency\" AS \"NoisyChestOccurrenceFrequencyValue\", ocb.\"Frequency\" AS \"ShortnessOfBreathPAOccurrenceValue\" " +
                    "FROM \"SpirometryExamResults\" ser " +
                    "INNER JOIN \"SpirometryExam\" se ON ser.\"SpirometryExamId\" = se.\"SpirometryExamId\" " +
                    "INNER JOIN \"NormalityIndicator\" ni ON ser.\"NormalityIndicatorId\" = ni.\"NormalityIndicatorId\" " +
                    "INNER JOIN \"OccurrenceFrequency\" oc ON oc.\"OccurrenceFrequencyId\" = ser.\"CoughMucusOccurrenceFrequencyId\" " +
                    "LEFT JOIN \"OccurrenceFrequency\" occ ON occ.\"OccurrenceFrequencyId\" = ser.\"NoisyChestOccurrenceFrequencyId\" " +
                    "LEFT JOIN \"OccurrenceFrequency\" ocb ON ocb.\"OccurrenceFrequencyId\" = ser.\"ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId\" " +
                    $"WHERE se.\"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(_retryDelay);
        }
        throw new SpiroNotFoundException($"EvaluationId {evaluationId} not found in SpirometryExamResults table");
        
    }
    
    protected  async Task<Models.Database.SpiroResults> GetSpiroTrileanTypeResultsByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var records =
                await _postgresHandler.ExecuteReadQueryAsync<Models.Database.SpiroResults>(
                    $"SELECT ser.*, se.*, ni.*, tt.\"TrileanValue\" AS \"HadWheezingPast12moTrileanType\" ,tr.\"TrileanValue\" AS \"GetsShortnessOfBreathAtRestTrileanType\", tl.\"TrileanValue\" AS \"GetsShortnessOfBreathWithMildExertionTrileanType\" " +
                    "FROM \"SpirometryExamResults\" ser " +
                    "INNER JOIN \"SpirometryExam\" se ON ser.\"SpirometryExamId\" = se.\"SpirometryExamId\" " +
                    "INNER JOIN \"NormalityIndicator\" ni ON ser.\"NormalityIndicatorId\" = ni.\"NormalityIndicatorId\" " +
                    "INNER JOIN \"TrileanType\" tt ON tt.\"TrileanTypeId\" = ser.\"HadWheezingPast12moTrileanTypeId\" " +
                    "LEFT JOIN \"TrileanType\" tr ON tr.\"TrileanTypeId\" = ser.\"GetsShortnessOfBreathAtRestTrileanTypeId\" " +
                    "LEFT JOIN \"TrileanType\" tl ON tl.\"TrileanTypeId\" = ser.\"GetsShortnessOfBreathWithMildExertionTrileanTypeId\" " +
                    $"WHERE se.\"EvaluationId\" = '{evaluationId}'").ToListAsync();
            if ( records.Count!=0)
                return records[0];
            await Task.Delay(_retryDelay);
        }
        throw new SpiroNotFoundException($"EvaluationId {evaluationId} not found in SpirometryExamResults table");
    }
   
    
    
}