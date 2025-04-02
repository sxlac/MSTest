@fobt
@envnot=prod
Feature: FOBT Exam Performed 

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def FobtDb = function() { var FobtDb = Java.type('helpers.database.fobt.FobtDb'); return new FobtDb(); }
        * def FobtFileshare = function() { var FobtFileshare = Java.type('helpers.fileshare.FobtFileshare'); return new FobtFileshare(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'FOBT'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        
    @TestCaseKey=ANC-T344
    Scenario Outline: FOBT Performed
        * def randomBarcode = Faker().randomDigit(6)
        * def eventId = DataGen().uuid()
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

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "Performed", 40, 5000))            
        * match event.Barcode == randomBarcode

        # Validate that the Kafka events include the expected event headers  
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("FOBT_Status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed'

        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTNotPerformed
        * def result = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = result.OrderCorrelationId
        * match result.EvaluationId == evaluation.evaluationId
        * match result.ProviderId == providerDetails.providerId
        * match result.Barcode == randomBarcode
        * match result.ReceivedDateTime.toString() contains dateStamp

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'HomeAccessResultsReceived'}
        * string correlationId = orderCorrelationId
        * string homeaccessTopic = "homeaccess_labresults"
        * string resultsReceivedValue = {'EventId': '#(eventId)','CreatedDateTime': '#(timestamp)','OrderCorrelationId': '#(correlationId)','Barcode': '#(randomBarcode)','LabTestType': 'FOBT','LabResults': #(<lab_result>),'AbnormalIndicator': #(<abnormal_indicator>),'Exception': #(<exception_message>),'CollectionDate': '#(dateStamp)','ServiceDate': '#(timestamp)','ReleaseDate': '#(timestamp)'}
        * kafkaProducerHelper.send(homeaccessTopic, randomBarcode, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 

        # Get the lab results from the database and verify they match the results file
        * def labResult = FobtDb().getLabResultsByEvaluationId(evaluation.evaluationId)[0]
        * match labResult.OrderCorrelationId == orderCorrelationId
        * match labResult.Barcode == randomBarcode
        * match labResult.ProductCode == 'FOBT'
        * match labResult.AbnormalIndicator == <abnormal_indicator>

        # Validate that the Kafka event for the results are as expected
        * json resultsEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Results", evaluation.evaluationId + '', "Results", 10, 5000))             
        * match resultsEvent.ProductCode == 'FOBT'
        * match resultsEvent.EvaluationId == evaluation.evaluationId
        * match resultsEvent.Determination == <abnormal_indicator>
        * match resultsEvent.Barcode == randomBarcode
        * match resultsEvent.IsBillable == true
        * match resultsEvent.Result[0].Result == <lab_result>
        * match resultsEvent.Result[0].AbnormalIndicator == <abnormal_indicator>
        * match resultsEvent.MemberCollectionDate contains  dateStamp

        # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed
        * match examStatusResults[*].FOBTStatusCodeId contains 1

        Examples:
            | lab_result | abnormal_indicator | exception_message | 
            | 'Positive' | 'A'                |    ''             |
            | 'Negative' | 'N'                |    ''             |

    @TestCaseKey=ANC-T667
    Scenario Outline: FOBT Exam Performed with update FOBTBarcodeHistory
        * def randomBarcode = Faker().randomDigit(6)
        * def eventId = DataGen().uuid()
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

        # Validate that the database FOBT details are as expected using EvaluationId in FOBT table. 
        * def result = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = result.OrderCorrelationId
        * match result.EvaluationId == evaluation.evaluationId
        * match result.Barcode == randomBarcode

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'HomeAccessResultsReceived'}
        * string correlationId = orderCorrelationId
        * string homeaccessTopic = "homeaccess_labresults"
        * string resultsReceivedValue = {'EventId': '#(eventId)','CreatedDateTime': '#(timestamp)','OrderCorrelationId': '#(correlationId)','Barcode': '#(randomBarcode)','LabTestType': 'FOBT','LabResults': #(<lab_result>),'AbnormalIndicator': #(<abnormal_indicator>),'Exception': #(<exception_message>),'CollectionDate': '#(timestamp)','ServiceDate': '#(timestamp)','ReleaseDate': '#(timestamp)'}
        * kafkaProducerHelper.send(homeaccessTopic, randomBarcode, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000)  

        # Get the lab results from the database and verify they match the results file
        * def labResult = FobtDb().getLabResultsByEvaluationId(evaluation.evaluationId)[0]
        * match labResult.OrderCorrelationId == orderCorrelationId
        * match labResult.Barcode == randomBarcode
        * match labResult.ProductCode == 'FOBT'
        * match labResult.AbnormalIndicator == <abnormal_indicator>
        
        # Validate that the Kafka event for the results are as expected
        * json resultsEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Results", evaluation.evaluationId + '', "Results", 10, 5000))
        * match resultsEvent.ProductCode == 'FOBT'
        
        # Search the barcode history entry in the FOBTBarcodeHistory table. There should be no entry for this new evaluation.
        * def barcodehistory = FobtDb().getBarcodeHistoryResultByFOBTId(result.FOBTId)[0]
        * match barcodehistory == null

        # Generating new random barcode to publish to the labs_barcode topic
        * def newRandomBarcode = Faker().randomDigit(6)

        # Publish the BarcodeUpdate event to the labs_barcode topic
        * string barcodeUpdateEventValue = {"MemberPlanId":"#(memberDetails.memberPlanId)","EvaluationId":"#(evaluation.evaluationId)", "ProductCode":"FOBT", "Barcode":"#(newRandomBarcode)","OrderCorrelationId":""}
        * string EventKey = evaluation.evaluationId
        * string EventHeader = { 'Type' : 'BarcodeUpdate'}
        * kafkaProducerHelper.send("labs_barcode", EventKey, EventHeader, barcodeUpdateEventValue)
        Then print "BreakPoint",  barcodeUpdateEventValue 

        # Check DB “FOBT” table contains updated barcode for this evaluation;
        * def newBarcodeCheck = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match newBarcodeCheck.Barcode == newRandomBarcode
        
        # Check DB “FOBTBarcodeHistory” table contains entry for old barcode 
        * def updatedBarcodeHistory = FobtDb().getBarcodeHistoryResultByFOBTId(result.FOBTId)[0]
        * match updatedBarcodeHistory.Barcode == randomBarcode

        Examples:
            | lab_result | abnormal_indicator | exception_message | 
            | 'Positive' | 'A'                |      ''           |
            | 'Negative' | 'N'                |      ''           |

    @TestCaseKey=ANC-T730
    Scenario Outline: FOBT Exam Performed - Finding FOBT Record by OrderCorelationId in FOBTBarcodeHistory table
        * def randomBarcode = Faker().randomDigit(6)
        * def eventId = DataGen().uuid()
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
    
        # Validate that the database FOBT details are as expected using EvaluationId in FOBT table. 
        * def result = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = result.OrderCorrelationId
        * def fobtId = result.FOBTId
        * match result.EvaluationId == evaluation.evaluationId
        * match result.Barcode == randomBarcode
    
        # Search the barcode history entry in the FOBTBarcodeHistory table. There should be no entry for this new evaluation.
        * def barcodehistory = FobtDb().getBarcodeHistoryResultByFOBTId(result.FOBTId)[0]
        * match barcodehistory == null

        # Generating new random barcode to publish to the labs_barcode topic
        * def newRandomBarcode = Faker().randomDigit(6)
        
        # Publish the BarcodeUpdate event to the labs_barcode topic using newly created barcode & OrderCorrelationId 
        * string barcodeUpdateEventValue = {"MemberPlanId":"#(memberDetails.memberPlanId)","EvaluationId":"#(evaluation.evaluationId)", "ProductCode":"FOBT", "Barcode":"#(newRandomBarcode)","OrderCorrelationId":""}
        * string EventKey = evaluation.evaluationId
        * string EventHeader = { 'Type' : 'BarcodeUpdate'}
        * kafkaProducerHelper.send("labs_barcode", EventKey, EventHeader, barcodeUpdateEventValue)
        * eval sleep(5000) 
    
        # Check DB “FOBT” table contains updated barcode for this evaluation;
        * def newBarcodeCheck = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match newBarcodeCheck.Barcode == newRandomBarcode
            
        # Check DB “FOBTBarcodeHistory” table contains entry for old barcode 
        * def updatedBarcodeHistory = FobtDb().getBarcodeHistoryResultByFOBTId(result.FOBTId)[0]
        * match updatedBarcodeHistory.Barcode == randomBarcode

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'HomeAccessResultsReceived'}
        * string correlationId = orderCorrelationId
        * string homeaccessTopic = "homeaccess_labresults"
        * string resultsReceivedValue = {'EventId': '#(eventId)','CreatedDateTime': '#(timestamp)','OrderCorrelationId': '#(correlationId)','Barcode': '#(randomBarcode)','LabTestType': 'FOBT','LabResults': #(<lab_result>),'AbnormalIndicator': #(<abnormal_indicator>),'Exception': #(<exception_message>),'CollectionDate': '#(timestamp)','ServiceDate': '#(timestamp)','ReleaseDate': '#(timestamp)'}
        * kafkaProducerHelper.send(homeaccessTopic, randomBarcode, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(5000)  
    
        # Get the lab results from the database and verify they match the results file
        * def labResult = FobtDb().getLabResultsByFOBTId(fobtId)[0]
        * match labResult.OrderCorrelationId.toString() == correlationId
        * match labResult.Barcode == randomBarcode
        * match labResult.ProductCode == 'FOBT'
        * match labResult.AbnormalIndicator == <abnormal_indicator>
            
        # Validate that the Kafka event for the results are as expected
        * json resultsEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Results", evaluation.evaluationId + '', "Results", 10, 5000))
        * match resultsEvent.ProductCode == 'FOBT'
    
        Examples:
            | lab_result | abnormal_indicator | exception_message | 
            | 'Positive' | 'A'                |      ''           |
            | 'Negative' | 'N'                |      ''           |