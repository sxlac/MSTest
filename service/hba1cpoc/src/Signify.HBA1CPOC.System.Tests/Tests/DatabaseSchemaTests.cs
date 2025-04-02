using Signify.QE.MSTest.Attributes;

namespace Signify.HBA1CPOC.System.Tests.Tests;

[TestClass,TestCategory("regression"),TestCategory("prod_smoke")]
public class DatabaseSchemaTests : DatabaseActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    public void ANC_T989_Validate_HBA1CPOC_Table_Schema()
    {
        // Arrange
        var columnNames = new List<string>()
        {
            "HBA1CPOCId",
            "EvaluationId",
            "MemberPlanId",
            "MemberId",
            "CenseoId",
            "AppointmentId",
            "ProviderId",
            "DateOfService",
            "CreatedDateTime",
            "ReceivedDateTime",
            "ClientId",
            "UserName",
            "ApplicationId",
            "FirstName",
            "MiddleName",
            "LastName",
            "DateOfBirth",
            "AddressLineOne",
            "AddressLineTwo",
            "City",
            "State",
            "ZipCode",
            "NationalProviderIdentifier",
            "ExpirationDate",
            "A1CPercent",
            "NormalityIndicator"
        };
        
        // Act
        var recordList = GetTableSchemaColumnNames("HBA1CPOC");
        // Assert
        ValidateSchema(recordList, columnNames);
        TestContext.WriteLine("HBA1CPOC table columns validated"); 
    }
    
    [RetryableTestMethod]
    public void ANC_T990_Validate_HBA1CPOCNotPerformed_Table_Schema()
    {
        // Arrange
        var columnNames = new List<string>()
        {
            "HBA1CPOCNotPerformedId",
            "HBA1CPOCId",
            "NotPerformedReasonId",
            "CreatedDateTime",
            "Notes"
        };
        
        // Act
        var recordList = GetTableSchemaColumnNames("HBA1CPOCNotPerformed");
        
        // Assert
        ValidateSchema(recordList, columnNames);
        TestContext.WriteLine("HBA1CPOCNotPerformed table columns validated");
    }
    
    [RetryableTestMethod]
    public void ANC_T991_Validate_HBA1CPOCRCMBilling_Table_Schema()
    {
        // Arrange
        var columnNames = new List<string>()
        {
            "Id",
            "BillId",
            "HBA1CPOCId",
            "CreatedDateTime",
            "Accepted",
            "AcceptedAt",
        };
        
        // Act
        var recordList = GetTableSchemaColumnNames("HBA1CPOCRCMBilling");

        // Assert
        ValidateSchema(recordList, columnNames);
        TestContext.WriteLine("HBA1CPOCRCMBilling table columns validate");
    }
    
    [RetryableTestMethod]
    public void ANC_T994_Validate_HBA1CPOCStatus_Table_Schema()
    {
        // Arrange
        var columnNames = new List<string>()
        {
            "HBA1CPOCStatusId",
            "HBA1CPOCStatusCodeId",
            "HBA1CPOCId",
            "CreatedDateTime"
        };
        // Act
        var recordList = GetTableSchemaColumnNames("HBA1CPOCStatus");

        // Assert
        ValidateSchema(recordList, columnNames);
        TestContext.WriteLine("HBA1CPOCStatus table columns validated");
    }
    
    [RetryableTestMethod]
    public void ANC_T1028_Validate_HBA1CPOCStatusCode_Table_Schema()
    {
        // Arrange
        var columnNames = new List<string>()
        {
            "HBA1CPOCStatusCodeId",
            "StatusCode"
        };
        
        // Act
        var recordList = GetTableSchemaColumnNames("HBA1CPOCStatusCode");

        // Assert
        ValidateSchema(recordList, columnNames);
        TestContext.WriteLine("HBA1CPOCStatusCode table columns validated");
    }
    
    [RetryableTestMethod]
    public void ANC_T993_Validate_NotPerformedReason_Table_Schema()
    {
        // Arrange
        var columnNames = new List<string>()
        {
            "NotPerformedReasonId",
            "AnswerId",
            "Reason"
        };
        
        // Act
        var recordList = GetTableSchemaColumnNames("NotPerformedReason");

        // Assert
        ValidateSchema(recordList, columnNames);
        TestContext.WriteLine("NotPerformedReason table columns validated");
    }
    
    [RetryableTestMethod]
    public void ANC_T992_Validate_PDFToClient_Table_Schema()
    {
        // Arrange
        var columnNames = new List<string>()
        {
            "PDFDeliverId",
            "EventId",
            "EvaluationId",
            "DeliveryDateTime",
            "DeliveryCreatedDateTime",
            "BatchId",
            "BatchName",
            "HBA1CPOCId",
            "CreatedDateTime"
        };
        
        // Act
        var recordList = GetTableSchemaColumnNames("PDFToClient");

        // Assert
        ValidateSchema(recordList, columnNames);
        TestContext.WriteLine("PDFToClient table columns validated");
    }
    
    [RetryableTestMethod]
    public void ANC_T995_Validate_ProviderPay_Table_Schema()
    {
        // Arrange
        var columnNames = new List<string>()
        {
            "ProviderPayId",
            "PaymentId",
            "HBA1CPOCId",
            "CreatedDateTime"
        };
        
        // Act
        var recordList = GetTableSchemaColumnNames("ProviderPay");

        // Assert
        ValidateSchema(recordList, columnNames);
        TestContext.WriteLine("ProviderPay table columns validated");
    }
    
    private static void ValidateSchema(List<string> columnList, List<string> columns)
    {
        columnList.Count.Should().Be(columns.Count);
        foreach (var tuple in columnList.Zip(columns, (x, y) => (Actual: x, Expected: y)))
        {
            tuple.Actual.Should().Be(tuple.Expected);
        }
    }
}