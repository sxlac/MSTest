@uacr
@envnot=prod
Feature: uACR  - Dos Second Eval

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type('helpers.database.uacr.UacrDb'); return new UacrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'UACR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

    @TestCaseKey=ANC-T1012
    Scenario: Submit uACR  - Dos Second Eval
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
                    "AnswerId": 51276,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(evaluation.evaluationId)
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": "#(dateStamp)"
                },
                {
                    "AnswerId": 52482,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(DataGen().GetLGCBarcode())
                },
                {
                    "AnswerId": 52481,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(DataGen().GetAlfacode())
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
        
        * def result = UacrDb().getExamDates(evaluation.evaluationId)
        * def toUTCDate = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * match toUTCDate.split('T')[0] contains dateStamp.toString()
        * def NewdateStamp = DataGen().isoDateStamp(-2)

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
                "AnswerId": 51276,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(evaluation.evaluationId)
            },
            {
                "AnswerId": 22034,
                "AnsweredDateTime": "#(dateStamp)",
                "AnswerValue": "#(NewdateStamp)"
            },
            {
                "AnswerId": 52482,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(DataGen().GetLGCBarcode())
            },
            {
                "AnswerId": 52481,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": #(DataGen().GetAlfacode())
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
    * def result = UacrDb().getExamDates(evaluation.evaluationId)
    * def toUTCDate = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
    * match toUTCDate.split('T')[0] contains NewdateStamp.toString()