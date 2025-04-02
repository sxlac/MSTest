@pad
@envnot=prod
Feature: PAD Evaluation Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def PadDb = function() { var PadDb = Java.type('helpers.database.pad.PadDb'); return new PadDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')

    @TestCaseKey=ANC-T354
    Scenario Outline: PAD Not Performed - Provider Unable to Perform- With Reason Notes Answer
        * def reasonNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 29561,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '2'
                },
                {
                    'AnswerId': 30958,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Unable to perform'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 30971,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(reasonNotes)
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
        * def result = PadDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.LeftScoreAnswerValue == null
        * match result.LeftSeverityAnswerValue == null
        * match result.RightScoreAnswerValue == null
        * match result.RightSeverityAnswerValue == null
        * match result.NotPerformedId != null
        * match result.AnswerId == <answer_id>
        * match result.Notes == reasonNotes

        # Verify the status from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'PADNotPerformed'

        # Validate that the Kafka event details are as expected  
        * json padStatus = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))    
        * match padStatus.ProviderId == providerDetails.providerId
        * match padStatus.ReceivedDate contains dateStamp 
        * match padStatus.ReasonNotes == reasonNotes
        * match padStatus.ProductCode == 'PAD'
        * match padStatus.ReasonType == 'Unable to perform' 
        * match padStatus.CreateDate contains dateStamp 
        * match padStatus.Reason == <expected_reason>
        * match padStatus.MemberPlanId == memberDetails.memberPlanId
    
        Examples:
            | answer_id | answer_value                                                     | expected_reason            |
            | 30966     | 'Technical issue (please call Mobile Support at (877) 570-9359)' | 'Technical issue'          |
            | 30967     | 'Environmental issue'                                            | 'Environmental issue'      |
            | 30968     | 'No supplies or equipment'                                       | 'No supplies or equipment' |
            | 30969     | 'Insufficient training'                                          | 'Insufficient training'    |
            | 50917     | 'Member physically unable'                                       | 'Member physically unable' |
    
    @TestCaseKey=ANC-T520
    Scenario: PAD Not Performed - Not Clinically Relevant
        * def reasonNotes = Faker().randomQuote()
        * set evaluation.answers =
                """
                [
                    {
                        'AnswerId': 29561,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': '2'
                    },
                    {
                        'AnswerId': 31125,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': 'Not clinically relevant'
                    },
                    {
                        'AnswerId': 31126,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': #(reasonNotes)
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
        * def result = PadDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.LeftScoreAnswerValue == null
        * match result.LeftSeverityAnswerValue == null
        * match result.RightScoreAnswerValue == null
        * match result.RightSeverityAnswerValue == null
        * match result.NotPerformedId != null
        * match result.AnswerId == 31125
        * match result.Notes == reasonNotes    
    
        # Verify the status from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'PADNotPerformed'
    
         # Validate that the Kafka event details are as expected  
        * json padStatus = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))    
        * match padStatus.ProviderId == providerDetails.providerId
        * match padStatus.ReceivedDate contains dateStamp 
        * match padStatus.ReasonNotes == reasonNotes
        * match padStatus.ProductCode == 'PAD'
        * match padStatus.ReasonType == 'Not clinically relevant' 
        * match padStatus.CreateDate contains dateStamp 
        * match padStatus.Reason == 'Not clinically relevant'
        * match padStatus.MemberPlanId == memberDetails.memberPlanId                    
                
    @TestCaseKey=ANC-T525
    Scenario Outline: PAD Not Performed - Member Refused
        * def reasonNotes = Faker().randomQuote()
        * set evaluation.answers =
                """
                [
                    {
                        'AnswerId': 29561,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': '2'
                    },
                    {
                        'AnswerId': 30957,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': 'Member refused'
                    },
                    {
                        'AnswerId': <answer_id>,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': <answer_value>
                    },
                    {
                        'AnswerId': 30964,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': #(reasonNotes)
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
        * def result = PadDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.LeftScoreAnswerValue == null
        * match result.LeftSeverityAnswerValue == null
        * match result.RightScoreAnswerValue == null
        * match result.RightSeverityAnswerValue == null
        * match result.NotPerformedId != null
        * match result.AnswerId == <answer_id>
        * match result.Notes == reasonNotes
    
        # Validate that the Kafka event details are as expected
        * json padStatus = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))    
        * match padStatus.ProviderId == providerDetails.providerId
        * match padStatus.ReceivedDate contains dateStamp 
        * match padStatus.ReasonNotes == reasonNotes
        * match padStatus.ProductCode == 'PAD'
        * match padStatus.ReasonType == 'Member refused' 
        * match padStatus.CreateDate contains dateStamp 
        * match padStatus.Reason == <expected_reason>
        * match padStatus.MemberPlanId == memberDetails.memberPlanId
            
        Examples:
            | answer_id | answer_value                | expected_reason             |
            | 30959     | 'Member recently completed' | 'Member recently completed' |
            | 30960     | 'Scheduled to complete'     | 'Scheduled to complete'     |
            | 30961     | 'Member apprehension'       | 'Member apprehension'       |
            | 30962     | 'Not interested'            | 'Not interested'            |
            | 30963     | 'Other'                     | 'Other'                     |

        @TestCaseKey=ANC-T570
        Scenario Outline: PAD Not Performed - Without Reason Notes Answer
        * set evaluation.answers =
            """
                [
                    {
                        'AnswerId': 29561,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': '2'
                    },
                    {
                        'AnswerId': <refusal_type_answer_id>,
                        'AnsweredDateTime': '#(timestamp)',
                        'AnswerValue': <refusal_type>
                    },
                    {
                        'AnswerId': <answer_id>,
                        'AnsweredDateTime': '#(timestamp)',
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
    
        # Validate that the database details are as expected
        * def result = PadDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.LeftScoreAnswerValue == null
        * match result.LeftSeverityAnswerValue == null
        * match result.RightScoreAnswerValue == null
        * match result.RightSeverityAnswerValue == null
        * match result.NotPerformedId != null
        * match result.AnswerId == <answer_id>
        * match result.Notes == ''
    
        # Verify the status from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'PADNotPerformed'
    
        # Validate that the Kafka event details are as expected  
        * json padStatus = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))    
        * match padStatus.ProviderId == providerDetails.providerId
        * match padStatus.ReceivedDate contains dateStamp 
        * match padStatus.ReasonNotes == ''
        * match padStatus.ProductCode == 'PAD'
        * match padStatus.ReasonType == <refusal_type> 
        * match padStatus.CreateDate contains dateStamp 
        * match padStatus.Reason == <expected_reason>
        * match padStatus.MemberPlanId == memberDetails.memberPlanId
        
        Examples:
            | answer_id | answer_value                | expected_reason              | refusal_type              | refusal_type_answer_id |
            | 30959     | 'Member recently completed' | 'Member recently completed'  | 'Member refused'          | 30957                  |
            | 30967     | 'Environmental issue'       | 'Environmental issue'        | 'Unable to perform'       | 30958                  |
            | 31125     | 'Not clinically relevant'   | 'Not clinically relevant'    | 'Not clinically relevant' | 31225                  |
           
    
