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
        * def cdiDateTime = DataGen().utcTimestampWithOffset(10)

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'DEE'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
    
    @TestCaseKey=ANC-T684   
    Scenario Outline:  Scenario: DEE Only 1 Image Submitted to Iris.
                       Non Payable

        * def image3 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
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
                    "AnswerRowId": "EF1BFDA7-C1EA-4DA1-9C0E-4892CADCEE70",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image3)"
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
        
        # Publish the cdi event to the cdi_events topic for DEE
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId": "#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"DEE"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)


        # Validate the entry using EvaluationId in DEE & DEEStatus tables
        * def examStatusResults = DeeDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 24 = DEE Not Performed
        * match examStatusResults[*].ExamStatusCodeId contains <statusId>

        # Validate that a Kafka event - ProviderPayRequestSent - was not raised
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))
        * match requestSentEvent == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))
        * match payableEvent == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 5, 1000))
        * match nonPayableEvent == "#notnull"


        Examples:
        | cdiEventHeaderName | payProvider | statusId     |
        | "CDIPassedEvent"   | true        | 24           |
        | "CDIFailedEvent"   | true        | 24           |
        | "CDIFailedEvent"   | False       | 24           |