using Signify.DEE.Svc.System.Tests.Core.Models.Database;
using Signify.Dps.Test.Utilities.Database.Actions;
using Signify.Dps.Test.Utilities.Database.Exceptions;
using Signify.Dps.Test.Utilities.Database.Models;
using Signify.QE.Core.Utilities;
using Exam = Signify.DEE.Svc.System.Tests.Core.Models.Database.Exam;
using LateralityCode = Signify.DEE.Svc.System.Tests.Core.Models.Database.LateralityCode;
using NotPerformedReason = Signify.DEE.Svc.System.Tests.Core.Models.Database.NotPerformedReason;

namespace Signify.DEE.Svc.System.Tests.Core.Actions;

public class DatabaseActions
{
    private readonly PostgresHandler _postgresHandler = new($"Host={Environment.GetEnvironmentVariable("DEE_DB_HOST")};Username={Environment.GetEnvironmentVariable("DEE_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("DEE_DB_PASSWORD")};Database={Environment.GetEnvironmentVariable("SERVICE")}");
    private readonly CoreDatabaseActions _coreDatabaseActions;
    private readonly int _retryCount = 20;
    private readonly int _retryDelay = 3000;

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
    
    protected async Task<List<StatusCode>> GetExamStatusCodes()
    {
        return await _postgresHandler.ExecuteReadQueryAsync<StatusCode>(
            "SELECT * FROM \"ExamStatusCode\" ORDER BY \"ExamStatusCodeId\" ASC").ToListAsync();
    }
    
    protected async Task <List<NotPerformedReason>> GetNotPerformedReasons()
    {
        return await _postgresHandler.ExecuteReadQueryAsync<NotPerformedReason>(
            $"SELECT * FROM \"NotPerformedReason\" ORDER BY \"NotPerformedReasonId\" ASC").ToListAsync();
    }
    
    protected async Task<List<LateralityCode>> GetLateralityCodes()
    {
        return await _postgresHandler.ExecuteReadQueryAsync<LateralityCode>(
            $"SELECT * FROM \"LateralityCode\" ORDER BY \"LateralityCodeId\" ASC").ToListAsync();
    }

    protected async Task<Exam> GetExamByEvaluationId(int evaluationId)
    {
        for(var i=0; i<_retryCount; i++)
        {
            var exam = await _postgresHandler.ExecuteReadQueryAsync<Exam>(
                $"SELECT * FROM \"Exam\" WHERE \"EvaluationId\" = {evaluationId}").FirstOrDefaultAsync();
            if(exam != null)
            {
                return exam;
            }
            await Task.Delay(_retryDelay);
        }

        throw new ExamNotFoundException($"Exam not found in database for evaluationId:  {evaluationId}");
    }
    
    protected async Task<DeeNotPerformed> GetNotPerformedReasonByExamId(int examId)
    {
        for(var i=0; i<_retryCount; i++)
        {
            var notPerformed = await _postgresHandler.ExecuteReadQueryAsync<DeeNotPerformed>(
                "SELECT dnp.* FROM \"DeeNotPerformed\" dnp "+
                "JOIN \"NotPerformedReason\" npr ON dnp.\"NotPerformedReasonId\" = npr.\"NotPerformedReasonId\" "+
                $"WHERE dnp.\"ExamId\" = {examId}").FirstOrDefaultAsync();
            if(notPerformed != null)
            {
                return notPerformed;
            }
            await Task.Delay(_retryDelay);
        }

        throw new ExamNotFoundException($"DeeNotPerformed not found in database for ExamId:  {examId}");
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
}