@spirometry
@envnot=prod
Feature: Spirometry cdi-events tests. With session grades values D, E, F with Overread required

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", 0)

        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def monthDayYearCdi = DataGen().getMonthDayYear(cdiDateTime)

    @TestCaseKey=ANC-T713
    Scenario Outline: Spirometry Provider Pay.
                      D/E/F where overread required
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
                    "AnswerValue": 100
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 30
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0.3
                },
                {
                    "AnswerId": 50944,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Yes" 
                },        
                {
                    "AnswerId": 50948,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  "No"   
                },
                {
                    "AnswerId": 50952,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Unknown"
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

        * def result = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Publish the Overread event to the overread_spirometry topic
        * string overrreadEventKey = appointment.appointmentId
        * def OverreadId = DataGen().uuid()

        * string overreadHeader = {'Type': 'OverreadProcessed'}
        * string overreadEventValue = {'OverreadId': '#(OverreadId)','MemberId':'','AppointmentId': '#(appointment.appointmentId)','SessionId': '#(OverreadId)','PerformedDateTime': '#(timestamp)','OverreadDateTime': '#(timestamp)','BestTestId': '#(OverreadId)','BestFvcTestId': '#(OverreadId)','BestFvcTestComment': 'TestComment','BestFev1TestId': '#(OverreadId)','BestFev1TestComment': 'TestComment','BestPefTestId': '#(OverreadId)','BestPefTestComment': 'TestComment','Comment': 'TestComment','Fev1FvcRatio':2.5,'OverreadBy': 'JohnDoe','ObstructionPerOverread':<obstructionPerOverread>,'ReceivedDateTime': '#(timestamp)'}
        * kafkaProducerHelper.send("overread_spirometry", overrreadEventKey, overreadHeader, overreadEventValue)
        

        # Validate that overread message data are saved correctly to OverreadResult table
        * def result = SpiroDb().getOverreadResultByAppointmentId(appointment.appointmentId)[0]
        * match result.NormalityIndicatorId      == <normality>


        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"SPIROMETRY"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        

        # Validate the entry using EvaluationId in ProviderPay table for Spiro
        * def providerPayResult = SpiroDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId

        * match providerPayResult.ClientId == 14
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
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
        | session_grade_id | session_grade_value | obstructionPerOverread|  normality  | cdiEventHeaderName| payProvider   | statusId     |
        | 50940            | "D"                 | "YES"                 |  3          | "CDIPassedEvent"  | true          | 13           |
        | 50940            | "D"                 | "YES"                 |  3          | "CDIFailedEvent"  | true          | 14           |
        | 50941            | "E"                 | "NO"                  |  2          | "CDIPassedEvent"  | true          | 13           |
        | 50941            | "E"                 | "NO"                  |  2          | "CDIFailedEvent"  | true          | 14           |
    
    @TestCaseKey=ANC-T714
    Scenario Outline: Spirometry Provider Pay.
                      Non-payable
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
                    "AnswerValue": 100
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 30
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0.3
                },
                {
                    "AnswerId": 50944,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Yes" 
                },        
                {
                    "AnswerId": 50948,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  "No"   
                },
                {
                    "AnswerId": 50952,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Unknown"
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
        
        # Publish the Overread event to the overread_spirometry topic
        * string overrreadEventKey = appointment.appointmentId
        * def OverreadId = DataGen().uuid()

        * string overreadHeader = {'Type': 'OverreadProcessed'}
        * string overreadEventValue = {'OverreadId': '#(OverreadId)','MemberId':'','AppointmentId': '#(appointment.appointmentId)','SessionId': '#(OverreadId)','PerformedDateTime': '#(timestamp)','OverreadDateTime': '#(timestamp)','BestTestId': '#(OverreadId)','BestFvcTestId': '#(OverreadId)','BestFvcTestComment': 'TestComment','BestFev1TestId': '#(OverreadId)','BestFev1TestComment': 'TestComment','BestPefTestId': '#(OverreadId)','BestPefTestComment': 'TestComment','Comment': 'TestComment','Fev1FvcRatio':2.5,'OverreadBy': 'JohnDoe','ObstructionPerOverread':<obstructionPerOverread>,'ReceivedDateTime': '#(timestamp)'}
        * kafkaProducerHelper.send("overread_spirometry", overrreadEventKey, overreadHeader, overreadEventValue)
        
        # Validate that overread message data are saved correctly to OverreadResult table
        * def result = SpiroDb().getOverreadResultByAppointmentId(appointment.appointmentId)[0]
        * match result == '#notnull'

        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"SPIROMETRY"}]}
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        

        # Validate the entry using EvaluationId in ProviderPay table for Spiro
        * def providerPayResult = SpiroDb().getProviderPayResultsWithEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'


        # Validate the entry using EvaluationId in SpirometryExam & SpiroStatus tables
        * def examStatusResults = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatusResults[*].StatusCodeId contains <statusId>
        
         # Validate that a Kafka event - ProviderPayRequestSent - was not raised
         * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))
         * match requestSentEvent == {}
         
         # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
         * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))
          * match payableEvent == {}
         
         # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
         * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 5, 1000))
         * match nonPayableEvent == "#notnull"

        Examples:
        | session_grade_id | session_grade_value | obstructionPerOverread|  normality  | cdiEventHeaderName| payProvider   | statusId     |
        | 50942            | "F"                 | "INCONCLUSIVE"        |  1          | "CDIFailedEvent"  | false         | 15          |
        | 50942            | "F"                 | "INCONCLUSIVE"        |  1          | "CDIPassedEvent"  | true          | 13           |
        | 50942            | "F"                 | "INCONCLUSIVE"        |  1          | "CDIFailedEvent"  | true          | 14           |