using Newtonsoft.Json.Linq;
using Signify.PAD.Svc.System.Tests.Core.Constants;
using Signify.PAD.Svc.System.Tests.Core.Models.Database;

namespace Signify.PAD.Svc.System.Tests.Tests;


[TestClass, TestCategory("database")]
public class DatabaseTests : DatabaseActions
{
    public TestContext TestContext { get; set; }
      
    [RetryableTestMethod]
    [DynamicData(nameof(GetTableSchemaTestData))]
    public async Task ANC_T1038_Validate_Table_Schema(string tableName, List<Schema> expectedSchema)
    {
        // Arrange
        
        // Act
        var actualTableSchema = await GetTableSchema(tableName);
        
        // Assert
        ValidateTableSchema(expectedSchema, actualTableSchema);

        TestContext.WriteLine($"{tableName} table columns validated");
        
    }
    
    [RetryableTestMethod]
    public async Task ANC_T1041_Validate_PADStatusCodes()
    {
        // Act
        var recordList = await GetPadStatusCodes();

        // Assert
        foreach (var fi in typeof(PADStatusCode).GetFields())
        {
            var x = (PADStatusCode)fi.GetValue(null)!;
            recordList[x.PADStatusCodeId].Should().Be(x.StatusCode);
        }
        TestContext.WriteLine("PADStatusCodes validated");
    }
    
    [RetryableTestMethod]
    public async Task ANC_T1042_Validate_LookupPADAnswer()
    {
        // Act
        var recordList = await GetLookupPADAnswer();

        // Assert
        foreach (var fi in typeof(LookupPadAnswers).GetFields())
        {
            var x = (LookupPadAnswer)fi.GetValue(null)!;
            recordList[x.PadAnswerId].Should().Be(x.PadAnswerValue);
        }
        TestContext.WriteLine("LookupPADAnswers validated");
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
            const string filePath = "../../../../Signify.PAD.Svc.System.Tests.Core/Data/tableSchema.json";

            var jObject = JObject.Parse(File.ReadAllText(filePath));

            foreach (var tableData in jObject.Properties())
            {
                yield return [tableData.Name, tableData.Value.ToObject<List<Schema>>()];
            }
        }
    }
    
}