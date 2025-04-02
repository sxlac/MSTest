@pad
@envnot=prod
Feature: PAD Severity Determination Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def PadDb = function() { var PadDb = Java.type("helpers.database.pad.PadDb"); return new PadDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        
    @TestCaseKey=ANC-T373
    @TestCaseKey=ANC-T374
    @TestCaseKey=ANC-T365
    @TestCaseKey=ANC-T360
    @TestCaseKey=ANC-T361
    Scenario Outline: PAD Severity Business Rules Validation
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
                    "AnswerId": <left_answer_result>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<left_outcome>"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_float_result>"
                },
                {
                    "AnswerId": <right_answer_result>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_outcome>"
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

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')
        * def result = PadDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result == "#notnull"

        # Verify the results
        * match result.LeftScoreAnswerValue == <left_float_result> + ''
        * match result.LeftSeverityAnswerValue == <left_outcome>
        * match result.RightScoreAnswerValue == <right_float_result> + ''
        * match result.RightSeverityAnswerValue == <right_outcome>
        * match result.LeftNormalityIndicator == <left_normality>
        * match result.RightNormalityIndicator == <right_normality>

        Examples:
            | left_outcome | left_float_result | left_answer_result | left_normality | right_outcome | right_float_result | right_answer_result | right_normality |
            | 'Normal'     | 1                 | 31042              | 'N'            | 'Normal'      | 1.4                | 31047               | 'N'             |
            | 'Borderline' | 0.9               | 31043              | 'N'            | 'Borderline'  | 0.99               | 31048               | 'N'             |
            | 'Mild'       | 0.6               | 31044              | 'A'            | 'Mild'        | 0.89               | 31049               | 'A'             |
            | 'Moderate'   | 0.3               | 31045              | 'A'            | 'Moderate'    | 0.59               | 31050               | 'A'             |
            | 'Severe'     | 0                 | 31046              | 'A'            | 'Severe'      | 0.29               | 31051               | 'A'             |