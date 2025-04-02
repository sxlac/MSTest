@fobt
@envnot=prod
Feature: FOBT DOS scenario

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def FobtDb = function() { var FobtDb = Java.type('helpers.database.fobt.FobtDb'); return new FobtDb(); }
        * def FobtFileshare = function() { var FobtFileshare = Java.type('helpers.fileshare.FobtFileshare'); return new FobtFileshare(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'FOBT'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        
    @TestCaseKey=ANC-T1012
    Scenario Outline: FOBT DOS scenario
        * def randomBarcode = Faker().randomDigit(6)
        * def eventId = DataGen().uuid()
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 21113,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Yes'
                },
                {
                    'AnswerId': 21119,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomBarcode),
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
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * eval sleep(30000)

        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTNotPerformed
        * def result = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def toUTCDate = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * match toUTCDate.split('T')[0] contains dateStamp.toString()
        * def NewdateStamp = DataGen().isoDateStamp(-2)
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 21113,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Yes'
                },
                {
                    'AnswerId': 21119,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(randomBarcode),
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(dateStamp)',
                    'AnswerValue': '#(NewdateStamp)'
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
        # Validate that the database FOBT details are as expected using EvaluationId in FOBT and FOBTNotPerformed
        * def result = FobtDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def toUTCDate = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * match toUTCDate.split('T')[0] contains NewdateStamp.toString()

        Examples:
            | lab_result | abnormal_indicator | exception_message | 
            | 'Positive' | 'A'                |    ''             |