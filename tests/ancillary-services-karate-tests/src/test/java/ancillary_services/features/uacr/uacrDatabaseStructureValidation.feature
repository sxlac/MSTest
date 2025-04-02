@uacr
@envnot=prod
Feature: Uacr Database Structure Validation Check

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type("helpers.database.uacr.UacrDb"); return new UacrDb(); }


    @TestCaseKey=ANC-T761 
    Scenario: Uacr ProviderPay table present
        * json providerPay = UacrDb().checkTableSchema('ProviderPay')
        * match providerPay[0].column_name == 'ProviderPayId'
        * match providerPay[1].column_name == 'PaymentId'
        * match providerPay[2].column_name == 'ExamId'
        * match providerPay[3].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T788
    Scenario: Uacr LabResult table present
        * json labResult = UacrDb().checkTableSchema('LabResult')
        * match labResult[0].column_name == 'LabResultId'
        * match labResult[1].column_name == 'EvaluationId'
        * match labResult[2].column_name == 'ReceivedDate'
        * match labResult[3].column_name == 'UacrResult'
        * match labResult[4].column_name == 'ResultColor'
        * match labResult[5].column_name == 'Normality'
        * match labResult[6].column_name == 'NormalityCode'
        * match labResult[7].column_name == 'ResultDescription'
        * match labResult[8].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T835
    Scenario: Uacr BarcodeExam table present
        * json barcodeExam = UacrDb().checkTableSchema('BarcodeExam')
        * match barcodeExam[0].column_name == 'BarcodeExamId'
        * match barcodeExam[1].column_name == 'ExamId'
        * match barcodeExam[2].column_name == 'Barcode'
        * match barcodeExam[3].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T748
    Scenario: Uacr BillRequest table present
        * json billRequest = UacrDb().checkTableSchema('BillRequest')
        * match billRequest[0].column_name == 'BillRequestId'
        * match billRequest[1].column_name == 'ExamId'
        * match billRequest[2].column_name == 'BillId'
        * match billRequest[3].column_name == 'CreatedDateTime'
        * match billRequest[4].column_name == 'Accepted'
        * match billRequest[5].column_name == 'AcceptedAt'
        * match billRequest[6].column_name == 'BillingProductCode'

    @TestCaseKey=ANC-T748
    Scenario: Uacr PdfDeliveredToClient table present
        * json pdfDeliveredToClient = UacrDb().checkTableSchema('PdfDeliveredToClient')
        * match pdfDeliveredToClient[0].column_name == 'PdfDeliveredToClientId'
        * match pdfDeliveredToClient[1].column_name == 'EventId'
        * match pdfDeliveredToClient[2].column_name == 'EvaluationId'
        * match pdfDeliveredToClient[3].column_name == 'BatchId'
        * match pdfDeliveredToClient[4].column_name == 'BatchName'
        * match pdfDeliveredToClient[5].column_name == 'DeliveryDateTime'
        * match pdfDeliveredToClient[6].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T835
    Scenario: Uacr ExamNotPerformed table present
        * json examNotPerformed = UacrDb().checkTableSchema('ExamNotPerformed')
        * match examNotPerformed[0].column_name == 'ExamNotPerformedId'
        * match examNotPerformed[1].column_name == 'ExamId'
        * match examNotPerformed[2].column_name == 'NotPerformedReasonId'
        * match examNotPerformed[3].column_name == 'CreatedDateTime'
        * match examNotPerformed[4].column_name == 'Notes'

    @TestCaseKey=ANC-T835
    Scenario: Uacr ExamStatus table present
        * json examStatus = UacrDb().checkTableSchema('ExamStatus')
        * match examStatus[0].column_name == 'ExamStatusId'
        * match examStatus[1].column_name == 'ExamId'
        * match examStatus[2].column_name == 'ExamStatusCodeId'
        * match examStatus[3].column_name == 'StatusDateTime'
        * match examStatus[4].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T762
    Scenario: Uacr ExamStatusCode table present
        * json examStatusCode = UacrDb().checkTableSchema('ExamStatusCode')
        * match examStatusCode[0].column_name == 'ExamStatusCodeId'
        * match examStatusCode[1].column_name == 'StatusName'

    @TestCaseKey=ANC-T835
    Scenario: Uacr NotPerformedReason table present
        * json notPerformedReason = UacrDb().checkTableSchema('NotPerformedReason')
        * match notPerformedReason[0].column_name == 'NotPerformedReasonId'
        * match notPerformedReason[1].column_name == 'AnswerId'
        * match notPerformedReason[2].column_name == 'Reason'

    @TestCaseKey=ANC-T836
    Scenario: Uacr Exam table present
        * json exam = UacrDb().checkTableSchema('Exam')
        * match exam[0].column_name == 'ExamId'
        * match exam[1].column_name == 'EvaluationId'
        * match exam[2].column_name == 'ApplicationId'
        * match exam[3].column_name == 'ProviderId'
        * match exam[4].column_name == 'MemberId'
        * match exam[5].column_name == 'MemberPlanId'
        * match exam[6].column_name == 'CenseoId'
        * match exam[7].column_name == 'AppointmentId'
        * match exam[8].column_name == 'ClientId'
        * match exam[9].column_name == 'DateOfService'
        * match exam[10].column_name == 'FirstName'
        * match exam[11].column_name == 'MiddleName'
        * match exam[12].column_name == 'LastName'
        * match exam[13].column_name == 'DateOfBirth'
        * match exam[14].column_name == 'AddressLineOne'
        * match exam[15].column_name == 'AddressLineTwo'
        * match exam[16].column_name == 'City'
        * match exam[17].column_name == 'State'
        * match exam[18].column_name == 'ZipCode'
        * match exam[19].column_name == 'NationalProviderIdentifier'
        * match exam[20].column_name == 'EvaluationReceivedDateTime'
        * match exam[21].column_name == 'EvaluationCreatedDateTime'
        * match exam[22].column_name == 'CreatedDateTime'
