@spirometry
Feature: Publish Overread Results to Kafka

    Background:
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

    @TestCaseKey=ANC-T828
    Scenario Outline: Spirometry Process Manager Labs. Negative scenario. Invalid csv has not been processed and record not appeared in Overread result
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
                    "AnswerValue": 100
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 30
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 0.3
                },
                {
                    "AnswerId": 50944,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Yes" 
                },        
                {
                    "AnswerId": 50948,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  "No"   
                },
                {
                    "AnswerId": 50952,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Unknown"
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "#(timestamp)"
                },
                {
                    "AnswerId": 29614,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <Hx_of_COPD>
                },
                {
                    "AnswerId": 51420,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue":  <LungFunctionScore>
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

        # Publish the Overread event to the overread_spirometry topic
        * string overrreadEventKey = appointment.appointmentId
        * def OverreadId = DataGen().uuid()

        * string overreadHeader = {'Type': 'OverreadProcessed'}

        * def CsvOverreadFileshare = function() { var CsvOverreadFileshare = Java.type('helpers.endToEndHelpers.CsvOverreadHelpers'); return new CsvOverreadFileshare(); }
        * def fileName = `Valid_SpiroOverread_${DataGen().randomInteger(0000, 9999)}.csv`
        * CsvOverreadFileshare().createAndDropSpiroCsvToPendingFolder(fileName, true, appointment.appointmentId)
        * CsvOverreadFileshare().checkSpiroCsvMovedToInvalidFolder(fileName, 130, 250)
        # Validate that overread message data are saved correctly to OverreadResult table
        * def result = SpiroDb().getOverreadResultByAppointmentId(appointment.appointmentId)[0]
        * match result == null

        Examples:
        | session_grade_id | session_grade_value|             Hx_of_COPD                       |LungFunctionScore|overread_ratio|
        | 51947            | "E"                |"Chronic obstructive pulmonary disease (COPD)"|19               |    0.65      |
