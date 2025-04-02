@spirometry
@envnot=prod
 
Feature: Spirometry Results Publishing to Kafka topic

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()
        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        
    @TestCaseKey=ANC-T717
    Scenario Outline: Spirometry Result Kafka Message Validation
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 50919,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': <session_grade_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <session_grade_value>
                },
                {
                    'AnswerId': 50999,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <fvc>
                },
                {
                    'AnswerId': 51000,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <fev1>
                },
                {
                    'AnswerId': 51002,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <fev1_fvc>
                },
                {
                    'AnswerId': 20486,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 21211,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 20724,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 51407,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Sometimes'
                },
                {
                    'AnswerId': 20484,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 20498,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 20500,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 51412,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Sometimes'
                },
                {
                    'AnswerId': 51417,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Sometimes'
                },
                {
                    'AnswerId': 51420,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 29
                },
                {
                    'AnswerId': 50993,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(timestamp)'
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

        # Validate that the Kafka event has the expected properties as in the result contract
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_result", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))   
        * match event.IsBillable == <billable> 
        * match event.Determination == <normality_indicator>
        * match event.PerformedDate == '#notnull'
        * match event.ReceivedDate == '#notnull'
        * match event.Results.SessionGrade  == <session_grade_value> 
        * match event.Results.Fvc == <fvc> 
        * match event.Results.FvcNormality  == <fvcNormality> 
        * match event.Results.Fev1 == <fev1>     
        * match event.Results.Fev1Normality == <fev1Normality> 
        * match event.Results.Fev1OverFvc == <fev1_fvc> 
        * match event.Results.HasSmokedTobacco == true
        * match event.Results.TotalYearsSmoking == 1
        * match event.Results.ProducesSputumWithCough == true
        * match event.Results.CoughMucusOccurrenceFrequency == 'Sometimes'
        * match event.Results.HadWheezingPast12mo == 'Y'
        * match event.Results.GetsShortnessOfBreathAtRest == 'Y'
        * match event.Results.GetsShortnessOfBreathWithMildExertion == 'Y'
        * match event.Results.NoisyChestOccurrenceFrequency == 'Sometimes'
        * match event.Results.ShortnessOfBreathPhysicalActivityOccurrenceFrequency == 'Sometimes'
        * match event.Results.LungFunctionScore == 29
        * match event.Results.Copd == true

        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc | normality      |normality_indicator|billable|fvcNormality|fev1Normality|
        | 50938            | 'B'                 | 80  | 80   | 0.65     | 'Abnormal'     |'A'                |true    |'N'         |'N'          |
        | 50937            | 'A'                 | 65  | 125  | 0.7      |  'Normal'      |'N'                |true    |'A'         |'N'          |

    @TestCaseKey=ANC-T718
    Scenario: Spirometry Results Can be Null in Kafka
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 50919,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(timestamp)'
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

        # Validate that the Kafka event has the expected properties as in the result contract
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_result", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))   
        
        * match event.ReceivedDate == '#notnull'
        * match event.Results.SessionGrade  == null
        * match event.Results.Fvc == null
        * match event.Results.FvcNormality == 'U' 
        * match event.Results.Fev1 == null    
        * match event.Results.Fev1Normality == 'U' 
        * match event.Results.Fev1OverFvc == null
        * match event.Results.HasSmokedTobacco == null
        * match event.Results.TotalYearsSmoking == null
        * match event.Results.ProducesSputumWithCough == null
        * match event.Results.CoughMucusOccurrenceFrequency == null
        * match event.Results.HadWheezingPast12mo == null
        * match event.Results.GetsShortnessOfBreathAtRest == null
        * match event.Results.GetsShortnessOfBreathWithMildExertion == null
        * match event.Results.NoisyChestOccurrenceFrequency == null
        * match event.Results.ShortnessOfBreathPhysicalActivityOccurrenceFrequency == null
        * match event.Results.LungFunctionScore == null
        * match event.Results.Copd == null

    @TestCaseKey=ANC-T551
    Scenario Outline: Spirometry Result Kafka Message Validation - FEV1 & FVC Upper Limits
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
                        "AnswerValue": 29
                    },
                    {
                        "AnswerId": 22034,
                        "AnsweredDateTime": "#(timestamp)",
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
                
        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate that the Kafka event has the expected properties as in the result contract
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_result", evaluation.evaluationId + '', "ResultsReceived", 10, 5000)) 
        * match event.PerformedDate contains dateStamp
        * match event.ReceivedDate contains dateStamp
        * match event.IsBillable == <billable> 
        * match event.Determination == <normality_indicator>
        * match event.Results.SessionGrade  == <session_grade_value> 
        * match event.Results.Fvc == <fvc> 
        * match event.Results.FvcNormality  == <fvcNormality> 
        * match event.Results.Fev1 == <fev1>     
        * match event.Results.Fev1Normality == <fev1Normality> 
        * match event.Results.Fev1OverFvc == <fev1_fvc> 
        * match event.Results.HasSmokedTobacco == null
        * match event.Results.TotalYearsSmoking == null
        * match event.Results.ProducesSputumWithCough == null
        * match event.Results.CoughMucusOccurrenceFrequency == 'Never'
        * match event.Results.HadWheezingPast12mo == null
        * match event.Results.GetsShortnessOfBreathAtRest == null
        * match event.Results.GetsShortnessOfBreathWithMildExertion == null
        * match event.Results.NoisyChestOccurrenceFrequency == 'Never'
        * match event.Results.ShortnessOfBreathPhysicalActivityOccurrenceFrequency == 'Never'
        * match event.Results.LungFunctionScore == 29
        * match event.Results.Copd == null

        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc |normality_indicator|billable|fvcNormality|fev1Normality|
        | 50938            | 'B'                 | 600 | 600  | 0.9      | 'N'               | true   | 'N'        |'N'          |
        | 50938            | 'B'                 | 650 | 650  | 0.8      | 'N'               |true    | 'U'        |'U'          |