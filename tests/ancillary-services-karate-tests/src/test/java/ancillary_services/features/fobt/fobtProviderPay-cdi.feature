@fobt
@envnot=prod
Feature: FOBT CDI events based ProviderPay tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def FobtDb = function() { var FobtDb = Java.type('helpers.database.fobt.FobtDb'); return new FobtDb(); }
        * def FobtFileshare = function() { var FobtFileshare = Java.type('helpers.fileshare.FobtFileshare'); return new FobtFileshare(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def cdiDateTime = DataGen().utcTimestampWithOffset(10)
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'FOBT'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @TestCaseKey=ANC-T620
    Scenario Outline: FOBT Provider Pay. Scenario is to drop the file after the cdi event is raised
                      1. (Business rules met) - CDIPassedEvent
                      2. CDIFailedEvent payProvider - true

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


        # Get the result from the FOBT db table 
        * def dbResult = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = dbResult.OrderCorrelationId
        * match dbResult == '#notnull'

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"FOBT"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

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
        * match resultsEvent.EvaluationId == evaluation.evaluationId
        * match resultsEvent.Determination == <abnormal_indicator>
        * match resultsEvent.Barcode == randomBarcode
        * match resultsEvent.IsBillable == true
        * match resultsEvent.Result[0].Result == <lab_result>
        * match resultsEvent.Result[0].AbnormalIndicator == <abnormal_indicator>

        # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed
        * match examStatusResults[*].FOBTStatusCodeId contains 1


        # Validate the entry in the ProviderPay table
        # TO VERIFY CORRECT DATA RECORD FIELDS WHEN FOBT ProviderPay WILL BE ON LOWER ENVIRONMENTS
        * def providerPayResult = FobtDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.AddressLineOne == memberDetails.address.address1
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberId == memberDetails.memberId
        * match providerPayResult.CenseoId == memberDetails.censeoId
        * match providerPayResult.ClientId == 14
        * match providerPayResult.AddressLineTwo == memberDetails.address.address2
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.UserName == 'karate'
        * match providerPayResult.FirstName == memberDetails.firstName
        * match providerPayResult.ZipCode == memberDetails.address.zipCode
        * match providerPayResult.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match providerPayResult.City == memberDetails.address.city
        * match providerPayResult.MiddleName == memberDetails.middleName
        * match providerPayResult.AppointmentId == appointment.appointmentId
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.PaymentId != null
        * match providerPayResult.LastName == memberDetails.lastName
        * match providerPayResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the entry using EvaluationId in FOBT & FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed, Status 15 = ProviderPayableEventReceived, Status 17 = ProviderPayRequestSent
        # Status 18 = CdiPassedReceived, Status 19 = CdiFailedWithPayReceived
        * match examStatusResults[*].FOBTStatusCodeId contains 1 && 15 && 17 && <statusId>

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("FOBT_Status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'ProviderPayRequestSent' && 'ProviderPayableEventReceived'


        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("providerpay_internal", providerPayResult.PaymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == providerPayResult.PaymentId
        * match paymentRequested["event.providerId"] == providerPayResult.ProviderId
        * match paymentRequested["event.productCode"] == 'FOBT'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * assert paymentRequested["event.dateOfService"] == utcDateTime.split('T')[0]
        * match paymentRequested["event.personId"] == providerPayResult.CenseoId
        * match paymentRequested["event.commonClientId"] == providerPayResult.ClientId
        * match paymentRequested["event.engagementTypeOptions"] == 1
        * match paymentRequested["timestamp"] == '#notnull'

        # Validate that the Kafka event - ProviderPayableEventReceived - was raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == <cdiEventHeaderName>

        # Validate that the Kafka event details are as expected for FOBT_Status
        * json fobtStatus = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000)) 
        * match fobtStatus.EvaluationId == evaluation.evaluationId
        * match fobtStatus.ProviderId == providerDetails.providerId
        * match fobtStatus.MemberPlanId == memberDetails.memberPlanId
        * match fobtStatus.CreatedDate == '#notnull'
        * match fobtStatus.ProviderPayProductCode == 'FOBT'
        * match fobtStatus.ReceivedDate == '#notnull'
        * match fobtStatus.ProductCode == 'FOBT'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * match fobtStatus.PaymentId == providerPayResult.PaymentId

        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTNotPerformed
        * def result = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = result.OrderCorrelationId
        * match result.EvaluationId == evaluation.evaluationId
        * match result.ProviderId == providerDetails.providerId
        * match result.Barcode == randomBarcode
        * match result.ReceivedDateTime.toString() contains dateStamp

        Examples:
        | lab_result | abnormal_indicator | cdiEventHeaderName | payProvider | statusId | exception_message | 
        | 'Positive' | 'A'                | "CDIPassedEvent"   | true        | 18       | ''                |
        | 'Positive' | 'A'                | "CDIFailedEvent"   | true        | 19       | ''                |
        | 'Negative' | 'N'                | "CDIPassedEvent"   | true        | 18       | ''                |
        | 'Negative' | 'N'                | "CDIFailedEvent"   | true        | 19       | ''                |

    @ignore
    @TestCaseKey=ANC-T622
    Scenario Outline: FOBT Provider Pay. Scenario is to drop the file after the cdi event is raised 
                      1. CDIFailedEvent payProvider - false


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


        # Get the result from the FOBT db table 
        * def dbResult = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = dbResult.OrderCorrelationId
        * match dbResult == '#notnull'

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"FOBT"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

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
        * match resultsEvent.EvaluationId == evaluation.evaluationId
        * match resultsEvent.Determination == <abnormal_indicator>
        * match resultsEvent.Barcode == randomBarcode
        * match resultsEvent.IsBillable == true
        * match resultsEvent.Result[0].Result == <lab_result>
        * match resultsEvent.Result[0].AbnormalIndicator == <abnormal_indicator>

        # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed
        * match examStatusResults[*].FOBTStatusCodeId contains 1

        # Validate the entry in the ProviderPay table
        * def providerPayResult = FobtDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'

        # Validate the entry using EvaluationId in FOBT & FOBTStatus tables
        * def examStatusResultsByEval = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed, Status 15 = ProviderPayableEventReceived, Status 17 = ProviderPayRequestSent
        # Status 18 = CdiPassedReceived, Status 20 = CdiFailedWithoutPayReceived
        * match examStatusResults[*].FOBTStatusCodeId contains 1 && <statusId> && 16
        * match examStatusResultsByEval[*].FOBTStatusCodeId !contains 15
        * match examStatusResultsByEval[*].FOBTStatusCodeId !contains 17

        # Validate that a Kafka event related to ProviderPay was not raised
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", dbResult.CenseoId, 10, 5000)
        * assert paymentRequestEvent.length == 0

        # Validate that the Kafka event was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))   
        * match event == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))   
        * match nonPayableEvent == {}

        * def examStatusCodeDetails = FobtDb().getExamStatusForStatusCodeByEvaluationId(evaluation.evaluationId, 16)
        * match examStatusCodeDetails == '#notnull'
        
        # Validate that the Kafka event - ProviderNonPayableEventReceived - was raised.
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))
        * match nonPayableEvent.EvaluationId == dbResult.EvaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.MemberPlanId == memberDetails.memberPlanId
        * match nonPayableEvent.Reason contains <expectedFailReason>
        * match nonPayableEvent.ProductCode == "FOBT"
        * match DataGen().RemoveMilliSeconds(nonPayableEvent.CreatedDate) == DataGen().RemoveMilliSeconds(DataGen().getUtcDateTimeString(examStatusCodeDetails[0].CreatedDateTime.toString()))
        * assert nonPayableEvent.ReceivedDate.split('T')[0] == dbResult.ReceivedDateTime.toString()

        Examples:
        | lab_result | abnormal_indicator | cdiEventHeaderName  | payProvider | statusId     | expectedFailReason                            | exception_message | 
        | 'Positive' | 'A'                | "CDIFailedEvent"    | false       | 20           | "PayProvider is false for the CDIFailedEvent" | ''                |

    @TestCaseKey=ANC-T621
    Scenario Outline: FOBT Provider Pay. Scenario is to drop the file before the cdi event is raised
                      1. (Business rules met) - CDIPassedEvent
                      2. CDIFailedEvent payProvider - true.

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


        # Get the result from the FOBT db table 
        * def dbResult = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = dbResult.OrderCorrelationId
        * match dbResult == '#notnull'

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
        * match resultsEvent.EvaluationId == evaluation.evaluationId
        * match resultsEvent.Determination == <abnormal_indicator>
        * match resultsEvent.Barcode == randomBarcode
        * match resultsEvent.IsBillable == true
        * match resultsEvent.Result[0].Result == <lab_result>
        * match resultsEvent.Result[0].AbnormalIndicator == <abnormal_indicator>

        # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed
        * match examStatusResults[*].FOBTStatusCodeId contains 1

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"FOBT"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate the entry in the ProviderPay table
        # TO VERIFY CORRECT DATA RECORD FIELDS WHEN FOBT ProviderPay WILL BE ON LOWER ENVIRONMENTS
        * def providerPayResult = FobtDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.AddressLineOne == memberDetails.address.address1
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberId == memberDetails.memberId
        * match providerPayResult.CenseoId == memberDetails.censeoId
        * match providerPayResult.ClientId == 14
        * match providerPayResult.AddressLineTwo == memberDetails.address.address2
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.UserName == 'karate'
        * match providerPayResult.FirstName == memberDetails.firstName
        * match providerPayResult.ZipCode == memberDetails.address.zipCode
        * match providerPayResult.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match providerPayResult.City == memberDetails.address.city
        * match providerPayResult.MiddleName == memberDetails.middleName
        * match providerPayResult.AppointmentId == appointment.appointmentId
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.PaymentId != null
        * match providerPayResult.LastName == memberDetails.lastName
        * match providerPayResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the entry using EvaluationId in FOBT & FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed, Status 15 = ProviderPayableEventReceived, Status 17 = ProviderPayRequestSent
        # Status 18 = CdiPassedReceived, Status 19 = CdiFailedWithPayReceived
        * match examStatusResults[*].FOBTStatusCodeId contains 1 && 15 && 17 && <statusId>

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("FOBT_Status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'ProviderPayRequestSent' && 'ProviderPayableEventReceived'

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("providerpay_internal", providerPayResult.PaymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == providerPayResult.PaymentId
        * match paymentRequested["event.providerId"] == providerPayResult.ProviderId
        * match paymentRequested["event.productCode"] == 'FOBT'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * assert paymentRequested["event.dateOfService"] == utcDateTime.split('T')[0]
        * match paymentRequested["event.personId"] == providerPayResult.CenseoId
        * match paymentRequested["event.commonClientId"] == providerPayResult.ClientId
        * match paymentRequested["event.engagementTypeOptions"] == 1
        * match paymentRequested["timestamp"] == '#notnull'

        # Validate that the Kafka event - ProviderPayableEventReceived - was raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == <cdiEventHeaderName>

        # Validate that the Kafka event details are as expected for FOBT_Status
        * json fobtStatus = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000)) 
        * match fobtStatus.EvaluationId == evaluation.evaluationId
        * match fobtStatus.ProviderId == providerDetails.providerId
        * match fobtStatus.MemberPlanId == memberDetails.memberPlanId
        * match fobtStatus.CreatedDate == '#notnull'
        * match fobtStatus.ProviderPayProductCode == 'FOBT'
        * match fobtStatus.ReceivedDate == '#notnull'
        * match fobtStatus.ProductCode == 'FOBT'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * match fobtStatus.PaymentId == providerPayResult.PaymentId

        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTNotPerformed
        * def result = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = result.OrderCorrelationId
        * match result.EvaluationId == evaluation.evaluationId
        * match result.ProviderId == providerDetails.providerId
        * match result.Barcode == randomBarcode
        * match result.ReceivedDateTime.toString() contains dateStamp


        Examples:
        | lab_result | abnormal_indicator | cdiEventHeaderName | payProvider | statusId | exception_message | 
        | 'Positive' | 'A'                | "CDIPassedEvent"   | true        | 18       |''                 |
        #| 'Positive' | 'A'                | "CDIFailedEvent"   | true        | 19       |''                 |
        | 'Negative' | 'N'                | "CDIPassedEvent"   | true        | 18       |''                 |
        #| 'Negative' | 'N'                | "CDIFailedEvent"   | true        | 19       |''                 |

    @ignore
    @TestCaseKey=ANC-T623
    Scenario Outline: FOBT Provider Pay. Scenario is to drop the file before the cdi event is raised
                      1. CDIFailedEvent payProvider - false


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


        # Get the result from the FOBT db table 
        * def dbResult = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def orderCorrelationId = dbResult.OrderCorrelationId
        * match dbResult == '#notnull'

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
        * match resultsEvent.EvaluationId == evaluation.evaluationId
        * match resultsEvent.Determination == <abnormal_indicator>
        * match resultsEvent.Barcode == randomBarcode
        * match resultsEvent.IsBillable == true
        * match resultsEvent.Result[0].Result == <lab_result>
        * match resultsEvent.Result[0].AbnormalIndicator == <abnormal_indicator>

        # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed
        * match examStatusResults[*].FOBTStatusCodeId contains 1

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"FOBT"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate the entry in the ProviderPay table
        * def providerPayResult = FobtDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'

        # Validate the entry using EvaluationId in FOBT & FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = FOBTPerformed, Status 15 = ProviderPayableEventReceived, Status 17 = ProviderPayRequestSent
        # Status 18 = CdiPassedReceived, Status 20 = CdiFailedWithoutPayReceived
        * match examStatusResults[*].FOBTStatusCodeId contains 1 && <statusId>
        * match examStatusResults[*].FOBTStatusCodeId !contains 15
        * match examStatusResults[*].FOBTStatusCodeId !contains 17


        # Validate that a Kafka event related to ProviderPay was not raised
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", dbResult.CenseoId, 10, 5000)
        * assert paymentRequestEvent.length == 0

        # Validate that the Kafka event was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))   
        * match event == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))   
        * match nonPayableEvent == {}

        * def examStatusCodeDetails = FobtDb().getExamStatusForStatusCodeByEvaluationId(evaluation.evaluationId, 16)
        * match examStatusCodeDetails == '#notnull'

        # Validate that the Kafka event - ProviderNonPayableEventReceived - was raised.
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))
        * match nonPayableEvent.EvaluationId == dbResult.EvaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.MemberPlanId == memberDetails.memberPlanId
        * match nonPayableEvent.Reason contains <expectedFailReason>
        * match nonPayableEvent.ProductCode == "FOBT"
        * match DataGen().RemoveMilliSeconds(nonPayableEvent.CreatedDate) == DataGen().RemoveMilliSeconds(DataGen().getUtcDateTimeString(examStatusCodeDetails[0].CreatedDateTime.toString()))
        * assert nonPayableEvent.ReceivedDate.split('T')[0] == dbResult.ReceivedDateTime.toString()

        Examples:
        | lab_result | abnormal_indicator | cdiEventHeaderName  | payProvider | statusId     | expectedFailReason                            | exception_message | 
        | 'Positive' | 'A'                | "CDIFailedEvent"    | false       | 20           | "PayProvider is false for the CDIFailedEvent" | ''                |

    @ignore    
    @TestCaseKey=ANC-T627
    Scenario Outline: FOBT Non-Payable - Not Performed 
        * def randomNotes = Faker().randomQuote()
        * def eventId = DataGen().uuid()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 21112,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 'No'
                },
                {
                    "AnswerId": 30878,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 'Unable to perform'
                },
                {
                    "AnswerId": <answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 30891,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": #(randomNotes)
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
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

        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTNotPerformed.
        * def result = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def not_performed_result = FobtDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match not_performed_result.EvaluationId == evaluation.evaluationId
        * match not_performed_result.MemberPlanId == memberDetails.memberPlanId
        * match not_performed_result.CenseoId == memberDetails.censeoId
        * match not_performed_result.AppointmentId == appointment.appointmentId
        * match not_performed_result.ProviderId == providerDetails.providerId
        * match not_performed_result.FOBTNotPerformedId != null
        * match not_performed_result.Notes == randomNotes


        # Validate that the Kafka event details are as expected  
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))            
    
        * match event.Reason == <expected_reason>
        * match event.ReasonNotes == randomNotes
        * match event.ReasonType == 'Unable to perform'

        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("FOBT_Status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'NotPerformed'

        # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 9 = FOBTNOtPerformed
        * match examStatusResults[*].FOBTStatusCodeId contains only 9

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"FOBT"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate the entry in the ProviderPay table
        * def providerPayResult = FobtDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'
        
        # Validate that a Kafka event related to ProviderPay was not raised
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", result.CenseoId, 10, 5000)
        * assert paymentRequestEvent.length == 0

        # Validate that the Kafka event was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))   
        * match event == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))   
        * match nonPayableEvent == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("FOBT_Status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 5, 1000))
        * match nonPayableEvent == {}

        Examples:
        | answer_id | answer_value                                                     | expected_reason            | cdiEventHeaderName  | payProvider |
        | 30886     | 'Technical issue - please call Mobile Support at 877 570-9359)'  | 'Technical issue'          | "CDIPassedEvent"    | true        |
        | 30886     | 'Technical issue - please call Mobile Support at 877 570-9359)'  | 'Technical issue'          | "CDIFailedEvent"    | true        |
        | 30886     | 'Technical issue - please call Mobile Support at 877 570-9359)'  | 'Technical issue'          | "CDIFailedEvent"    | false       |
        | 30887     | 'Environmental issue'                                            | 'Environmental issue'      | "CDIPassedEvent"    | true        |
        | 30887     | 'Environmental issue'                                            | 'Environmental issue'      | "CDIFailedEvent"    | true        |
        | 30887     | 'Environmental issue'                                            | 'Environmental issue'      | "CDIFailedEvent"    | false       |
        | 30888     | 'No supplies or equipment'                                       | 'No supplies or equipment' | "CDIPassedEvent"    | true        |
        | 30888     | 'No supplies or equipment'                                       | 'No supplies or equipment' | "CDIFailedEvent"    | true        |
        | 30888     | 'No supplies or equipment'                                       | 'No supplies or equipment' | "CDIFailedEvent"    | false       |
        | 30889     | 'Insufficient training'                                          | 'Insufficient training'    | "CDIPassedEvent"    | true        |
        | 30889     | 'Insufficient training'                                          | 'Insufficient training'    | "CDIFailedEvent"    | true        |
        | 30889     | 'Insufficient training'                                          | 'Insufficient training'    | "CDIFailedEvent"    | false       |
        | 50908     | 'Member physically unable'                                       | 'Member physically unable' | "CDIPassedEvent"    | true        |
        | 50908     | 'Member physically unable'                                       | 'Member physically unable' | "CDIFailedEvent"    | true        |
        | 50908     | 'Member physically unable'                                       | 'Member physically unable' | "CDIFailedEvent"    | false       |