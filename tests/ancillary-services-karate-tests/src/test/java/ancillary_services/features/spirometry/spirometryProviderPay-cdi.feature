@spirometry
@envnot=prod
@parallel=false
Feature: Spirometry CDI events based ProviderPay tests

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
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def monthDayYearCdi = DataGen().getMonthDayYear(cdiDateTime)

    @TestCaseKey=ANC-T644
    Scenario Outline: Spirometry  Provider Pay. (Business rules met)
                      1. CDIPassedEvent
                      2. CDIFailedEvent payProvider - true

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
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.ClientId == 14
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.PaymentId != null

        # Validate the entry using EvaluationId in SpirometryExam & SpiroStatus tables
        * def examStatusResults = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = Spirometry Exam Performed, Status 10 = ProviderPayableEventReceived, Status 11 = ProviderPayRequestSent
        # Status 13 = CdiPassedReceived, Status 14 = CdiFailedWithPayReceived
        * match examStatusResults[*].StatusCodeId contains 1 && <statusId> && 10 && 11

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match requestSentEvent.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))
        * match payableEvent.EvaluationId == evaluation.evaluationId

        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc |symptom_support_answer_id| symptom_support_answer_value | risk_factors_answer_id | risk_factors_answer_value | comorbidity_answer_id |comorbidity_answer_value | normality  | cdiEventHeaderName| payProvider   | statusId     |
        | 50938            | "B"                 | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Abnormal" | "CDIPassedEvent"  | true          | 13           |
        | 50938            | "B"                 | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Abnormal" | "CDIFailedEvent"  | true          | 14           |
        | 50938            | "B"                 | 70  | 100  | 0.7      | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Normal"   | "CDIFailedEvent"  | true          | 14           |
        | 51947            | "A"                 | 70  | 100  | 0.7      | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Normal"   | "CDIPassedEvent"  | true          | 13           |


    @TestCaseKey=ANC-T715
    Scenario Outline: Spirometry Non-Payable  -  Not Performed 
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 50920,
                    'AnsweredDateTime': '#(timestamp)',
                    "AnswerValue": 'No'
                },
                {
                    'AnswerId': 50921,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Member refused'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 50927,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomNotes)
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

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"SPIROMETRY"}]}
        
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate that the database details are as expected
        * def result = SpiroDb().getNotPerformedByEvaluationId(evaluation.evaluationId)[0]
        
        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match event.ReasonType == 'Member refused'
        * match event.Reason == <expected_reason>
        * match event.ReasonNotes == randomNotes
        * match event.ProductCode == 'SPIROMETRY'
        * match event.EvaluationId == evaluation.evaluationId


         # Validate the entry using EvaluationId in Spiro & Status tables
        * def examStatusResults = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
         # Status 2 = Spiro Not Performed
         * match examStatusResults[*].StatusCodeId contains 2
 
         # Validate that a Kafka event - ProviderPayRequestSent - was not raised
         * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))
         * match requestSentEvent == {}
         
         # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
         * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))
          * match payableEvent == {}
         
         # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
         * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 5, 1000))
         * match nonPayableEvent == {}       

        Examples:
        | answer_id | answer_value                | expected_reason             | expected_not_performed_reason_id | cdiEventHeaderName | payProvider|
        | 50923     | 'Member recently completed' | 'Member recently completed' | 1                                | "CDIPassedEvent"   | true       |
        | 50924     | 'Scheduled to complete'     | 'Scheduled to complete'     | 2                                | "CDIFailedEvent"   | false      |
        | 50925     | 'Member apprehension'       | 'Member apprehension'       | 3                                | "CDIFailedEvent"   | true       |

    
    @TestCaseKey=ANC-T716
    Scenario Outline: Spirometry Normality Validation
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
                    "AnswerId": 51405,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },        
                {
                    "AnswerId": 51410,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },
                {
                    "AnswerId": 51415,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Never"
                },
                {
                    "AnswerId": 51420,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(dateStamp)"
                },
                {
                    "AnswerId": <Hx_of_COPD_AID>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <Hx_of_COPD_answer_value>
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

        # Validate the entry using EvaluationId in SpirometryExamResults table for Spiro
        * def result = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.Normality == <normality>
        * match result.HasHistoryOfCopd == <HasHistoryOfCopd>  
            
        # Validate the entry using EvaluationId in ProviderPay table for Spiro
        * def providerPayResult = SpiroDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.ClientId == 14
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.PaymentId != null

        # Validate the entry using EvaluationId in SpirometryExam & SpiroStatus tables
        * def examStatusResults = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = Spirometry Exam Performed, Status 10 = ProviderPayableEventReceived, Status 11 = ProviderPayRequestSent
        # Status 13 = CdiPassedReceived, Status 14 = CdiFailedWithPayReceived
        * match examStatusResults[*].StatusCodeId contains 1 && <statusId> && 10 && 11

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match requestSentEvent.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))
        * match payableEvent.EvaluationId == evaluation.evaluationId

        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc | normality  |Hx_of_COPD_AID|Hx_of_COPD_answer_value                       |HasHistoryOfCopd| cdiEventHeaderName| payProvider   | statusId     |
        | 50938            | "B"                 | 70  | 70   | 0.65     | "Abnormal" |29614         |"Chronic obstructive pulmonary disease (COPD)"|true            | "CDIPassedEvent"  | true          | 13           |
        | 50937            | "A"                 | 80  | 80   | 0.7      | "Normal"   |21925         |"Chronic obstructive pulmonary disease (COPD)"|true            | "CDIFailedEvent"  | true          | 14           |
