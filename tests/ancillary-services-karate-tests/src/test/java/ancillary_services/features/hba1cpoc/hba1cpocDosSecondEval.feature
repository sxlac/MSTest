@hba1cpoc
@envnot=prod
Feature: HBA1CPOC Dos Second Eval

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Hba1cPOCDb = function() { var Hba1cpocDb = Java.type("helpers.database.hba1cpoc.Hba1cpocDb"); return new Hba1cpocDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def expirationDate = DataGen().isoDateStamp(30)

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'HBA1CPOC'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        
    @parallel=false
    @TestCaseKey=ANC-T1012
    Scenario Outline: HBA1CPOC - Dos Second Eval
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 33070,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 33088,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>,
                },
                {
                    "AnswerId": 33264,
                    "AnsweredDateTime": '#(timestamp)',
                    "AnswerValue":  '#(expirationDate)',
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
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * eval sleep(30000)
        * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def toUTCDate = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * match toUTCDate.split('T')[0] contains dateStamp.toString()
        * def NewdateStamp = DataGen().isoDateStamp(-2)
        * set evaluation.answers =
        """
        [
            {
                "AnswerId": 33070,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": 1
            },
            {
                "AnswerId": 33088,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": <answer_value>,
            },
            {
                "AnswerId": 33264,
                "AnsweredDateTime": '#(timestamp)',
                "AnswerValue":  '#(expirationDate)',
            },
            {
                "AnswerId": 22034,
                "AnsweredDateTime": '#(dateStamp)',
                "AnswerValue": '#(NewdateStamp)'
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
        * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def toUTCDate = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * match toUTCDate.split('T')[0] contains NewdateStamp.toString()
        Examples:
            | answer_value | normality |
            | '3.9'        | 'A'       |
