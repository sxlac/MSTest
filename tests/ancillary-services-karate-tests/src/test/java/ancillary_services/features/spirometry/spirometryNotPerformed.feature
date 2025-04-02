@spirometry
@envnot=prod
Feature: Spirometry Not Performed

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def SpirometryDb = function() { var SpirometryDb = Java.type("helpers.database.spirometry.SpirometryDb"); return new SpirometryDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        
    @TestCaseKey=ANC-T399
    Scenario Outline: Spirometry Not Performed - Member Unable to Perform
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
                    'AnswerId': 50922,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Unable to perform'
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

        # Validate that the database details are as expected
        * def result = SpirometryDb().getNotPerformedByEvaluationId(evaluation.evaluationId)[0]
        * match result.EvaluationId == evaluation.evaluationId
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.ExamNotPerformedId != null
        * match result.NotPerformedReasonId == <expected_not_performed_reason_id>
        * match result.Notes == randomNotes
        * match result.CenseoId == memberDetails.censeoId
        * match result.AppointmentId == appointment.appointmentId
        * match result.ProviderId == providerDetails.providerId

        # Validate that the Kafka event details are as expected
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
        * match event.ReasonType == 'Unable to perform'
        * match event.Reason == <expected_reason>
        * match event.ReasonNotes == randomNotes
        * match event.ProductCode == 'SPIROMETRY'
        * match event.EvaluationId == evaluation.evaluationId

        # Validate that the Kafka event details are as expected
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("spirometry_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'NotPerformed'

        Examples:
            | answer_id | answer_value                                                     | expected_reason                     | expected_not_performed_reason_id |
            | 50928     | 'Technical issue'                                                | 'Technical issue'                   | 5                                |
            | 50929     | 'Environmental issue'                                            | 'Environmental issue'               | 6                                |
            | 50930     | 'No supplies or equipment'                                       | 'No supplies or equipment'          | 7                                |
            | 50931     | 'Insufficient training'                                          | 'Insufficient training'             | 8                                |
            | 50932     | 'Member physically unable'                                       | 'Member physically unable'          | 9                                |
            | 51960     | 'Member outside demographic ranges'                              | 'Member outside demographic ranges' | 10                               |


        @TestCaseKey=ANC-T402
        Scenario Outline: Spirometry Not Performed - Member Refused
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
    
            # Validate that the database details are as expected
            * def result = SpirometryDb().getNotPerformedByEvaluationId(evaluation.evaluationId)[0]

            * match result.EvaluationId == evaluation.evaluationId
            * match result.MemberPlanId == memberDetails.memberPlanId
            * match result.ExamNotPerformedId != null
            * match result.Notes == randomNotes
            * match result.CenseoId == memberDetails.censeoId
            * match result.AppointmentId == appointment.appointmentId
            * match result.ProviderId == providerDetails.providerId 
            
            # Validate that the Kafka event details are as expected
            * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))
            * match event.ReasonType == 'Member refused'
            * match event.Reason == <expected_reason>
            * match event.ReasonNotes == randomNotes
            * match event.ProductCode == 'SPIROMETRY'
            * match event.EvaluationId == evaluation.evaluationId

            # Validate that the Kafka event details are as expected
            * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("spirometry_status", evaluation.evaluationId + '', 10, 5000)
            * match headers contains 'NotPerformed'

            Examples:
                | answer_id | answer_value                | expected_reason             | expected_not_performed_reason_id |
                | 50923     | 'Member recently completed' | 'Member recently completed' | 1                                |
                | 50924     | 'Scheduled to complete'     | 'Scheduled to complete'     | 2                                |
                | 50925     | 'Member apprehension'       | 'Member apprehension'       | 3                                |
                | 50926     | 'Not interested'            | 'Not interested'            | 4                                |