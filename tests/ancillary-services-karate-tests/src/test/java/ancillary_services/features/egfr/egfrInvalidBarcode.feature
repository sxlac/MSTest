Feature: Validate ProviderPay and Billing flows when the BarCode is Invalid

    Background: Prepare Evaluation and helper objects
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'EGFR', 'UACR'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

    @TestCaseKey=ANC-T1080
    Scenario: eGFR Performed with Invalid BarCode - verify no OrderCreation, ProviderPay and Billing
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
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": "#(dateStamp)",
                    "AnswerValue": "#(dateStamp)"
                },
                {
                    "AnswerId": 52484,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": #(DataGen().GetInvalidBarcode())
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
        
        * def result = EgfrDb().getResultsByEvaluationId(evaluation.evaluationId)
        # Validate Exam Status Update in database
        * match result[*].StatusName !contains ["Order Requested"]
        * match result[*].ExamStatusCodeId !contains [14]

        # Verify barcode details
        * def barcodeHistory = EgfrDb().getBarcodeHistoryByExamId(result[0].ExamId)
        * match barcodeHistory[0].Barcode == evaluation.answers[3].AnswerValue+'|'+ evaluation.answers[4].AnswerValue

        # Verify Kafka message contains Performed header 
        * json kafkaEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("dps_oms_order", evaluation.evaluationId + '', "OrderCreationEvent", 10, 5000))
        * print kafkaEvent
        * match kafkaEvent == {}

        # Publish the homeaccess lab results
        * string homeAccessResultsReceivedHeader = {'Type': 'KedEgfrLabResult'}
        * string homeaccessTopic = "dps_labresult_egfr"
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result[0].DateOfService.toString())
        * string resultsReceivedValue = {'EvaluationId': '#(parseInt(evaluation.evaluationId))','DateLabReceived': '#(ProperDateOfService)',,'EgfrResult': '65','EstimatedGlomerularFiltrationRateResultDescription': '','EstimatedGlomerularFiltrationRateResultColor': 'Green'}
        * kafkaProducerHelper.send(homeaccessTopic, memberDetails.censeoId, homeAccessResultsReceivedHeader, resultsReceivedValue)
        * eval sleep(2000)

        # Validate the entry in the ProviderPay table
        * def providerPay = EgfrDb().getProviderPayResultByEvaluationId(evaluation.evaluationId)[0]
        * match providerPay.PaymentId == "#notnull"
        * match providerPay.ExamId == "#notnull"

        # Validate ProviderPayRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))
        * match event.ProviderPayProductCode == 'EGFR' 
        * match event.ProductCode == 'EGFR'
        * match event.PaymentId == providerPay.PaymentId.toString()
        * match event.ParentEventDateTime == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['EGFR'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue) 
        * eval sleep(2000) 
        
        # Validate the entry in the BillRequestSent table
        * def billingResult = EgfrDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId == '#notnull'
        * match billingResult.ExamId == '#notnull'
        * match billingResult.BillingProductCode == 'eGFR'

        # Validate record present in table PdfDeliveredToClient in database
        * def resultPdfDeliveredToClient = EgfrDb().getPdfDeliveredToClientByEvaluationId(evaluation.evaluationId)[0]
        * match resultPdfDeliveredToClient.EvaluationId == evaluation.evaluationId

        # Validate BillRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("egfr_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match event.BillingProductCode == 'EGFR' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate == '#notnull'
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId

        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreatedDate.toString().split('+')[1] == "00:00"
        * match event.ReceivedDate.toString().split('+')[1] == "00:00"
        * match DataGen().RemoveMilliSeconds(event.CreatedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationCreatedDateTime.toString())
        * match DataGen().RemoveMilliSeconds(event.ReceivedDate) == DataGen().getUtcDateTimeString(result[0].EvaluationReceivedDateTime.toString())
        
        # Validate Exam Status Update in database
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received", "ProviderPayRequestSent", "ProviderPayableEventReceived","CDIPassedReceived","Bill Request Sent","Billable Event Received"])
        # Validate response contains 1 - Exam Performed, 6 - Lab Results Received, 8 - ProviderPayableEventReceived, 9 - ProviderPayRequestSent, relevant cdi event
        * match result[*].ExamStatusCodeId contains [1, 3, 4, 6, 8, 9, 11]