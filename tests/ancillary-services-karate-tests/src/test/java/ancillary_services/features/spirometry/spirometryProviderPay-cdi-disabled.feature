@ignore
@spirometry
@envnot=prod
@parallel=false
Feature: Spirometry CDI events based ProviderPay tests. Check if feature is disabled and no entries are made.

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", 0)
        
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

        * def monthDayYearCdi = DataGen().getMonthDayYear(cdiDateTime)
    
    @TestCaseKey=ANC-T644
    Scenario Outline: Spirometry  Provider Pay. Check if feature is disabled and no entries are made.

        * set evaluation.answers =
        """
            [
                {
                    "AnswerId": 50919,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": <session_grade_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <session_grade_value>
                },
                {
                    "AnswerId": 50999,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fvc>
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1>
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1_fvc>
                },
                {
                    "AnswerId": <symptom_support_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <symptom_support_answer_value>
                },        
                {
                    "AnswerId": <risk_factors_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <risk_factors_answer_value>
                },
                {
                    "AnswerId": <comorbidity_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <comorbidity_answer_value>
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
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"SPIROMETRY"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
            
        * def evalResult = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]  
            

        # Validate the entry using EvaluationId in ProviderPay table for Spiro
        * def providerPayResult = SpiroDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult  == '#null'

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 1000))
        * match requestSentEvent == {}

        # Validate that the Kafka event - ProviderNonPayableEventReceived - was raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 1000))   
        * match nonPayableEvent == {}

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 1000))
        * match payableEvent == {}

        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc |symptom_support_answer_id| symptom_support_answer_value | risk_factors_answer_id | risk_factors_answer_value | comorbidity_answer_id |comorbidity_answer_value | normality  | cdiEventHeaderName| payProvider   |
        | 50938            | "B"                 | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Abnormal" | "CDIPassedEvent"  | true          |
        | 50938            | "B"                 | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Abnormal" | "CDIFailedEvent"  | true          |
        | 50938            | "B"                 | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Abnormal" | "CDIFailedEvent"  | false          |
