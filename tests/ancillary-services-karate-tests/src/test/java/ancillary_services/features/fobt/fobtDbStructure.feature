@fobt
@smoke-fobt
Feature: FOBT Db Structure Validation Check

    Background:
        * def FobtDb = function() { var FobtDb = Java.type('helpers.database.fobt.FobtDb'); return new FobtDb(); }
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }

    @TestCaseKey=ANC-T602
    Scenario: FOBT StatusCodes added 
        * def examStatusCodes = FobtDb().getExamStatusCodes()
        * assert examStatusCodes[0].FOBTStatusCodeId == 1
        * assert DataGen().compareStrings(examStatusCodes[0].StatusCode , 'FOBTPerformed')
        * assert examStatusCodes[1].FOBTStatusCodeId == 2
        * assert DataGen().compareStrings(examStatusCodes[1].StatusCode , 'InventoryUpdateRequested')
        * assert examStatusCodes[2].FOBTStatusCodeId == 3
        * assert DataGen().compareStrings(examStatusCodes[2].StatusCode , 'InventoryUpdateSuccess')
        * assert examStatusCodes[3].FOBTStatusCodeId == 4
        * assert DataGen().compareStrings(examStatusCodes[3].StatusCode , 'InventoryUpdateFail')
        * assert examStatusCodes[4].FOBTStatusCodeId == 5
        * assert DataGen().compareStrings(examStatusCodes[4].StatusCode , 'BillRequestSent')
        * assert examStatusCodes[5].FOBTStatusCodeId == 6
        * assert DataGen().compareStrings(examStatusCodes[5].StatusCode , 'OrderUpdated')
        * assert examStatusCodes[6].FOBTStatusCodeId == 7
        * assert DataGen().compareStrings(examStatusCodes[6].StatusCode , 'ValidLabResultsReceived')
        * assert examStatusCodes[7].FOBTStatusCodeId == 8
        * assert DataGen().compareStrings(examStatusCodes[7].StatusCode , 'LabOrderCreated')
        * assert examStatusCodes[8].FOBTStatusCodeId == 9
        * assert DataGen().compareStrings(examStatusCodes[8].StatusCode , 'FOBTNotPerformed')
        * assert examStatusCodes[9].FOBTStatusCodeId == 10
        * assert DataGen().compareStrings(examStatusCodes[9].StatusCode , 'InvalidLabResultsReceived')
        * assert examStatusCodes[10].FOBTStatusCodeId == 11
        * assert DataGen().compareStrings(examStatusCodes[10].StatusCode , 'ClientPDFDelivered')
        * assert examStatusCodes[11].FOBTStatusCodeId == 12
        * assert DataGen().compareStrings(examStatusCodes[11].StatusCode , 'FOBT-Left')
        * assert examStatusCodes[12].FOBTStatusCodeId == 13
        * assert DataGen().compareStrings(examStatusCodes[12].StatusCode , 'FOBT-Results')
        * assert examStatusCodes[13].FOBTStatusCodeId == 14
        * assert DataGen().compareStrings(examStatusCodes[13].StatusCode , 'BillRequestNotSent')        
        * assert examStatusCodes[14].FOBTStatusCodeId == 15
        * assert DataGen().compareStrings(examStatusCodes[14].StatusCode , 'ProviderPayableEventReceived')        
        * assert examStatusCodes[15].FOBTStatusCodeId == 16
        * assert DataGen().compareStrings(examStatusCodes[15].StatusCode , 'ProviderNonPayableEventReceived')        
        * assert examStatusCodes[16].FOBTStatusCodeId == 17
        * assert DataGen().compareStrings(examStatusCodes[16].StatusCode , 'ProviderPayRequestSent')        
        * assert examStatusCodes[17].FOBTStatusCodeId == 18
        * assert DataGen().compareStrings(examStatusCodes[17].StatusCode , 'CdiPassedReceived')       
        * assert examStatusCodes[18].FOBTStatusCodeId == 19
        * assert DataGen().compareStrings(examStatusCodes[18].StatusCode , 'CdiFailedWithPayReceived')        
        * assert examStatusCodes[19].FOBTStatusCodeId == 20
        * assert DataGen().compareStrings(examStatusCodes[19].StatusCode , 'CdiFailedWithoutPayReceived')        

    @TestCaseKey=ANC-T603
    Scenario: FOBT ProviderPay table added 
        * json providerPay = FobtDb().checkTableSchema('ProviderPay')
        * match providerPay[0].column_name == 'Id'
        * match providerPay[1].column_name == 'PaymentId'
        * match providerPay[2].column_name == 'FOBTId'
        * match providerPay[3].column_name == 'CreatedDateTime'
