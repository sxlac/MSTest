using Newtonsoft.Json.Linq;
using Signify.DEE.Svc.System.Tests.Core.Constants;
using Signify.DEE.Svc.System.Tests.Core.Models.Database;
using Signify.Dps.Test.Utilities.Database.Models;

namespace Signify.DEE.Svc.System.Tests.Tests;

[TestClass, TestCategory("database")]
public class DatabaseTests : DatabaseActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DynamicData(nameof(GetTableSchemaTestData))]
    public async Task ANC_T665_Validate_Table_Schema(string tableName, List<Schema> expectedSchema)
    {
        // Arrange
        // Act
        var actualSchema = await GetTableSchema(tableName);
        
        // Assert
        ValidateTableSchema(expectedSchema, actualSchema);
        TestContext.WriteLine($"{tableName} table columns validated");
    }
    
    [RetryableTestMethod]
    public async Task ANC_T605_Validate_ExamStatusCodes()
    {
        // Act
        var recordList = await GetExamStatusCodes();
        
        // Assert
        recordList.Select(x => x.ExamStatusCodeId)
            .Should().BeEquivalentTo(typeof(ExamStatusCodes).GetFields()
                .Select(y => ((StatusCode)y.GetValue(null)!).ExamStatusCodeId));
        
        recordList.Select(x => x.Name)
            .Should().BeEquivalentTo(typeof(ExamStatusCodes).GetFields()
                .Select(y => ((StatusCode)y.GetValue(null)!).Name));
        
        TestContext.WriteLine("ExamStatusCodes validated.");
    }
    
    [RetryableTestMethod]
    public async Task ANC_T558_Validate_NotPerformedReasons()
    {
        // Act
        var recordList = await GetNotPerformedReasons();
        
        // Assert
        recordList.Select(x => x.AnswerId)
            .Should().BeEquivalentTo(typeof(NotPerformedReasons).GetFields()
                .Select(y => ((NotPerformedReason)y.GetValue(null)!).AnswerId));
        
        recordList.Select(x => x.Reason)
            .Should().BeEquivalentTo(typeof(NotPerformedReasons).GetFields()
                .Select(y => ((NotPerformedReason)y.GetValue(null)!).Reason));
        
        TestContext.WriteLine("NotPerformedReasons validated.");
    }
    
    [RetryableTestMethod]
    public async Task ANC_T557_Validate_LateralityCodes()
    {
        // Act
        var recordList = await GetLateralityCodes();
        
        // Assert
        recordList.Select(x => x.Name)
            .Should().BeEquivalentTo(typeof(LateralityCodes).GetFields()
                .Select(y => ((LateralityCode)y.GetValue(null)!).Name));
        
        recordList.Select(x => x.LateralityCodeId)
            .Should().BeEquivalentTo(typeof(LateralityCodes).GetFields()
                .Select(y => ((LateralityCode)y.GetValue(null)!).LateralityCodeId));
        
        recordList.Select(x => x.Description)
            .Should().BeEquivalentTo(typeof(LateralityCodes).GetFields()
                .Select(y => ((LateralityCode)y.GetValue(null)!).Description));
        
        TestContext.WriteLine("LateralityCodes validated.");
    }
    
    private static void ValidateTableSchema(IList<Schema> expectedSchema, IDictionary<string, Schema> actualSchema)
    {
        actualSchema.Count.Should().Be(expectedSchema.Count);

        foreach (var columnSchema in expectedSchema)
        {
            actualSchema[columnSchema.ColumnName].IsNullable.Should().Be(columnSchema.IsNullable);
            actualSchema[columnSchema.ColumnName].DataType.Should().Be(columnSchema.DataType);
        }
    }
    
    private static IEnumerable<object[]> GetTableSchemaTestData
    {
        get{
            const string filePath = "../../../../Signify.DEE.Svc.System.Tests.Core/Data/tableSchema.json";

            var jObject = JObject.Parse(File.ReadAllText(filePath));

            foreach (var tableData in jObject.Properties())
            {
                yield return [tableData.Name, tableData.Value.ToObject<List<Schema>>()];
            }
        }
    }
}