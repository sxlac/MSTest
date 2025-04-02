@egfr
@smoke-egfr

Feature: eGFR Database Structure Validation Check

    Background:
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }

    @TestCaseKey=ANC-T610
    Scenario: eGFR StatusCodes added 
        * def examStatusCodes = EgfrDb().getExamStatusCodes()
        * assert examStatusCodes[0].ExamStatusCodeId == 1
        * assert DataGen().compareStrings(examStatusCodes[0].StatusName, 'Exam Performed')
        * assert examStatusCodes[1].ExamStatusCodeId == 2
        * assert DataGen().compareStrings(examStatusCodes[1].StatusName, 'Exam Not Performed')
        * assert examStatusCodes[2].ExamStatusCodeId == 3
        * assert DataGen().compareStrings(examStatusCodes[2].StatusName, 'Billable Event Received')
        * assert examStatusCodes[3].ExamStatusCodeId == 4
        * assert DataGen().compareStrings(examStatusCodes[3].StatusName, 'Bill Request Sent')
        * assert examStatusCodes[4].ExamStatusCodeId == 5
        * assert DataGen().compareStrings(examStatusCodes[4].StatusName, 'Client PDF Delivered')
        * assert examStatusCodes[5].ExamStatusCodeId == 6
        * assert DataGen().compareStrings(examStatusCodes[5].StatusName, 'Lab Results Received')
        * assert examStatusCodes[6].ExamStatusCodeId == 7
        * assert DataGen().compareStrings(examStatusCodes[6].StatusName, 'Bill Request Not Sent')
        * assert examStatusCodes[7].ExamStatusCodeId == 8
        * assert DataGen().compareStrings(examStatusCodes[7].StatusName, 'ProviderPayableEventReceived')
        * assert examStatusCodes[8].ExamStatusCodeId == 9
        * assert DataGen().compareStrings(examStatusCodes[8].StatusName, 'ProviderPayRequestSent')
        * assert examStatusCodes[9].ExamStatusCodeId == 10
        * assert DataGen().compareStrings(examStatusCodes[9].StatusName, 'ProviderNonPayableEventReceived')
        * assert examStatusCodes[10].ExamStatusCodeId == 11
        * assert DataGen().compareStrings(examStatusCodes[10].StatusName, 'CDIPassedReceived')
        * assert examStatusCodes[11].ExamStatusCodeId == 12
        * assert DataGen().compareStrings(examStatusCodes[11].StatusName, 'CDIFailedWithPayReceived')
        * assert examStatusCodes[12].ExamStatusCodeId == 13
        * assert DataGen().compareStrings(examStatusCodes[12].StatusName, 'CDIFailedWithoutPayReceived')    

    @TestCaseKey=ANC-T609
    Scenario: eGFR ProviderPay table added 
        * json providerPay = EgfrDb().checkTableSchema('ProviderPay')
        * match providerPay[0].column_name == 'ProviderPayId'
        * match providerPay[1].column_name == 'PaymentId'
        * match providerPay[2].column_name == 'ExamId'
        * match providerPay[3].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T838
    Scenario: eGFR ExamStatusCode table present
        * json examStatusCode = EgfrDb().checkTableSchema('ExamStatusCode')
        * match examStatusCode[0].column_name == 'ExamStatusCodeId'
        * match examStatusCode[1].column_name == 'StatusName'

    @TestCaseKey=ANC-T838
    Scenario: eGFR NotPerformedReason table present
        * json notPerformedReason = EgfrDb().checkTableSchema('NotPerformedReason')
        * match notPerformedReason[0].column_name == 'NotPerformedReasonId'
        * match notPerformedReason[1].column_name == 'AnswerId'
        * match notPerformedReason[2].column_name == 'Reason'


    @TestCaseKey=ANC-T838
    Scenario: eGFR Exam table added 
        * json exam = EgfrDb().checkTableSchema('Exam')
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

    @TestCaseKey=ANC-T838
    Scenario: eGFR ExamStatus table added 
        * json examStatus = EgfrDb().checkTableSchema('ExamStatus')
        * match examStatus[0].column_name == 'ExamStatusId'
        * match examStatus[1].column_name == 'ExamId'
        * match examStatus[2].column_name == 'ExamStatusCodeId'
        * match examStatus[3].column_name == 'StatusDateTime'
        * match examStatus[4].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T838
    Scenario: eGFR BillRequestSent table added 
        * json billRequestSent = EgfrDb().checkTableSchema('BillRequestSent')
        * match billRequestSent[0].column_name == 'BillRequestSentId'
        * match billRequestSent[1].column_name == 'BillId'
        * match billRequestSent[2].column_name == 'CreatedDateTime'
        * match billRequestSent[3].column_name == 'ExamId'
        * match billRequestSent[4].column_name == 'Accepted'
        * match billRequestSent[5].column_name == 'AcceptedAt'
        * match billRequestSent[6].column_name == 'BillingProductCode'

    @TestCaseKey=ANC-T838
    Scenario: eGFR BarcodeHistory table added 
        * json barcodeHistory = EgfrDb().checkTableSchema('BarcodeHistory')
        * match barcodeHistory[0].column_name == 'BarcodeHistoryId'
        * match barcodeHistory[1].column_name == 'ExamId'
        * match barcodeHistory[2].column_name == 'Barcode'
        * match barcodeHistory[3].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T838
    Scenario: eGFR ExamNotPerformed table added 
        * json examNotPerformed = EgfrDb().checkTableSchema('ExamNotPerformed')
        * match examNotPerformed[0].column_name == 'ExamNotPerformedId'
        * match examNotPerformed[1].column_name == 'ExamId'
        * match examNotPerformed[2].column_name == 'NotPerformedReasonId'
        * match examNotPerformed[3].column_name == 'CreatedDateTime'
        * match examNotPerformed[4].column_name == 'Notes'

   @TestCaseKey=ANC-T838
   Scenario: eGFR QuestLabResult table added 
        * json labResult = EgfrDb().checkTableSchema('QuestLabResult')
        * match labResult[0].column_name == 'LabResultId'
        * match labResult[1].column_name == 'CenseoId'
        * match labResult[2].column_name == 'VendorLabTestId'
        * match labResult[3].column_name == 'VendorLabTestNumber'
        * match labResult[4].column_name == 'eGFRResult'
        * match labResult[5].column_name == 'CreatinineResult'
        * match labResult[6].column_name == 'Normality'
        * match labResult[7].column_name == 'NormalityCode'
        * match labResult[8].column_name == 'MailDate'
        * match labResult[9].column_name == 'CollectionDate'
        * match labResult[10].column_name == 'AccessionedDate'
        * match labResult[11].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T838
    Scenario: eGFR PdfDeliveredToClient table present
        * json pdfDeliveredToClient = EgfrDb().checkTableSchema('PdfDeliveredToClient')
        * match pdfDeliveredToClient[0].column_name == 'PdfDeliveredToClientId'
        * match pdfDeliveredToClient[1].column_name == 'EventId'
        * match pdfDeliveredToClient[2].column_name == 'EvaluationId'
        * match pdfDeliveredToClient[3].column_name == 'BatchId'
        * match pdfDeliveredToClient[4].column_name == 'BatchName'
        * match pdfDeliveredToClient[5].column_name == 'DeliveryDateTime'
        * match pdfDeliveredToClient[6].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T821
    Scenario: eGFR LabResult table added
        * json providerPay = EgfrDb().checkTableSchema('LabResult')
        * match providerPay[0].column_name == 'LabResultId'
        * match providerPay[1].column_name == 'ExamId'
        * match providerPay[2].column_name == 'ReceivedDate'
        * match providerPay[3].column_name == 'EgfrResult'
        * match providerPay[4].column_name == 'NormalityIndicatorId'
        * match providerPay[5].column_name == 'ResultDescription'
        * match providerPay[6].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T820
    Scenario: eGFR NormalityIndicator table added
        * json providerPay = EgfrDb().checkTableSchema('NormalityIndicator')
        * match providerPay[0].column_name == 'NormalityIndicatorId'
        * match providerPay[1].column_name == 'Normality'
        * match providerPay[2].column_name == 'Indicator'
