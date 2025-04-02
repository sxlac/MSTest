using Signify.Dps.Test.Utilities.Database.Actions;
using Signify.HBA1CPOC.System.Tests.Core.Models.Database;
using Signify.QE.Core.Utilities;

namespace Signify.HBA1CPOC.System.Tests.Core.Actions;

public class DatabaseActions
{
    private readonly PostgresHandler _postgresHandler = new ($"Host={Environment.GetEnvironmentVariable("HBA1CPOC_DB_HOST")};Username={Environment.GetEnvironmentVariable("HBA1CPOC_DB_USERNAME")};Password={Environment.GetEnvironmentVariable("HBA1CPOC_DB_PASSWORD")};Database={Environment.GetEnvironmentVariable("SERVICE")}");
    private readonly CoreDatabaseActions _coreDatabaseActions;
    private readonly int _retryCount = 15;
    private readonly int _retryDelay = 2000;
    protected List<object[]> GetResultsFromDatabase(string query)
    {
        return _postgresHandler.ExecuteReadQuery(query);
    }
    
    protected List<string> GetTableSchemaColumnNames(string table)
    {
        var returnResult = new List<string>();
        var results = _postgresHandler.ReadData(
            $"SELECT * FROM information_schema.columns WHERE table_schema = \'public\' AND table_name = \'{table}'");
        foreach (var result in results)
        {
            returnResult.Add((string)result["column_name"]);
        }
        return returnResult;
    }    
    
    public bool ValidateHBA1CPOCStatusCodeByHBA1CPOCId(int examId, int expectedId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var examStatusRecords = _postgresHandler.ExecuteReadQuery<HBA1CPOCStatus>(
                $"SELECT * FROM \"HBA1CPOCStatus\" WHERE \"HBA1CPOCId\" = {examId}");
            if (examStatusRecords.Any(record => Convert.ToInt32(record.HBA1CPOCStatusCodeId) == expectedId))  
                return true;
            Thread.Sleep(_retryDelay);
        }
        
        throw new Exception($"HBA1CPOCStatusCodeId {expectedId} not found for HBA1CPOCId {examId}");
    } 
    protected bool ValidateExamStatusCodesByExamId(int examId, List<int> expectedIds)
    {
        var idsNotPresent = new List<int>();
        for (var i = 0; i < _retryCount; i++)
        {
            var examStatusRecords = _postgresHandler.ExecuteReadQuery<HBA1CPOCStatus>(
                $"SELECT * FROM \"HBA1CPOCStatus\" WHERE \"HBA1CPOCId\" = {examId}");
            var statusCodeIds = examStatusRecords.Select(record => record.HBA1CPOCStatusCodeId).ToList();
            if (expectedIds.All(id=>statusCodeIds.Contains(id))) 
                return true;
            idsNotPresent = expectedIds.Except(statusCodeIds).ToList();
            Thread.Sleep(_retryDelay);
        }
        
        throw new Exception($"ExamStatusCodeIds {string.Join(", ",idsNotPresent)} not found for ExamId {examId}");
    }
    
    protected bool ValidateExamStatusCodesNotPresentByExamId(int examId, List<int> unExpectedIds)
    {
        var idsPresent = new List<int>();
        for (var i = 0; i < _retryCount; i++)
        {
            var examStatusRecords = _postgresHandler.ExecuteReadQuery<HBA1CPOCStatus>(
                $"SELECT * FROM \"HBA1CPOCStatus\" WHERE \"HBA1CPOCId\" = {examId}");
            var statusCodeIds = examStatusRecords.Select(record => record.HBA1CPOCStatusCodeId).ToList();
            var nonExistingIds = unExpectedIds.Except(statusCodeIds).ToList();
            if (nonExistingIds.Count==unExpectedIds.Count) 
                return true;
            idsPresent = unExpectedIds.Except(nonExistingIds).ToList();
            Thread.Sleep(_retryDelay);
        }
        
        throw new Exception($"ExamStatusCodeIds {string.Join(", ",idsPresent)} were found for ExamId {examId}");
    }
    
    public Models.Database.HBA1CPOC GetHba1CpocRecordByEvaluationId(int evaluationId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var examRecords=  _postgresHandler.ExecuteReadQuery<Models.Database.HBA1CPOC>(
                $"SELECT * FROM \"HBA1CPOC\" WHERE \"EvaluationId\" = {evaluationId}");
            if (examRecords.Count!=0)
                return examRecords[0];
            Thread.Sleep(_retryDelay);
        }
        throw new Exception($"EvaluationId {evaluationId} not found in Exam table");
    }
    public HBA1CPOCNotPerformed GetNotPerformedRecordByExamId(int examId)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var notPerformedRecords = _postgresHandler.ExecuteReadQuery<HBA1CPOCNotPerformed>(
                $"SELECT * FROM \"HBA1CPOCNotPerformed\" WHERE \"HBA1CPOCId\" = {examId}");
            
            if (notPerformedRecords.Count!=0)
                return notPerformedRecords[0];
            
            Thread.Sleep(_retryDelay);
        }
        
        throw new Exception($"ExamId {examId} not found in HBA1CPOCNotPerformed table");
    }
    
    public NotPerformedReason GetNotPerformedReasonById(int Id)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var notPerformedRecords = _postgresHandler.ExecuteReadQuery<NotPerformedReason>(
                $"SELECT * FROM \"NotPerformedReason\" WHERE \"NotPerformedReasonId\" = {Id}");
            
            if (notPerformedRecords.Count!=0)
                return notPerformedRecords[0];
            
            Thread.Sleep(_retryDelay);
        }
        
        throw new Exception($"NotPerformedReasonId {Id} not found in NotPerformedReason table");
    }
    
    public BillRequest GetBillingResultsByEvaluationId(int Id)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var billingRecords = _postgresHandler.ExecuteReadQuery<BillRequest>(
                $"SELECT * FROM \"HBA1CPOCRCMBilling\" hc " +
                $"INNER JOIN \"HBA1CPOC\" p ON p.\"HBA1CPOCId\" = hc.\"HBA1CPOCId\" " +
                $"WHERE p.\"EvaluationId\" = {Id}");
            
            if (billingRecords.Count!=0)
                return billingRecords[0];
            
            Thread.Sleep(_retryDelay);
        }
        
        throw new Exception($"NotPerformed Record {Id} not found in HBA1CPOCBilling table");
    }

    public ProviderPay GetProviderPayResultsWithEvalId(int Id)
    {
        for (var i = 0; i < _retryCount; i++)
        {
            var providerPayRecords = _postgresHandler.ExecuteReadQuery<ProviderPay>(
                $"SELECT * FROM \"ProviderPay\" pp " +
                $"INNER JOIN \"HBA1CPOC\" p ON p.\"HBA1CPOCId\" = pp.\"HBA1CPOCId\" " +
                $"WHERE p.\"EvaluationId\" = {Id}");

            if (providerPayRecords.Count != 0)
                return providerPayRecords[0];

            Thread.Sleep(_retryDelay);
        }

        return null;
        //throw new Exception($"NotPerformed Record {Id} not found in HBA1CPOCBilling table");
    }
}