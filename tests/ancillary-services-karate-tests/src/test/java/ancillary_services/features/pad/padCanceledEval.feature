@pad
@envnot=prod
Feature: PAD ProviderPay Canceled and Missing Evaluation Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type("helpers.data.DataGen"); return new DataGen(); }
        * def PadDb = function() { var PadDb = Java.type("helpers.database.pad.PadDb"); return new PadDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }

        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def pdfDeliveryDate = DataGen().utcTimestamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", -1)
        * def monthDayYearCdi = DataGen().getMonthDayYear(cdiDateTime)

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @TestCaseKey=ANC-T795
    Scenario Outline: PAD ProviderPay - Evaluation is Canceled without Finalizing. Id - <rowId>
        # Setting product code other than PAD so that the evaluation is created but the cdi events generated by cdi does not have PAD. This will lead to 'exam not found' scenario.
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'CKD'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29560,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 29564,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<left_float_result>"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_float_result>"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": "#(dateStamp)"
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

        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/cancelEval.feature")
       
        # Validate that no entry was made into the PAD table
        * def evalResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId, 3, 500)[0]
        * match evalResult == "#null"
        
        # Publish the cdi event to the cdi_events topic as the events raised by cdi service have CKD instead of PAD
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
         # Including PAD in product code so that PM processes the event
         * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"PAD"}]}
        
        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate that there is no entry in the ProviderPay table
        * def providerPayResult = PadDb().getProviderPayByEvalId(evaluation.evaluationId, 3, 500)[0]
        * match providerPayResult == "#null"
        
        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))   
        * match event == {}
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))   
        * match nonPayableEvent == {}
                
        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added

        Examples:
            | rowId | left_float_result | left_normality | right_float_result   | right_normality | determination | cdiEventHeaderName| payProvider |
            | 1     | 0.3               | "A"            | 0.6                  | "A"             | "A"           | "CDIPassedEvent"  | true        |
            | 2     | 0.3               | "A"            | 0.6                  | "A"             | "A"           | "CDIFailedEvent"  | true        |
            | 3     | 99                | "U"            | 0.3                  | "A"             | "A"           | "CDIPassedEvent"  | true        |
            | 4     | 99                | "U"            | 0.3                  | "A"             | "A"           | "CDIFailedEvent"  | true        |
            | 5     | 99                | "U"            | 1                    | "N"             | "N"           | "CDIPassedEvent"  | true        |
            | 6     | 99                | "U"            | 1                    | "N"             | "N"           | "CDIFailedEvent"  | true        |
            | 7     | 0.3               | "A"            | 1                    | "N"             | "A"           | "CDIPassedEvent"  | true        |
            | 8     | 0.3               | "A"            | 1                    | "N"             | "A"           | "CDIFailedEvent"  | true        |
            | 9     | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIPassedEvent"  | true        |
            | 10    | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIFailedEvent"  | true        |
            | 11    | 99                | "U"            | 99                   | "U"             | "U"           | "CDIPassedEvent"  | true        |
            | 12    | 99                | "U"            | 99                   | "U"             | "U"           | "CDIFailedEvent"  | true        |
            | 13    | 0.3               | "A"            | 99                   | "U"             | "A"           | "CDIFailedEvent"  | false       |
            | 14    | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIFailedEvent"  | false       |
            | 15    | 99                | "U"            | 99                   | "U"             | "U"           | "CDIFailedEvent"  | false       |

    # The test sends a EvaluationFinalizedEvent without PAD product code thus creating a missing evaluation scenario
    @TestCaseKey=ANC-T796
    Scenario Outline: PAD ProviderPay - Converted Evaluations - Evaluation is Finalized after Canceling. Id - <rowId>
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29560,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 29564,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<left_float_result>"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_float_result>"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": "#(dateStamp)"
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

        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/cancelEval.feature")        
       
        # Validate that no entry was made into the PAD table since the finalized event has not been published yet
        # This also gives the PM a few seconds to process the CDI event sent by cdi service
        * def evalResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId, 4, 1000)[0]
        * match evalResult == "#null"
        
        * karate.call("classpath:helpers/eval/finalizeEval.feature")

        # Validate that an entry was made into the PAD table since the finalized event is raised 
        * def evalResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#notnull"
        
        ## UnCommenting as the cdi service sends events but that might be CDIFailedEvent with PayProvider false
        # Publish the cdi event to the cdi_events topic as cdi service might not for Canceled Evaluations
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        # Including PAD in product code so that PM processes the event. Explicitly publishing the event as the one published by cdi might be published with delay
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"PAD"}]}

        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate that there is entry in the ProviderPay table
        * def providerPayResult = PadDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == "#notnull"
        
        # Validate that the Kafka event - ProviderPayRequestSent - was raised
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 500))   
        * match event != {}
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 500))   
        * match nonPayableEvent != {}
        
        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added

        Examples:
            | rowId | left_float_result | left_normality | right_float_result   | right_normality | determination | cdiEventHeaderName| payProvider | expectedCdiStatus             |
            | 1     | 0.3               | "A"            | 0.6                  | "A"             | "A"           | "CDIPassedEvent"  | true        | "CdiPassedReceived"           |
            | 2     | 0.3               | "A"            | 0.6                  | "A"             | "A"           | "CDIFailedEvent"  | true        | "CdiFailedWithPayReceived"    |
            | 3     | 99                | "U"            | 0.3                  | "A"             | "A"           | "CDIPassedEvent"  | true        | "CdiPassedReceived"           |
            | 4     | 99                | "U"            | 0.3                  | "A"             | "A"           | "CDIFailedEvent"  | true        | "CdiFailedWithPayReceived"    |
            | 5     | 99                | "U"            | 1                    | "N"             | "N"           | "CDIPassedEvent"  | true        | "CdiPassedReceived"           |
            | 6     | 99                | "U"            | 1                    | "N"             | "N"           | "CDIFailedEvent"  | true        | "CdiFailedWithPayReceived"    |
            | 7     | 0.3               | "A"            | 1                    | "N"             | "A"           | "CDIPassedEvent"  | true        | "CdiPassedReceived"           |
            | 8     | 0.3               | "A"            | 1                    | "N"             | "A"           | "CDIFailedEvent"  | true        | "CdiFailedWithPayReceived"    |
            | 9     | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIPassedEvent"  | true        | "CdiPassedReceived"           |
            | 10    | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIFailedEvent"  | true        | "CdiFailedWithPayReceived"    |
    
    # The test sends a EvaluationFinalizedEvent without PAD product code thus creating a missing evaluation scenario
    @TestCaseKey=ANC-T797
    Scenario Outline: PAD ProviderPay - Missing Evaluations - Evaluation is Finalized but never Canceled. Id - <rowId>
        # Setting product code other than PAD so that the evaluation is not captured and added to PAD database. This will lead to a missing evaluation scenario.
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'CKD'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29560,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 29564,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<left_float_result>"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_float_result>"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": "#(dateStamp)"
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

        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")
        
        # Validate that no entry was made into the PAD table since the finalized event did not contain PAD product code
        * def evalResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId, 3, 500)[0]
        * match evalResult == "#null"
        
        # Publish the cdi event to the cdi_events topic as the events raised by cdi service have CKD instead of PAD
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        # Including PAD in product code so that PM processes the event
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"PAD"}]}

        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate that there is no entry in the ProviderPay table
        * def providerPayResult = PadDb().getProviderPayByEvalId(evaluation.evaluationId, 3, 500)[0]
        * match providerPayResult == "#null"
        
        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 500))   
        * match event == {}
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 500))   
        * match nonPayableEvent == {}

        ## Additional checks on error queue count increasing to be added
        ## Additional checks on New Relic dashboard updates to be added

        Examples:
            | rowId | left_float_result | left_normality | right_float_result   | right_normality | determination | cdiEventHeaderName| payProvider |
            | 1     | 0.3               | "A"            | 0.6                  | "A"             | "A"           | "CDIPassedEvent"  | true        |
            | 2     | 0.3               | "A"            | 0.6                  | "A"             | "A"           | "CDIFailedEvent"  | true        |
            | 3     | 99                | "U"            | 0.3                  | "A"             | "A"           | "CDIPassedEvent"  | true        |
            | 4     | 99                | "U"            | 0.3                  | "A"             | "A"           | "CDIFailedEvent"  | true        |
            | 5     | 99                | "U"            | 1                    | "N"             | "N"           | "CDIPassedEvent"  | true        |
            | 6     | 99                | "U"            | 1                    | "N"             | "N"           | "CDIFailedEvent"  | true        |
            | 7     | 0.3               | "A"            | 1                    | "N"             | "A"           | "CDIPassedEvent"  | true        |
            | 8     | 0.3               | "A"            | 1                    | "N"             | "A"           | "CDIFailedEvent"  | true        |
            | 9     | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIPassedEvent"  | true        |
            | 10    | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIFailedEvent"  | true        |
            | 11    | 99                | "U"            | 99                   | "U"             | "U"           | "CDIPassedEvent"  | true        |
            | 12    | 99                | "U"            | 99                   | "U"             | "U"           | "CDIFailedEvent"  | true        |
            | 13    | 0.3               | "A"            | 99                   | "U"             | "A"           | "CDIFailedEvent"  | false       |
            | 14    | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIFailedEvent"  | false       |
            | 15    | 99                | "U"            | 99                   | "U"             | "U"           | "CDIFailedEvent"  | false       |