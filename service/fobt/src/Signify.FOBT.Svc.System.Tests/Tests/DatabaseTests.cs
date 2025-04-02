using Newtonsoft.Json.Linq;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.FOBT.Svc.System.Tests.Core.Actions;
using Signify.FOBT.Svc.System.Tests.Core.Models.Database;
using Signify.QE.MSTest.Attributes;
namespace Signify.FOBT.Svc.System.Tests.Tests;

[TestClass, TestCategory("database")]
public class DatabaseTests : DatabaseActions
{
    public TestContext TestContext { get; set; }
      
    [RetryableTestMethod]
    [DynamicData(nameof(GetTableSchemaTestData))]
    public async Task ANC_T603_Validate_Table_Schema(string tableName, List<Schema> expectedSchema)
    {
        // Arrange
        
        // Act
        var actualTableSchema = await GetTableSchema(tableName);
        
        // Assert
        ValidateTableSchema(expectedSchema, actualTableSchema);

        TestContext.WriteLine($"{tableName} table columns validated");
        
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
    
    [RetryableTestMethod]
    public async Task ANC_T602_Validate_FOBTStatusCodes()
    {
        // Act
        var recordList = await GetFOBTStatusCodes();

        // Assert
        foreach (var fi in typeof(FOBTStatusCode).GetFields())
        {
            var x = (FOBTStatusCode)fi.GetValue(null)!;
            recordList[x.FOBTStatusCodeId].Should().Be(x.StatusCode);
        }
        TestContext.WriteLine("FOBT StatusCodes validated");
    }
    public static IEnumerable<object[]> GetTableSchemaTestData
    {
        get{
            const string filePath = "../../../../Signify.FOBT.Svc.System.Tests.Core/Data/tableSchema.json";

            var jObject = JObject.Parse(File.ReadAllText(filePath));

            foreach (var tableData in jObject.Properties())
            {
                yield return [tableData.Name, tableData.Value.ToObject<List<Schema>>()];
            }
        }
    }
}