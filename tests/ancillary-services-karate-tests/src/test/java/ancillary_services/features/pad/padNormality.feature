@pad
@envnot=prod
Feature: PAD Normality Determination Tests

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
        
    @TestCaseKey=ANC-T372
    Scenario Outline: PAD Normality Business Rules Validation
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

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')
        * def result = PadDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result == "#notnull"
        * match result.LeftNormalityIndicator == <left_normality>
        * match result.RightNormalityIndicator == <right_normality>

        # Validate that the Kafka event has the expected determination
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("PAD_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))    
        * match event.Determination == <determination>

        Examples:
            | left_float_result | left_normality | right_float_result | right_normality | determination |
            | 99                | 'U'            | 99                 | 'U'             | 'U'           |
            | 0.3               | 'A'            | 0.6                | 'A'             | 'A'           |
            | 1                 | 'N'            | 0.3                | 'A'             | 'A'           |
            | 99                | 'U'            | 0.3                | 'A'             | 'A'           |
            | 1                 | 'N'            | 1                  | 'N'             | 'N'           |
            | 99                | 'U'            | 1                  | 'N'             | 'N'           |
    
    @TestCaseKey=ANC-T1055
    Scenario Outline: PAD AoESymptom Support Answers Validation
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
                    "AnswerValue": "0.3"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "0.6"
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
                },
                {
                    "AnswerId": <footpainresting_elevated_answerId>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<footpainresting_elevated_answer>"
                },
                {
                    "AnswerId": <footpaindisappear_walking_answerId>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<footpaindisappear_walking_answer>"
                },
                {
                    "AnswerId": <footpaindisappear_otc_answerId>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<footpaindisappear_otc_answer>"
                },
                {
                    "AnswerId": <pedalpulse_answerId>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<pedalpulse_answer>"
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')
        * def result = PadDb().getAoESymptomSupportResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.FootPainRestingElevatedLateralityCodeId == <lateralitycode>
        * match result.FootPainDisappearsWalkingOrDangling == <footpaindisappear_walking>
        * match result.FootPainDisappearsOtc == <footpaindisappear_otc>
        * match result.PedalPulseCodeId == <pedalpulse_codeId>

        # Validate that the Kafka event has the expected determination
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("PAD_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))    
        * match event.Determination == 'A'

        Examples:
            | footpainresting_elevated_answerId|footpainresting_elevated_answer|lateralitycode | footpaindisappear_walking_answerId|footpaindisappear_walking_answer|footpaindisappear_walking|footpaindisappear_otc_answerId|footpaindisappear_otc_answer|footpaindisappear_otc| pedalpulse_answerId | pedalpulse_answer       |pedalpulse_codeId |
            | 52178                            | 'Right'                       | 3             | 52182                             | 'Yes'                          | true                    |52184                         |'Yes'                       | true                |  52186              |   'Normal'              | 1                |
            | 52179                            | 'Left'                        | 2             | 52183                             | 'No'                           | false                   |52185                         |'No'                        | false               |  52187              |   'Abnormal-Left'       | 2                |
            | 52180                            | 'Both'                        | 4             | 52183                             | 'No'                           | false                   |52185                         |'No'                        | false               |  52188              |   'Abnormal-Right'      | 3                |
            | 52181                            | 'No'                          | 1             | 52183                             | 'No'                           | false                   |52185                         |'No'                        | false               |  52189              |   'Abnormal-Bilateral'  | 4                |
            | 52181                            | 'No'                          | 1             | 52183                             | 'No'                           | false                   |52185                         |'No'                        | false               |  52190              |   'Not Performed'       | 5                |
            
      