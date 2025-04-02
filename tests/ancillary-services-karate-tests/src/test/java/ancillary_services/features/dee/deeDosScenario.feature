# Iris GradeAsNormal API is not functional. All tests with Iris integrations will need to be tested manually until issue is resolved. 
@ignore
@envnot=prod  
# @dee
@parallel=false
Feature: DEE DOS

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def DeeDb = function() { var DeeDb = Java.type("helpers.database.dee.DeeDb"); return new DeeDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'DEE'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        
    @TestCaseKey=ANC-T1012
    Scenario: DEE DOS
        * def image1 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-1.txt')
        * def image2 = karate.readAsString('classpath:ancillary_services/features/dee/images/normalEval/image-2.txt')
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29554,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 28377,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "John"
                },
                {
                    "AnswerId": 28378,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Doe"
                },
                {
                    "AnswerId": 30974,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "M"
                },
                {
                    "AnswerId": 28383,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "TX"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "5AA52E97-D999-4093-BF1B-7AE171C2DFBC",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image1)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "B5C78B69-1A5C-40F6-B53A-306F0E1A54C6",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image2)"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
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
        # Needed due to there being a 10 second delay between CreateDEE command when message is transferred to ProcessDEE
        * eval sleep(10000)        


        * def exam = karate.call('classpath:/helpers/iris/exam/getExam.feature')
        * karate.call('classpath:helpers/iris/exam/gradeExamAsNormal.feature', exam)
        * eval sleep(30000)
        * def result = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def toUTCDate = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * match toUTCDate.split('T')[0] contains dateStamp.toString()
        * def NewdateStamp = DataGen().isoDateStamp(-2)

        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29554,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 28377,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "John"
                },
                {
                    "AnswerId": 28378,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Doe"
                },
                {
                    "AnswerId": 30974,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "M"
                },
                {
                    "AnswerId": 28383,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "TX"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "5AA52E97-D999-4093-BF1B-7AE171C2DFBC",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image1)"
                },
                {
                    "AnswerId": 30856,
                    "AnswerRowId": "B5C78B69-1A5C-40F6-B53A-306F0E1A54C6",
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(image2)"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
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

        * def result = DeeDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * def toUTCDate = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * match toUTCDate.split('T')[0] contains NewdateStamp.toString()