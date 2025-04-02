using Signify.Spirometry.Svc.System.Tests.Core.Actions;
using Newtonsoft.Json.Linq;
using Signify.QE.MSTest.Attributes;
using Signify.Dps.Test.Utilities.Database.Models;
using Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

namespace Signify.Spirometry.Svc.System.Tests.Tests;

[TestClass, TestCategory("database")]
public class DatabaseTests : DatabaseActions
{
    [RetryableTestMethod]
    [DynamicData(nameof(GetTableSchemaTestData))]
    public async Task ANC_T616_Validate_Table_Schema(string tableName, List<Schema> expectedSchema)
    {
        // Arrange
        
        // Act
        var actualTableSchema = await GetTableSchema(tableName);
        
        // Assert
        ValidateTableSchema(expectedSchema, actualTableSchema);

    }
    
    [RetryableTestMethod]
    public async Task ANC_T615_Validate_FOBTStatusCodes()
    {
        // Act
        var recordList = await GetSpiroStatusCodes();

        // Assert
        foreach (var fi in typeof(SpiroStatusCode).GetFields())
        {
            var x = (SpiroStatusCode)fi.GetValue(null)!;
            Assert.AreEqual(x.StatusCode, recordList[x.SpiroStatusCodeId]);
        }
        
    }
    
    private static void ValidateTableSchema(IList<Schema> expectedSchema, IDictionary<string, Schema> actualSchema)
    {
        Assert.AreEqual(expectedSchema.Count, actualSchema.Count);
        
        foreach (var columnSchema in expectedSchema)
        {
            Assert.AreEqual(columnSchema.IsNullable, actualSchema[columnSchema.ColumnName].IsNullable);
            Assert.AreEqual(columnSchema.DataType, actualSchema[columnSchema.ColumnName].DataType);
        }
    }
    public static IEnumerable<object[]> GetTableSchemaTestData
    {
        get{
            const string filePath = "../../../../Signify.Spirometry.Svc.System.Tests.Core/Data/tableSchema.json";

            var jObject = JObject.Parse(File.ReadAllText(filePath));

            foreach (var tableData in jObject.Properties())
            {
                yield return [tableData.Name, tableData.Value.ToObject<List<Schema>>()];
            }
        }
    }
    
}

