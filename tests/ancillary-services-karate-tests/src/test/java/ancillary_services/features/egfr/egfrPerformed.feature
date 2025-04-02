@egfr
@envnot=prod
Feature: eGFR Lab Performed Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'EGFR'] }).response
        

    @TestCaseKey=ANC-T409
    Scenario: eGFR Performed KED
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 52456,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 51261,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 52484,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(DataGen().GetLGCBarcode())
                },
                {
                    "AnswerId": 52483,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(DataGen().GetAlfacode())
                },
                {
                    "AnswerId": 52480,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(DataGen().GetAlfacode())
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * print evalEvent
        * match evalEvent.Id == '#notnull'	
        
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        # Verify member details
        * match result[0].FirstName == memberDetails.firstName
        * match result[0].MiddleName == memberDetails.middleName
        * match result[0].LastName == memberDetails.lastName
        * match result[0].AddressLineOne == memberDetails.address.address1
        * match result[0].AddressLineTwo == memberDetails.address.address2
        * match result[0].City == memberDetails.address.city
        * match result[0].State == memberDetails.address.state
        * match result[0].ZipCode == memberDetails.address.zipCode
        * match result[0].MemberId == memberDetails.memberId
        * match result[0].CenseoId == memberDetails.censeoId
        * match result[0].MemberPlanId == memberDetails.memberPlanId
        # Verify provider details
        * match result[0].ProviderId == providerDetails.providerId
        * match result[0].NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        # Verify evaluation details
        * match result[0].EvaluationId == evaluation.evaluationId
        * match result[0].AppointmentId == appointment.appointmentId
        * match result[0].StatusDateTime != null
        * match result[0].DateOfService != null
        * match result[*].StatusName contains ["Order Requested", "Exam Performed"]
        * match result[*].ExamStatusCodeId contains [14, 1]

        # check if the data received via EvaluationFinalizedEvent event is written to the database properly
        * match DataGen().RemoveMilliSeconds(evalEvent.ReceivedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(evalEvent.CreatedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match evalEvent.DateOfService == DataGen().getUtcDateTimeString(result[0].DateOfService.toString()).split('+')[0]

        # Verify barcode details
        * def barcodeHistory = EgfrDb().getBarcodeHistoryByExamId(result[0].ExamId)
        * match barcodeHistory[0].Barcode == evaluation.answers[2].AnswerValue+'|'+ evaluation.answers[3].AnswerValue
        
        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * print kafkaEvent
        * match kafkaEvent.Barcode == evaluation.answers[2].AnswerValue+'|'+ evaluation.answers[3].AnswerValue
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
