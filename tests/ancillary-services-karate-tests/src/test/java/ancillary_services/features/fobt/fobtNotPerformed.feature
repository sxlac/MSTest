@fobt
@envnot=prod
Feature: FOBT Exam NotPerformed 

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def FobtDb = function() { var FobtDb = Java.type('helpers.database.fobt.FobtDb'); return new FobtDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'FOBT'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        
    @TestCaseKey=ANC-T347
    Scenario Outline: FOBT Not Performed - Member refused
        * def randomNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 21112,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 'No'
                },
                {
                    "AnswerId": 30877,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 'Member refused'
                },
                {
                    "AnswerId": <answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>
                },
                {
                    "AnswerId": 30884,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": #(randomNotes)
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

        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTNotPerformed
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
        * match event.ReasonType == 'Member refused'

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("FOBT_Status", evaluation.evaluationId + '', 10, 5000)
    
        * match headers contains 'NotPerformed'

        # Validate the entry using EvaluationId in FOBT and FOBTStatus tables
        * def examStatusResults = FobtDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 9 = FOBTNOtPerformed
        * match examStatusResults[*].FOBTStatusCodeId contains only 9

        Examples:
            | answer_id | answer_value                 | expected_reason             |
            | 30883     | 'Other'                      | 'Other'                     | 
            | 30882     | 'Not interested'             | 'Not interested'            |
            | 30881     | 'Member apprehension'        | 'Member apprehension'       |
            | 30880	    | 'Scheduled to complete'      | 'Scheduled to complete'     |
            | 30879     | 'Member recently completed'  | 'Member recently completed' |

        @TestCaseKey=ANC-T345
    Scenario Outline: FOBT Not Performed - Unable to perform
        * def randomNotes = Faker().randomQuote()
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

        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTNotPerformed
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

        Examples:
        | answer_id | answer_value                                                     | expected_reason                                                          |
        | 30886     | 'Technical issue - please call Mobile Support at 877 570-9359)'  | 'Technical issue'                                                        |
        | 30887     | 'Environmental issue'                                            | 'Environmental issue'                                                    |
        | 30888     | 'No supplies or equipment'                                       | 'No supplies or equipment'                                               |
        | 30889     | 'Insufficient training'                                          | 'Insufficient training'                                                  |
        | 50908     | 'Member physically unable'                                       | 'Member physically unable'                                               |