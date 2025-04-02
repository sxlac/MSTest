@spirometry
@envnot=prod
Feature: Spirometry Evaluation Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()
        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')

    @TestCaseKey=ANC-T397
    Scenario Outline: Spirometry Evaluation - Frequency Type Answers
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 50919,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 50938,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'B'
                },
                {
                    'AnswerId': 50999,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 80
                },
                {
                    'AnswerId': 51000,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 80
                },
                {
                    'AnswerId': 51002,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 0.65
                },
                {
                    'AnswerId': <cough_frequency>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <cough_frequency_value>
                },
                {
                    'AnswerId': <chest_noise>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <chest_noise_value>
                },
                {
                    'AnswerId': <breath_shortness>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <breath_shortness_value>
                },
                {
                    'AnswerId': <lung_function_score>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <lung_function_score_value>
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

        * def result = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        * match result.CoughMucusOccurrenceFrequencyId == <cough_frequency_id>
        * match result.NoisyChestOccurrenceFrequencyId == <chest_noise_id>
        * match result.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId == <breath_shortness_id>
        * match result.LungFunctionScore == <lung_function_score_value>

        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_result", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))
        * match event.Results.CoughMucusOccurrenceFrequency == <cough_frequency_value>
        * match event.Results.NoisyChestOccurrenceFrequency == <chest_noise_value>
        * match event.Results.ShortnessOfBreathPhysicalActivityOccurrenceFrequency == <breath_shortness_value>
        * match event.Results.LungFunctionScore == <lung_function_score_value>

        Examples:
            | cough_frequency | cough_frequency_value | cough_frequency_id | chest_noise | chest_noise_value | chest_noise_id | breath_shortness | breath_shortness_value | breath_shortness_id | lung_function_score | lung_function_score_value |
            | 51405           | 'Never'               | 1                  | 51410       | 'Never'           | 1              | 51415            | 'Never'                | 1                   | 51420               | 1                         |
            | 51406           | 'Rarely'              | 2                  | 51411       | 'Rarely'          | 2              | 51416            | 'Rarely'               | 2                   | 51420               | 10                        |
            | 51407           | 'Sometimes'           | 3                  | 51412       | 'Sometimes'       | 3              | 51417            | 'Sometimes'            | 3                   | 51420               | 21                        |
            | 51408           | 'Often'               | 4                  | 51413       | 'Often'           | 4              | 51418            | 'Often'                | 4                   | 51420               | 8                         |
            | 51409           | 'Very often'          | 5                  | 51414       | 'Very often'      | 5              | 51419            | 'Very often'           | 5                   | 51420               | 35                        |


        @TestCaseKey=ANC-T398
        Scenario Outline: Spirometry Evaluation - Trilean Type Answers
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 50919,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 1
                },
                {
                    'AnswerId': 50938,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'B'
                },
                {
                    'AnswerId': 50999,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 80
                },
                {
                    'AnswerId': 51000,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 80
                },
                {
                    'AnswerId': 51002,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 0.65
                },
                {
                    'AnswerId': <smoke>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <smoke_answer_value>
                },
                {
                    'AnswerId': 21211,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <smoking_years>
                },
                {
                    'AnswerId': <sputumwithcough>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <sputumwithcough_value>
                },
                {
                    'AnswerId': <hadwheezing>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <hadwheezing_value>
                },
                {
                    'AnswerId': <shortnessofbreathatrest>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <shortnessofbreathatrest_value>
                },
                {
                    'AnswerId': <shortnessofbreathwithexertion>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <shortnessofbreathwithexertion_value>
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

        * def result = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.HasSmokedTobacco == <smoke_answer_id>
        * match result.TotalYearsSmoking == <smoking_years>
        * match result.ProducesSputumWithCough == <sputumwithcough_id>
        * match result.HadWheezingPast12moTrileanTypeId == <hadwheezing_id>
        * match result.GetsShortnessOfBreathAtRestTrileanTypeId == <shortnessofbreathatrest_id>
        * match result.GetsShortnessOfBreathWithMildExertionTrileanTypeId == <shortnessofbreathwithexertion_id>

        # Validate that the Kafka event has the expected properties as in the result contract
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_result", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))
        
        * match event.Results.HasSmokedTobacco == <smoke_answer_id>
        * match event.Results.TotalYearsSmoking == <smoking_years>
        * match event.Results.ProducesSputumWithCough == <sputumwithcough_id>
        * match event.Results.HadWheezingPast12mo == <hadwheezing_value>
        * match event.Results.GetsShortnessOfBreathAtRest == <shortnessofbreathatrest_value>
        * match event.Results.GetsShortnessOfBreathWithMildExertion == <shortnessofbreathwithexertion_value>

        Examples:
            | smoke | smoke_answer_value | smoke_answer_id | smoking_years | sputumwithcough | sputumwithcough_value | sputumwithcough_id | hadwheezing | hadwheezing_value | hadwheezing_id | shortnessofbreathatrest | shortnessofbreathatrest_value | shortnessofbreathatrest_id | shortnessofbreathwithexertion | shortnessofbreathwithexertion_value | shortnessofbreathwithexertion_id |
            | 20486 | 1                  | true            | 15            | 20724           | 1                     | true               | 20484       | 'Y'               | 2              | 20498                   | 'Y'                           | 2                          | 20500                         | 'Y'                                 | 2                                |
            | 20486 | 1                  | true            | 15            | 20724           | 1                     | true               | 33594       | 'U'               | 1              | 33596                   | 'U'                           | 1                          | 33597                         | 'U'                                 | 1                                |
            | 20485 | 2                  | false           | 35            | 20723           | 1                     | false              | 20483       | 'N'               | 3              | 20497                   | 'N'                           | 3                          | 20499                         | 'N'                                 | 3                                |