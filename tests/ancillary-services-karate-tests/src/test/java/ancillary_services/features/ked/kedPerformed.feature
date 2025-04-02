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

    @TestCaseKey=ANC-T784
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * match evalEvent.Id == '#notnull'	

        # UACR record check
        * def result = UacrDb().getResultsByEvaluationId(evaluation.evaluationId)
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

        # Validate Exam Status Update in database
        # Validate response contains 14 - Order Requested
        * match result[*].StatusName contains "Order Requested" && "Exam Performed"
        * match result[*].ExamStatusCodeId contains 1 && 14 

        # Check if the data received via EvaluationFinalizedEvent event is written to the database properly
        * match DataGen().RemoveMilliSeconds(evalEvent.ReceivedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(evalEvent.CreatedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match evalEvent.DateOfService == DataGen().getUtcDateTimeString(result[0].DateOfService.toString()).split('+')[0]

        # Verify barcode details
        * def barcodeHistory = UacrDb().getBarcodeByExamId(result[0].ExamId)
        * match barcodeHistory[0].Barcode == evaluation.answers[2].AnswerValue+'|'+ evaluation.answers[3].AnswerValue
        
        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("uacr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match kafkaEvent.Barcode == barcodeHistory[0].Barcode
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "UACR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())

        # EGFR record check
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
        * match result[*].StatusName contains "Order Requested" && "Exam Performed"
        * match result[*].ExamStatusCodeId contains 14 && 1

        # check if the data received via EvaluationFinalizedEvent event is written to the database properly
        * match DataGen().RemoveMilliSeconds(evalEvent.ReceivedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(evalEvent.CreatedDateTime) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match evalEvent.DateOfService == DataGen().getUtcDateTimeString(result[0].DateOfService.toString()).split('+')[0]

        # Verify barcode details
        * def barcodeHistory = EgfrDb().getBarcodeHistoryByExamId(result[0].ExamId)
        * match barcodeHistory[0].Barcode == evaluation.answers[2].AnswerValue+'|'+ evaluation.answers[3].AnswerValue
        
        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "Performed", 10, 5000))
        * match kafkaEvent.Barcode == evaluation.answers[2].AnswerValue+'|'+ evaluation.answers[3].AnswerValue
        * match kafkaEvent.ProviderId == providerDetails.providerId
        * match kafkaEvent.ProductCode == "EGFR"
        * match kafkaEvent.EvaluationId == evaluation.evaluationId
        * match kafkaEvent.MemberPlanId == memberDetails.memberPlanId
        * match kafkaEvent.CreatedDate.toString().split('+')[1] == "00:00"
        * match kafkaEvent.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(kafkaEvent.CreatedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(kafkaEvent.ReceivedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())

        # OMS record check
        * def result = OmsDb().getResultsByEvaluationId(evaluation.evaluationId)
        * match result[0].ProductCodeName == "EGFR"
        * match result[0].EvaluationId == evaluation.evaluationId
        * match result[0].OrderContext contains alphaBarcode && numericBarcode
        * match result[1].ProductCodeName == "UACR"
        * match result[1].EvaluationId == evaluation.evaluationId
        * match result[1].OrderContext contains alphaBarcode && numericBarcode

    @TestCaseKey=ANC-T794
    Scenario: KED Verify Ingested lab results are added to the "Complete" directory when processed
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * match evalEvent.Id == '#notnull'

        * def KEDLabResultsFileShare = function() { var HomeAccessFileshare = Java.type('helpers.endToEndHelpers.CsvOverreadHelpers'); return new HomeAccessFileshare(); }
        * def fileName = `Test_${DataGen().randomInteger(00000, 99999)}.csv`
        * def DateResultReported = "12/20/2023 1:08"
        * def UacrUrineAlbuminToCreatinineRatioResultLabValue = "29.999"
        * def CreatinineResultLabValue = "0.92"
        * def EstimatedGlomerularFiltrationRateResultLabValue = "66.01"
        * def DateLabReceived = "21/12/2023 19:46"
        # Drop file to Pending folder

        * KEDLabResultsFileShare().createAndDropKedCsvToPendingFolder(fileName, DateResultReported, evaluation.evaluationId, UacrUrineAlbuminToCreatinineRatioResultLabValue, CreatinineResultLabValue, EstimatedGlomerularFiltrationRateResultLabValue, DateLabReceived)
        * KEDLabResultsFileShare().checkKedCsvMovedToCompleteFolder(fileName, 200, 300)

        # Record is present in Quest and not present in Lab Result
        * def QuestlabResults = EgfrDb().checkQuestLabResultsRecordPresentByEvaluationId(evaluation.evaluationId)[0]
        * match QuestlabResults == null

        * def labResults = EgfrDb().getLabResultsRecordByEvaluationId(evaluation.evaluationId)
        * match labResults[0].EvaluationId == evaluation.evaluationId        

        # Verify Kafka message published KedEgfrLabResult
        * json egfrKafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_labresult_egfr", evaluation.evaluationId + '', "KedEgfrLabResult", 10, 5000))
        * match egfrKafkaEvent.EstimatedGlomerularFiltrationRateResultColor == null
        * match egfrKafkaEvent.EvaluationId == evaluation.evaluationId
        * match egfrKafkaEvent.DateLabReceived == "2023-12-21T19:46:00+00:00"
        * match egfrKafkaEvent.EstimatedGlomerularFiltrationRateResultDescription == "ESTIMATED_GLOMERULAR_FILTRATION_RATE_ResultDescription"
        * match egfrKafkaEvent.EgfrResult == EstimatedGlomerularFiltrationRateResultLabValue * 1

        # Verify Kafka message published KedUacrLabResult
        * json uacrKafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_labresult_uacr", evaluation.evaluationId + '', "KedUacrLabResult", 10, 5000))
        * match uacrKafkaEvent.CreatinineResult == CreatinineResultLabValue * 1
        * match uacrKafkaEvent.EvaluationId == evaluation.evaluationId
        * match uacrKafkaEvent.UrineAlbuminToCreatinineRatioResultColor == null
        * match uacrKafkaEvent.UrineAlbuminToCreatinineRatioResultDescription == "UACR_(URINE_ALBUMIN_TO_CREATININE_RATIO)_ResultDescription"
        * match uacrKafkaEvent.UacrResult == UacrUrineAlbuminToCreatinineRatioResultLabValue   * 1
        * match uacrKafkaEvent.DateLabReceived == "2023-12-21T19:46:00+00:00"

    @TestCaseKey=ANC-T798
    Scenario: KED Verify Ingested lab results are added to the "Invalid" directory when processed 1 entry with DateResultReported null
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

        * json evalEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("evaluation", evaluation.evaluationId + '', "EvaluationFinalizedEvent", 10, 5000))
        * match evalEvent.Id == '#notnull'

        * def KEDLabResultsFileShare = function() { var HomeAccessFileshare = Java.type('helpers.endToEndHelpers.CsvOverreadHelpers'); return new HomeAccessFileshare(); }
        * def fileName = `Test_${DataGen().randomInteger(00000, 99999)}.csv`
        * def DateResultReported = ""
        * def UacrUrineAlbuminToCreatinineRatioResultLabValue = "29.999"
        * def CreatinineResultLabValue = "0.92"
        * def EstimatedGlomerularFiltrationRateResultLabValue = "66"
        * def DateLabReceived = "21/12/2023 19:46"
        
        # Drop file to Pending folder
        * KEDLabResultsFileShare().createAndDropKedCsvToPendingFolder(fileName, DateResultReported, evaluation.evaluationId, UacrUrineAlbuminToCreatinineRatioResultLabValue, CreatinineResultLabValue, EstimatedGlomerularFiltrationRateResultLabValue, DateLabReceived)
        * KEDLabResultsFileShare().checkKedCsvMovedToInvalidFolder(fileName, 200, 300)

        # Verify Kafka message published KedEgfrLabResult
        * json egfrKafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_labresult_egfr", evaluation.evaluationId + '', "KedEgfrLabResult", 10, 5000))
        * match egfrKafkaEvent == {}

        # Verify Kafka message published KedUacrLabResult
        * json uacrKafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_labresult_uacr", evaluation.evaluationId + '', "KedUacrLabResult", 10, 5000))
        * match uacrKafkaEvent == {}
