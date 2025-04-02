# Iris GradeAsNormal API is not functional. All tests with Iris integrations will need to be tested manually until issue is resolved. 
@ignore
# @dee
@envnot=prod
@parallel=false
Feature: DEE Billing Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def DeeDb = function() { var DeeDb = Java.type("helpers.database.dee.DeeDb"); return new DeeDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", 0)

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'DEE'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        
    @TestCaseKey=ANC-T682
    Scenario Outline:  DEE Provider Pay. IRIS and then CDI event sent. (Business rules met)
                       1. CDIPassedEvent
                       2. CDIFailedEvent payProvider - true
        * def image1 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
        * def image2 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-2.txt')
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29554,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 28377,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "John"
                },
                {
                    "AnswerId": 28378,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Doe"
                },
                {
                    "AnswerId": 30974,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "M"
                },
                {
                    "AnswerId": 28383,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "TX"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "5AA52E97-D999-4093-BF1B-7AE171C2DFBC",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image1)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "B5C78B69-1A5C-40F6-B53A-306F0E1A54C6",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image2)"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
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
        # Needed due to there being a 10 second delay between CreateDEE command when message is transferred to ProcessDEE
        # * eval sleep(10000)        


        # * def exam = karate.call('classpath:/helpers/iris/exam/getExam.feature')
        # * karate.call('classpath:helpers/iris/exam/gradeExamAsNormal.feature', exam)

        * def evalResult = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"DEE"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate the entry using EvaluationId in ProviderPay table for DEE
        * def providerPayResult = DeeDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.Gradeable == "#notnull"
        * match providerPayResult.ClientId == 14
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.PaymentId != null

        # # Validate the entry using EvaluationId in DEE & DEEStatus tables
        * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 18 = DEEPerformed, Status 23 = ProviderPayableEventReceived, Status 25 = ProviderPayRequestSent
        # Status 26 = CdiPassedReceived, Status 27 = CdiFailedWithPayReceived
        * match examStatusResults[*].ExamStatusCodeId contains 18 && <statusId> && 23 && 25

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match requestSentEvent.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))
        * match payableEvent.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("providerpay_internal", providerPayResult.PaymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == providerPayResult.PaymentId
        * match paymentRequested["event.providerId"] == providerPayResult.ProviderId
        * match paymentRequested["event.productCode"] == 'DEE'
        * match paymentRequested["event.commonClientId"] == providerPayResult.ClientId
        * match paymentRequested["event.engagementTypeOptions"] == 1
        * match paymentRequested["timestamp"] == '#notnull'
        * match paymentRequested["event.additionalDetails.evaluationId"] == evaluation.evaluationId.toString()
        * match paymentRequested["event.additionalDetails.examId"] == evalResult.ExamId.toString()

        Examples:
        | cdiEventHeaderName | payProvider | statusId     |
        | "CDIPassedEvent"   | true        | 26           |
        | "CDIFailedEvent"   | true        | 27           |


    @TestCaseKey=ANC-T681
    Scenario Outline:  DEE Provider Pay. CDI event sent first and then IRIS. (Business rules met)
                       1. CDIPassedEvent
                       2. CDIFailedEvent payProvider - true
        * def image1 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
        * def image2 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-2.txt')
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29554,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 28377,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "John"
                },
                {
                    "AnswerId": 28378,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Doe"
                },
                {
                    "AnswerId": 30974,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "M"
                },
                {
                    "AnswerId": 28383,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "TX"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "5AA52E97-D999-4093-BF1B-7AE171C2DFBC",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image1)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "B5C78B69-1A5C-40F6-B53A-306F0E1A54C6",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image2)"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
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

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"DEE"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Needed due to there being a 10 second delay between CreateDEE command when message is transferred to ProcessDEE
        # * eval sleep(10000)        


        # * def exam = karate.call('classpath:/helpers/iris/exam/getExam.feature')
        # * karate.call('classpath:helpers/iris/exam/gradeExamAsNormal.feature', exam)

        * def evalResult = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Validate the entry using EvaluationId in ProviderPay table for DEE
        * def providerPayResult = DeeDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.Gradeable == "#notnull"
        * match providerPayResult.ClientId == 14
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.PaymentId != null

        # # Validate the entry using EvaluationId in DEE & DEEStatus tables
        * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 18 = DEEPerformed, Status 23 = ProviderPayableEventReceived, Status 25 = ProviderPayRequestSent
        # Status 26 = CdiPassedReceived, Status 27 = CdiFailedWithPayReceived
        * match examStatusResults[*].ExamStatusCodeId contains 18 && <statusId> && 23 && 25

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match requestSentEvent.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))
        * match payableEvent.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("providerpay_internal", providerPayResult.PaymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == providerPayResult.PaymentId
        * match paymentRequested["event.providerId"] == providerPayResult.ProviderId
        * match paymentRequested["event.productCode"] == 'DEE'
        * match paymentRequested["event.commonClientId"] == providerPayResult.ClientId
        * match paymentRequested["event.engagementTypeOptions"] == 1
        * match paymentRequested["timestamp"] == '#notnull'
        * match paymentRequested["event.additionalDetails.evaluationId"] == evaluation.evaluationId.toString()
        * match paymentRequested["event.additionalDetails.examId"] == evalResult.ExamId.toString()

        Examples:
        | cdiEventHeaderName | payProvider | statusId     |
        | "CDIPassedEvent"   | true        | 26           |
        | "CDIFailedEvent"   | true        | 27           |

    @TestCaseKey=ANC-T683
    Scenario Outline: DEE Not Performed - Member Refused
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 29555,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '2'
                },
                {
                    'AnswerId': 28377,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(memberDetails.firstName)'
                },
                {
                    'AnswerId': 28378,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(memberDetails.lastName)'
                },
                {
                    'AnswerId': 30974,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(memberDetails.gender)'
                },
                {
                    'AnswerId': 28383,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(memberDetails.address.state)'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(dateStamp)',
                    'AnswerValue': <answer_value>
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

        # Verify not performed details
        * def result = DeeDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.AnswerId == <answer_id>
        * match result.Reason == <expected_reason>

        # Get and check Kafka results 
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))             
        
        * match event.ReasonType == 'Member refused'
        * match event.Reason == <expected_reason>
        * match event.ReasonNotes == ''
        * match event.ProductCode == 'DEE'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == memberDetails.memberPlanId
        * match event.ProviderId == providerDetails.providerId
        * match event.CreateDate contains dateStamp
        * match event.ReceivedDate contains dateStamp

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"DEE"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate the entry using EvaluationId in DEE & DEEStatus tables
        * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 19 = DEE Not Performed
        * match examStatusResults[*].ExamStatusCodeId contains 19

        # Validate that a Kafka event - ProviderPayRequestSent - was not raised
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))
        * match requestSentEvent == {}
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))
         * match payableEvent == {}
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 5, 1000))
        * match nonPayableEvent == {}

        Examples:
            | answer_id | answer_value                | expected_reason             | cdiEventHeaderName | payProvider |
            | 30943     | 'Member recently completed' | 'Member recently completed' | "CDIPassedEvent"   | true        |
            | 30944     | 'Scheduled to complete'     | 'Scheduled to complete'     | "CDIFailedEvent"   | false       |
            | 30945     | 'Member apprehension'       | 'Member apprehension'       | "CDIPassedEvent"   | true        |
            | 30946     | 'Not interested'            | 'Not interested'            | "CDIFailedEvent"   | false       |
            | 30947     | 'Other'                     | 'Other'                     | "CDIPassedEvent"   | true        |
            | 30946     | 'Not interested'            | 'Not interested'            | "CDIFailedEvent"   | true       |
