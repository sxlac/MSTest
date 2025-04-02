using Newtonsoft.Json.Linq;
using Signify.eGFR.System.Tests.Core.Actions;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.eGFR.System.Tests.Core.Models.Database;
using Signify.QE.MSTest.Attributes;

namespace Signify.eGFR.System.Tests.Tests;

[TestClass,TestCategory("database")]
public class DatabaseTests : DatabaseActions
{
    public TestContext TestContext { get; set; }
      
    [RetryableTestMethod]
    [DynamicData(nameof(GetTableSchemaTestData))]
    public async Task ANC_T838_Validate_Table_Schema(string tableName, List<Schema> expectedSchema)
    {
        // Arrange
        
        // Act
        var actualTableSchema = await GetTableSchema(tableName);
        
        // Assert
        ValidateTableSchema(expectedSchema, actualTableSchema);

        TestContext.WriteLine($"{tableName} table columns validated");
        
    }
    
    [RetryableTestMethod] 
    public async Task ANC_T1173_Validate_ExamStatusCodes()
    {
        // Act
        var recordList = await GetExamStatusCodes();

        // Assert
        foreach (var fi in typeof(ExamStatusCode).GetFields())
        {
            var x = (ExamStatusCode)fi.GetValue(null)!;
            recordList[x.ExamStatusCodeId].Should().Be(x.StatusName);
        }
        TestContext.WriteLine("ExamStatusCodes validated");
    }
    
    [RetryableTestMethod]
    public async Task ANC_T1172_Validate_NotPerformedReasons()
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

    
    private static void ValidateTableSchema(IList<Schema> expectedSchema, IDictionary<string, Schema> actualSchema)
    {
        actualSchema.Count.Should().Be(expectedSchema.Count);

        foreach (var columnSchema in expectedSchema)
        {
            actualSchema[columnSchema.ColumnName].IsNullable.Should().Be(columnSchema.IsNullable);
            actualSchema[columnSchema.ColumnName].DataType.Should().Be(columnSchema.DataType);
        }
    }
    
    public static IEnumerable<object[]> GetTableSchemaTestData
    {
        get{
            const string filePath = "../../../../Signify.eGFR.System.Tests.Core/Data/tableSchema.json";

            var jObject = JObject.Parse(File.ReadAllText(filePath));

            foreach (var tableData in jObject.Properties())
            {
                yield return [tableData.Name, tableData.Value.ToObject<List<Schema>>()];
            }
        }
    }
}