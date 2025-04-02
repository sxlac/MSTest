@spirometry
@envnot=prod
Feature: Spirometry - Dos Second Eval

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()
        
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        
    @TestCaseKey=ANC-T711
    Scenario Outline: Spirometry - Dos Second Eval
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
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(dateStamp)"
                },
                {
                    "AnswerId": <Hx_of_COPD_AID>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <Hx_of_COPD_answer_value>
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
            * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
            * eval sleep(30000)
            * def result = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
            * def toUTCDate = DataGen().getUtcDateTimeString(result.DateOfService.toString())
            * match toUTCDate.split('T')[0] contains dateStamp.toString()
            * def NewdateStamp = DataGen().isoDateStamp(-2)
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
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(NewdateStamp)"
                },
                {
                    "AnswerId": <Hx_of_COPD_AID>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <Hx_of_COPD_answer_value>
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
            * eval sleep(30000)
            * def result = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
            * def toUTCDate = DataGen().getUtcDateTimeString(result.DateOfService.toString())
            * match toUTCDate.split('T')[0] contains NewdateStamp.toString()
        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc | normality  |Hx_of_COPD_AID|Hx_of_COPD_answer_value                       |HasHistoryOfCopd|
        | 50939            | "C"                 | 80  | 80   | 0.85     | "Normal"   |29614         |"Constipation"                                |false|  