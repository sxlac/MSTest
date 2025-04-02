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
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'EGFR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }


    Scenario Outline: eGFR Performed
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

        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        # Verify member details
        * match result.FirstName == memberDetails.firstName
        * match result.MiddleName == memberDetails.middleName
        * match result.LastName == memberDetails.lastName
        * match result.AddressLineOne == memberDetails.address.address1
        * match result.AddressLineTwo == memberDetails.address.address2
        * match result.City == memberDetails.address.city
        * match result.State == memberDetails.address.state
        * match result.ZipCode == memberDetails.address.zipCode
        * match result.MemberId == memberDetails.memberId
        * match result.CenseoId == memberDetails.censeoId
        * match result.MemberPlanId == memberDetails.memberPlanId
        # Verify provider details
        * match result.ProviderId == providerDetails.providerId
        * match result.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        # Verify evaluation details
        * match result.EvaluationId == evaluation.evaluationId
        * match result.AppointmentId == appointment.appointmentId
        * match result.StatusDateTime != null

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '#(CMP_eGFRResult)','EstimatedGlomerularFiltrationRateResultDescription': '#(CMP_Description)','EstimatedGlomerularFiltrationRateResultColor': '#(CMP_EstimatedGlomerularFiltrationRateResultColor)'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000) 

        # Validate the DB table for LabResults
        * def labResult = EgfrDb().getLabResultsRecordByEvaluationId(evaluation.evaluationId)[0]
        * match labResult.EvaluationId == evaluation.evaluationId
        
        # Verify Kafka message present in egfr_results
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.Determination == <expected_normality_indicator>
        * match kafkaEvent.IsBillable == <is_billable>
        * match kafkaEvent.PerformedDate != null
        * match kafkaEvent.ReceivedDate != null
        * match kafkaEvent.Result.AbnormalIndicator == <expected_normality_indicator>
        * match kafkaEvent.Result.Result == CMP_eGFRResult*1
        * match kafkaEvent.Result.Description == CMP_Description
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.PerformedDate.toString().split('T')[1] == "00:00:00+00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.PerformedDate) == DataGen().getUtcDateTimeString(result.DateOfService.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(labResult.CreatedDateTime.toString())

        # Validate Exam Status Update in database
        * def statuses = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        # Validate response contains 1 - Exam Performed
        * match statuses[*].ExamStatusCodeId contains 1

        Examples:
            | CMP_eGFRResult | CMP_EstimatedGlomerularFiltrationRateResultColor | CMP_Description | expected_normality_indicator   |is_billable |
            | 65          |        Green                                     |                 | 'N'                            |true        |
            | 45          |                                                  |  null           | 'A'                            |true        |
            | 0           |                                                  |  not valid      | 'U'                            |false       |