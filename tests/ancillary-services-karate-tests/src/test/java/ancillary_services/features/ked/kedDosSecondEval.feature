@ked
@uacr
@egfr
@envnot=prod
Feature: KED Lab Performed Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type('helpers.database.uacr.UacrDb'); return new UacrDb(); }
        * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
        * def OmsDb = function() { var OmsDb = Java.type('helpers.database.oms.OmsDb'); return new OmsDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'UACR', 'EGFR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

    @TestCaseKey=ANC-T1012
    Scenario: KED Performed
        * def alphaBarcode = DataGen().GetAlfacode()
        * def numericBarcode = DataGen().GetLGCBarcode()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52456,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52458,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52482,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(numericBarcode)
                },
                {
                    "AnswerId": 52481,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(alphaBarcode)
                },
                {
                    "AnswerId": 51261,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52484,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(numericBarcode)
                },
                {
                    "AnswerId": 52483,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(alphaBarcode)
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
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * eval sleep(30000)
        # UACR record check
        * def result = UacrDb().getResultsByEvaluationId(evaluation.evaluationId)
        * def toUTCDate = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * match toUTCDate.split('T')[0] contains dateStamp.toString()
        * def NewdateStamp = DataGen().isoDateStamp(-2)
        # EGFR record check
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        * def toUTCDate = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * match toUTCDate.split('T')[0] contains dateStamp.toString()
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52456,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52458,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52482,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(numericBarcode)
                },
                {
                    "AnswerId": 52481,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(alphaBarcode)
                },
                {
                    "AnswerId": 51261,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52484,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(numericBarcode)
                },
                {
                    "AnswerId": 52483,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(alphaBarcode)
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": "#(NewdateStamp)"
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
        # UACR record check
        * def result = UacrDb().getResultsByEvaluationId(evaluation.evaluationId)
        * def toUTCDate = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * match toUTCDate.split('T')[0] contains NewdateStamp.toString()
        # EGFR record check
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        * def toUTCDate = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * match toUTCDate.split('T')[0] contains NewdateStamp.toString()