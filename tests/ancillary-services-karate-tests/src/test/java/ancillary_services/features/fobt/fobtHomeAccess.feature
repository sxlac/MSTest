@fobt
@envnot=prod
Feature: FOBT - HomeAccess Exam Performed - Billing

Background:
    * eval if (env == 'prod') karate.abort();
    * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
    * def FobtDb = function() { var FobtDb = Java.type('helpers.database.fobt.FobtDb'); return new FobtDb(); }
    * def FobtFileshare = function() { var FobtFileshare = Java.type('helpers.fileshare.FobtFileshare'); return new FobtFileshare(); }
    * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
    * def timestamp = DataGen().isoTimestamp()
    * def dateStamp = DataGen().isoDateStamp()

    * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
    * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'FOBT'] }).response
    * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
    * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
    * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

@TestCaseKey=ANC-T690
Scenario Outline: FOBT Performed with Home Access Integration - Sent to Billing
    * def randomBarcode = Faker().randomDigit(6)
    * def eventId = DataGen().uuid()
    * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
    * set evaluation.answers =
        """
        [
            {
                'AnswerId': 21113,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': 'Yes'
            },
            {
                'AnswerId': 21119,
                'AnsweredDateTime': '#(timestamp)',
                'AnswerValue': #(randomBarcode),
            },
            {
                'AnswerId': 22034,
                'AnsweredDateTime': '#(dateStamp)',
                'AnswerValue': '#(dateStamp)'
            },
                {
                    "AnswerId": 33445,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 21989,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(providerDetails.signature)"
                },
                {
                    "AnswerId": 28386,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(providerDetails.firstName) #(providerDetails.lastName)"
                },
                {
                    "AnswerId": 28387,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(providerDetails.nationalProviderIdentifier)"
                },
                {
                    "AnswerId": 22019,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(providerDetails.degree)"
                }
        ]
        """
    * karate.call('classpath:helpers/eval/saveEval.feature')
    * karate.call('classpath:helpers/eval/stopEval.feature')
    * karate.call('classpath:helpers/eval/finalizeEval.feature')

    # Get the result from the FOBT db table 
    * def dbResult = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
    * def orderCorrelationId = dbResult.OrderCorrelationId

    * string pdfEventKey = evaluation.evaluationId
    * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyyMMdd')}`
    * def batchId = 12345
    
    # Karate is not letting this string be multi-line, sorry
    * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
    * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': #(evaluation.evaluationId),'CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['FOBT'],'BatchId': #(batchId)}
    * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)
    * eval sleep(8000)
    
    # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTBilling
    * def billingResult = FobtDb().getBillingResultsByEvaluationId(evaluation.evaluationId)
    
    # Validate BillRequestSent Message for 'FOBT-Left' Event published in Kafka
    * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
    
    * match event.BillingProductCode == 'FOBT-Left' 
    * match event.BillId == billingResult[0].BillId.toString()
    * match event.PdfDeliveryDate == '#notnull' 
    * match event.ProductCode == 'FOBT' 
    * match event.EvaluationId == evaluation.evaluationId
    * match event.MemberPlanId == appointment.memberPlanId
    * match event.ProviderId.toString() == appointment.providerId
    * match event.CreatedDate contains  dateStamp
    * match event.ReceivedDate contains  dateStamp
    
    #Drop the results file then wait for it to be processed
    * def fileName = `censeo_results_${DataGen().formattedDateStamp(-4,'yyyy_MM_dd')}_${randomBarcode}.txt`
    * FobtFileshare().createResultsFileWithDefaultValues(fileName, memberDetails, orderCorrelationId, randomBarcode, <lab_result>, <abnormal_indicator>, <exception_message>)
    * FobtFileshare().waitForFileProcessing(fileName);
    
    # Validate that the FOBT_Results Kafka event details are as expected
    * json resultsEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Results", evaluation.evaluationId + '', "Results", 15, 5000))            
    * match resultsEvent.IsBillable == true
    
    # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTBilling
    * def billingResult = FobtDb().getBillingResultsByEvaluationId(evaluation.evaluationId)
    * match billingResult[*].BillingProductCode contains 'FOBT-Left' && 'FOBT-Results'
    * match billingResult[*].ProviderId contains providerDetails.providerId
    * match billingResult[*].AddressLineOne contains memberDetails.address.address1
    * match billingResult[*].EvaluationId contains evaluation.evaluationId
    * match billingResult[*].MemberId contains memberDetails.memberId
    * match billingResult[*].CenseoId contains memberDetails.censeoId
    * match billingResult[*].ClientId contains 14
    * match billingResult[*].AddressLineTwo contains memberDetails.address.address2
    * match billingResult[*].MemberPlanId contains memberDetails.memberPlanId
    * match billingResult[*].UserName contains 'karate'
    * match billingResult[*].FirstName contains memberDetails.firstName
    * match billingResult[*].ZipCode contains memberDetails.address.zipCode
    * match billingResult[*].NationalProviderIdentifier contains providerDetails.nationalProviderIdentifier
    * match billingResult[*].City contains memberDetails.address.city
    * match billingResult[*].MiddleName contains memberDetails.middleName
    * match billingResult[*].AppointmentId contains appointment.appointmentId
    * match billingResult[*].Barcode contains randomBarcode
    * match billingResult[*].State contains memberDetails.address.state
    * match billingResult[*].LastName contains memberDetails.lastName
    * match billingResult[*].ApplicationId contains 'Signify.Evaluation.Service'
    * match billingResult[*].FOBTId contains dbResult.FOBTId
    
    # Validate BillRequestSent Message for 'FOBT-Results' event published in Kafka
    * json billSentEvent = JSON.parse(KafkaConsumerHelper.getEventByTopicAndKeyAndHeaderAndIndex("FOBT_Status", evaluation.evaluationId + '',"BillRequestSent", 1, 15, 5000))

    * match billSentEvent.BillingProductCode == 'FOBT-Results'
    * match billSentEvent.BillId == billingResult[1].BillId.toString() 
    * match billSentEvent.PdfDeliveryDate == '#notnull' 
    * match billSentEvent.ProductCode == 'FOBT' 
    * match billSentEvent.EvaluationId == evaluation.evaluationId
    * match billSentEvent.MemberPlanId == appointment.memberPlanId
    * match billSentEvent.ProviderId.toString() == appointment.providerId
    * match billSentEvent.CreatedDate contains  dateStamp
    * match billSentEvent.ReceivedDate contains  dateStamp 
    
    # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
    * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
    # Status 1 = FOBTPerformed - Status 8 = LabOrderCreated 
    # Status 12 = FOBT-Left - Status 13 = FOBT-Results
    * match examStatusResults[*].FOBTStatusCodeId contains 1 && 8 && 12 && 13

    Examples:
        | lab_result | abnormal_indicator | exception_message | 
        | 'Positive' | 'A'                |    ''             |

    @TestCaseKey=ANC-T691
    Scenario Outline: FOBT Performed with Home Access Integration - Undetermined Test Results - BillRequestNotSent
        * def randomBarcode = Faker().randomDigit(6)
        * def eventId = DataGen().uuid()
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 21113,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Yes'
                },
                {
                    'AnswerId': 21119,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomBarcode),
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(dateStamp)',
                    'AnswerValue': '#(dateStamp)'
                },
                {
                    "AnswerId": 33445,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 21989,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(providerDetails.signature)"
                },
                {
                    "AnswerId": 28386,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(providerDetails.firstName) #(providerDetails.lastName)"
                },
                {
                    "AnswerId": 28387,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(providerDetails.nationalProviderIdentifier)"
                },
                {
                    "AnswerId": 22019,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(providerDetails.degree)"
                }
            ]
            """
        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')
    
        # Get the result from the FOBT db table 
        * def dbResult = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = dbResult.OrderCorrelationId
    
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyyMMdd')}`
        * def batchId = 12345
        
        # Karate is not letting this string be multi-line, sorry
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': #(evaluation.evaluationId),'CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['FOBT'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)
        * eval sleep(8000)
        
        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTBilling
        * def billingResult = FobtDb().getBillingResultsByEvaluationId(evaluation.evaluationId)
        
        # Validate BillRequestSent Message for 'FOBT-Left' Event published in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        
        * match event.BillingProductCode == 'FOBT-Left' 
        * match event.BillId == billingResult[0].BillId.toString()
        * match event.PdfDeliveryDate == '#notnull' 
        * match event.ProductCode == 'FOBT' 
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate contains  dateStamp
        * match event.ReceivedDate contains  dateStamp
        
        #Drop the results file then wait for it to be processed
        * def fileName = `censeo_results_${DataGen().formattedDateStamp(-4,'yyyy_MM_dd')}_${randomBarcode}.txt`
        * FobtFileshare().createResultsFileWithDefaultValues(fileName, memberDetails, orderCorrelationId, randomBarcode, <lab_result>, <abnormal_indicator>, <exception_message>)
        * FobtFileshare().waitForFileProcessing(fileName);
        
        # Validate that the FOBT_Results Kafka event details are as expected
        * json resultsEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Results", evaluation.evaluationId + '', "Results", 15, 5000))            
        * match resultsEvent.IsBillable == false
        
        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTBilling
        * def billingResult = FobtDb().getBillingResultsByEvaluationId(evaluation.evaluationId)
        * match billingResult[0].BillingProductCode =='FOBT-Left'
        * match billingResult[0].ProviderId == providerDetails.providerId
        * match billingResult[0].AddressLineOne == memberDetails.address.address1
        * match billingResult[0].EvaluationId == evaluation.evaluationId
        * match billingResult[0].MemberId == memberDetails.memberId
        * match billingResult[0].CenseoId == memberDetails.censeoId
        * match billingResult[0].ClientId == 14
        * match billingResult[0].AddressLineTwo == memberDetails.address.address2
        * match billingResult[0].MemberPlanId == memberDetails.memberPlanId
        * match billingResult[0].UserName == 'karate'
        * match billingResult[0].FirstName == memberDetails.firstName
        * match billingResult[0].ZipCode == memberDetails.address.zipCode
        * match billingResult[0].NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match billingResult[0].City == memberDetails.address.city
        * match billingResult[0].MiddleName == memberDetails.middleName
        * match billingResult[0].AppointmentId == appointment.appointmentId
        * match billingResult[0].Barcode == randomBarcode
        * match billingResult[0].State == memberDetails.address.state
        * match billingResult[0].LastName == memberDetails.lastName
        * match billingResult[0].ApplicationId == 'Signify.Evaluation.Service'
        * match billingResult[0].FOBTId == dbResult.FOBTId
        
        # Validate BillRequestNotSent event published in Kafka
        * json billNotSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '',"BillRequestNotSent", 15, 5000))

        * match billNotSentEvent.BillingProductCode == 'FOBT-Results'
        * match billNotSentEvent.ProductCode == 'FOBT' 
        * match billNotSentEvent.EvaluationId == evaluation.evaluationId
        * match billNotSentEvent.MemberPlanId == appointment.memberPlanId
        * match billNotSentEvent.ProviderId.toString() == appointment.providerId
        * match billNotSentEvent.CreatedDate contains  dateStamp
        * match billNotSentEvent.ReceivedDate contains  dateStamp 
        
        # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed - Status 8 = LabOrderCreated- Status 10 = InvalidLabResultsReceived
        # Status 12 = FOBT-Left - Status 14 = BillRequestNotSent 
        * match examStatusResults[*].FOBTStatusCodeId contains 1 && 8 && 10 && 12 && 14
        
        Examples:
            | lab_result | abnormal_indicator | exception_message | 
            | 'Negative' | 'U'                | 'Results Expired' |        