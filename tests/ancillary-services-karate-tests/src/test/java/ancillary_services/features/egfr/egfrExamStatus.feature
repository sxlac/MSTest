@egfr
@envnot=prod
Feature: eGFR Lab Results Exam Status Tests

    Background:
        * eval if (env == 'prod') karate.abort();

    @TestCaseKey=ANC-T464
    Scenario: eGFR Exam Status
    * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
    * def examStatusCode = EgfrDb().getExamStatusCodeLabResultsReceived()
    # Verify ExamStatusCode table and that ExamStatus "Lab Results Received added
    * match examStatusCode[0].ExamStatusCodeId == 1
    * match examStatusCode[0].StatusName == 'Exam Performed'
    * match examStatusCode[1].ExamStatusCodeId == 2
    * match examStatusCode[1].StatusName == 'Exam Not Performed'
    * match examStatusCode[2].ExamStatusCodeId == 3
    * match examStatusCode[2].StatusName == 'Billable Event Received'
    * match examStatusCode[3].ExamStatusCodeId == 4
    * match examStatusCode[3].StatusName == 'Bill Request Sent'
    * match examStatusCode[4].ExamStatusCodeId == 5
    * match examStatusCode[4].StatusName == 'Client PDF Delivered'
    * match examStatusCode[5].ExamStatusCodeId == 6
    * match examStatusCode[5].StatusName == 'Lab Results Received'

    @TestCaseKey=ANC-T465
    Scenario: eGFR Check Lab Results table added
    * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
    * json labResultsResponse = EgfrDb().checkLabResultsTablePresent()

    # Currently record is added through INSERT in db as this functionality is not linked to anything in the PM currently. It will not be used until ANC-2253
    * match labResultsResponse[0].CenseoId != null
    * match labResultsResponse[0].CreatinineResult != null
    * match labResultsResponse[0].NormalityCode == null
    * match labResultsResponse[0].AccessionedDate != null
    * match labResultsResponse[0].eGFRResult != null
    * match labResultsResponse[0].MailDate != null
    * match labResultsResponse[0].CollectionDate != null
    * match labResultsResponse[0].VendorLabTestId != null
    * match labResultsResponse[0].LabResultId != null
    * match labResultsResponse[0].Normality == null
    * match labResultsResponse[0].VendorLabTestNumber != null
    * match labResultsResponse[0].CreatedDateTime != null

    @TestCaseKey=ANC-T527
    Scenario: eGFR 
    * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
    * json BillRequestSentResponse = EgfrDb().checkBillRequestSentPresent()
    * match BillRequestSentResponse[0].column_name == 'BillRequestSentId'
    * match BillRequestSentResponse[1].column_name == 'BillId'
    * match BillRequestSentResponse[2].column_name == 'CreatedDateTime'
    * match BillRequestSentResponse[3].column_name == 'ExamId'

    * json PdfDeliveredToClientResponse = EgfrDb().checkPdfDeliveredToClientPresent()
    * match PdfDeliveredToClientResponse[0].column_name == 'PdfDeliveredToClientId'
    * match PdfDeliveredToClientResponse[1].column_name == 'EventId'
    * match PdfDeliveredToClientResponse[2].column_name == 'EvaluationId'
    * match PdfDeliveredToClientResponse[3].column_name == 'BatchId'
    * match PdfDeliveredToClientResponse[4].column_name == 'BatchName'
    * match PdfDeliveredToClientResponse[5].column_name == 'DeliveryDateTime'
    * match PdfDeliveredToClientResponse[6].column_name == 'CreatedDateTime'
