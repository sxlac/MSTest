@dee
@envnot=prod
Feature: DEE Lab Not Performed Tests
    # See https://cvs-hcd.atlassian.net/wiki/spaces/AncillarySvcs/pages/51220985/DEE+Form+Questions for answer definitions

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def DeeDb = function() { var DeeDb = Java.type("helpers.database.dee.DeeDb"); return new DeeDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'DEE'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
    
    @TestCaseKey=ANC-T679    
    Scenario Outline: DEE Not Performed - Provider Unable to Perform without Notes
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
        
        * match event.ReasonType == 'Unable to perform'
        * match event.Reason == <expected_reason>
        * match event.ReasonNotes == ''
        * match event.ProductCode == 'DEE'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == memberDetails.memberPlanId
        * match event.ProviderId == providerDetails.providerId
        * match event.CreateDate contains dateStamp
        * match event.ReceivedDate contains dateStamp
        * match event.CreateDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreateDate) == DataGen().getUtcDateTimeString(result.CreatedDateTime.toString())

        Examples:
            | answer_id | answer_value                                                     | expected_reason            |
            | 30950     | 'Technical issue (please call Mobile Support at (877) 570-9359)' | 'Technical issue'          |
            | 30951     | 'Environmental issue'                                            | 'Environmental issue'      |
            | 30952     | 'No supplies or equipment'                                       | 'No supplies or equipment' |
            | 30953     | 'Insufficient training'                                          | 'Insufficient training'    |
            | 50914     | 'Member physically unable'                                       | 'Member physically unable' |

    @TestCaseKey=ANC-T680
    Scenario Outline: DEE Not Performed - Member Refused without Notes
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
        * match event.CreateDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreateDate) == DataGen().getUtcDateTimeString(result.CreatedDateTime.toString())

        Examples:
            | answer_id | answer_value                | expected_reason             |
            | 30943     | 'Member recently completed' | 'Member recently completed' |
            | 30944     | 'Scheduled to complete'     | 'Scheduled to complete'     |
            | 30945     | 'Member apprehension'       | 'Member apprehension'       |
            | 30946     | 'Not interested'            | 'Not interested'            |
            | 30947     | 'Other'                     | 'Other'                     |

     @TestCaseKey=ANC-T387
    @TestCaseKey=ANC-T388
    Scenario Outline: DEE Not Performed - With Notes
            * def randomNotes = Faker().randomQuote()
            * def testNotes = Faker().randomQuote()
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
                        'AnswerId': <notes_id>,
                        'AnsweredDateTime': '#(dateStamp)',
                        'AnswerValue': #(randomNotes)
                    },
                    {
                        'AnswerId': 50415,
                        'AnsweredDateTime': '#(dateStamp)',
                        'AnswerValue': #(testNotes)
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
            * match result.Reason == 'Other'
            * match result.RetinalImageTestingNotes == testNotes
    
            # Get and check Kafka results
            * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("dee_status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))             
            * match event.ReasonType == <refusal_type>
            * match event.Reason == 'Other'
            * match event.ReasonNotes == randomNotes
            * match event.ProductCode == 'DEE'
            * match event.EvaluationId == evaluation.evaluationId
            * match event.MemberPlanId == memberDetails.memberPlanId
            * match event.ProviderId == providerDetails.providerId
            * match event.RetinalImageTestingNotes == testNotes
            * match event.CreateDate contains dateStamp
            * match event.ReceivedDate contains dateStamp
            * match event.CreateDate.toString().split('+')[1] == "00:00"
            * match event.ReceivedDate.toString().split('+')[1] == "00:00"
            * match DataGen().RemoveMilliSeconds(event.CreateDate) == DataGen().getUtcDateTimeString(result.CreatedDateTime.toString())
    
            Examples:
                | answer_id | notes_id | refusal_type        |
                | 30947     | 52850    | 'Member refused'    |
                | 52851     | 52852    | 'Unable to perform' |

