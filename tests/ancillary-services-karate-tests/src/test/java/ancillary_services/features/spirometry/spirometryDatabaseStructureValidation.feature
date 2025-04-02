@spirometry
@envnot=prod
Feature: Spirometry Database Structure Validation Check

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }

    @TestCaseKey=ANC-T615
    Scenario: Spirometry StatusCodes added 
        * def examStatusCodes = SpiroDb().getStatusCodes()
        * assert examStatusCodes[0].StatusCodeId == 1
        * assert DataGen().compareStrings(examStatusCodes[0].Name, 'Spirometry Exam Performed')
        * assert examStatusCodes[1].StatusCodeId == 2
        * assert DataGen().compareStrings(examStatusCodes[1].Name, 'Spirometry Exam Not Performed')
        * assert examStatusCodes[2].StatusCodeId == 3
        * assert DataGen().compareStrings(examStatusCodes[2].Name, 'Billable Event Received')
        * assert examStatusCodes[3].StatusCodeId == 4
        * assert DataGen().compareStrings(examStatusCodes[3].Name, 'Bill Request Sent')
        * assert examStatusCodes[4].StatusCodeId == 5
        * assert DataGen().compareStrings(examStatusCodes[4].Name, 'Client PDF Delivered')
        * assert examStatusCodes[5].StatusCodeId == 6
        * assert DataGen().compareStrings(examStatusCodes[5].Name, 'Bill Request Not Sent')
        * assert examStatusCodes[6].StatusCodeId == 7
        * assert DataGen().compareStrings(examStatusCodes[6].Name, 'Overread Processed')
        * assert examStatusCodes[7].StatusCodeId == 8
        * assert DataGen().compareStrings(examStatusCodes[7].Name, 'Results Received')
        * assert examStatusCodes[8].StatusCodeId == 9
        * assert DataGen().compareStrings(examStatusCodes[8].Name, 'Clarification Flag Created')
        * assert examStatusCodes[9].StatusCodeId == 10
        * assert DataGen().compareStrings(examStatusCodes[9].Name, 'Provider Payable Event Received')
        * assert examStatusCodes[10].StatusCodeId == 11
        * assert DataGen().compareStrings(examStatusCodes[10].Name, 'Provider Pay Request Sent')
        * assert examStatusCodes[11].StatusCodeId == 12
        * assert DataGen().compareStrings(examStatusCodes[11].Name, 'Provider Non-Payable Event Received')
        * assert examStatusCodes[12].StatusCodeId == 13
        * assert DataGen().compareStrings(examStatusCodes[12].Name, 'CDI Passed Received')
        * assert examStatusCodes[13].StatusCodeId == 14
        * assert DataGen().compareStrings(examStatusCodes[13].Name, 'CDI Failed with Pay Received')
        * assert examStatusCodes[14].StatusCodeId == 15
        * assert DataGen().compareStrings(examStatusCodes[14].Name, 'CDI Failed without Pay Received')

    @TestCaseKey=ANC-T616
    Scenario: Spirometry ProviderPay table added 
        * json providerPay = SpiroDb().checkTableSchema('ProviderPay')
        * match providerPay[0].column_name == 'ProviderPayId'
        * match providerPay[1].column_name == 'SpirometryExamId'
        * match providerPay[2].column_name == 'PaymentId'
        * match providerPay[3].column_name == 'CreatedDateTime'

    @TestCaseKey=ANC-T656
    Scenario: Spirometry ProviderPay table added 
        * json providerPay = SpiroDb().checkTableSchema('CdiEventForPayment')
        * match providerPay[0].column_name == 'CdiEventForPaymentId'
        * match providerPay[1].column_name == 'EvaluationId'
        * match providerPay[2].column_name == 'RequestId'
        * match providerPay[3].column_name == 'EventType'
        * match providerPay[4].column_name == 'ApplicationId'
        * match providerPay[5].column_name == 'PayProvider'
        * match providerPay[6].column_name == 'Reason'
        * match providerPay[7].column_name == 'DateTime'
        * match providerPay[8].column_name == 'CreatedDateTime'
