@spirometry
@envnot=prod
Feature: Consume Kafka Overread Message

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @TestCaseKey=ANC-T457
    @TestCaseKey=ANC-T458
    @TestCaseKey=ANC-T456
    Scenario Outline: Spirometry Process Manager Consumes Overread Data from Kafka
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
        
        # Publish the Overread event to the overread_spirometry topic
        * string overrreadEventKey = appointment.appointmentId
        * def OverreadId = DataGen().uuid()

        * string overreadHeader = {'Type': 'OverreadProcessed'}
        * string overreadEventValue = {'OverreadId': '#(OverreadId)','MemberId':'','AppointmentId': '#(appointment.appointmentId)','SessionId': '#(OverreadId)','PerformedDateTime': '#(timestamp)','OverreadDateTime': '#(timestamp)','BestTestId': '#(OverreadId)','BestFvcTestId': '#(OverreadId)','BestFvcTestComment': 'TestComment','BestFev1TestId': '#(OverreadId)','BestFev1TestComment': 'TestComment','BestPefTestId': '#(OverreadId)','BestPefTestComment': 'TestComment','Comment': 'TestComment','Fev1FvcRatio':2.5,'OverreadBy': 'JohnDoe','ObstructionPerOverread':<obstructionPerOverread>,'ReceivedDateTime': '#(timestamp)'}
        * kafkaProducerHelper.send("overread_spirometry", overrreadEventKey, overreadHeader, overreadEventValue)
        
        # Validate that overread message data are saved correctly to OverreadResult table
        * def result = SpiroDb().getOverreadResultByAppointmentId(appointment.appointmentId)[0]
        * match result.ExternalId.toString() == OverreadId
        * match result.AppointmentId         == appointment.appointmentId
        * match result.SessionId.toString()  == OverreadId
        * match result.Fev1FvcRatio          == 2.5
        * match result.PerformedDateTime     == '#notnull' 
        * match result.OverreadDateTime      == '#notnull' 
        * match result.OverreadBy            == 'JohnDoe'   
        * match result.OverreadComment       == 'TestComment'    
        * match result.BestTestId.toString() == OverreadId     
        * match result.BestFvcTestComment    == 'TestComment'      
        * match result.BestFvcTestId.toString()  == OverreadId    
        * match result.BestFev1TestComment       == 'TestComment'         
        * match result.BestFev1TestId.toString() == OverreadId      
        * match result.BestPefTestComment        == 'TestComment'         
        * match result.BestPefTestId.toString()  == OverreadId       
        * match result.ReceivedDateTime          == '#notnull' 
        * match result.NormalityIndicatorId      == <normality>
        
        Examples:
        | session_grade_id | session_grade_value | obstructionPerOverread|  normality  |
        | 50940            | "D"                 | "YES"                 |  3          |
        | 50941            | "E"                 | "NO"                  |  2          |
        | 50942            | "F"                 | "INCONCLUSIVE"        |  1          |