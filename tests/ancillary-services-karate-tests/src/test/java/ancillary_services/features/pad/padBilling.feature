@pad
@envnot=prod
Feature: PAD Billing Determination Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type("helpers.data.DataGen"); return new DataGen(); }
        * def PadDb = function() { var PadDb = Java.type("helpers.database.pad.PadDb"); return new PadDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }

    @TestCaseKey=ANC-T363
    Scenario Outline: PAD Billable - <left_normality><right_normality><determination>
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29560,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 29564,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<left_float_result>"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_float_result>"
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

        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")
        * def evalResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#notnull"
        
        # Validate that the Kafka event has the expected billable status
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))   
        
        * match event.IsBillable == true

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 12345
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['PAD'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry in the PADRCMBilling table
        * def billingResult = PadDb().getBillingResultsWithEvalId(evaluation.evaluationId)[0]
        * match billingResult.BillId != null
        * match billingResult.PADId != null
        * match billingResult.Id != null
        * match billingResult.ProviderId == providerDetails.providerId
        * match billingResult.PADId == evalResult.PADId
        * match billingResult.AddressLineOne == memberDetails.address.address1
        * match billingResult.EvaluationId == evaluation.evaluationId
        * match billingResult.MemberId == memberDetails.memberId
        * match billingResult.CenseoId == memberDetails.censeoId
        * match billingResult.AddressLineTwo == memberDetails.address.address2
        * match billingResult.MemberPlanId == memberDetails.memberPlanId
        * match billingResult.UserName == 'karate'
        * match billingResult.FirstName == memberDetails.firstName
        * match billingResult.ZipCode == memberDetails.address.zipCode
        * match billingResult.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match billingResult.City == memberDetails.address.city
        * match billingResult.MiddleName == memberDetails.middleName
        * match billingResult.AppointmentId == appointment.appointmentId
        * match billingResult.State == memberDetails.address.state
        * match billingResult.LastName == memberDetails.lastName
        * match billingResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the entry in the PDFToClient table
        * def pdfToClientResult = PadDb().getPdfToClientResultsWithEvalId(evaluation.evaluationId)[0]
        * match pdfToClientResult.BatchId == batchId
        * match pdfToClientResult.BatchName == batchName
        * match pdfToClientResult.PADId == evalResult.PADId

        # Validate the statuses from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'BillableEventRecieved'
        * match statusResults[*].StatusCode contains 'BillRequestSent'
         
        # Validate that the Kafka event - BillRequestSent - has the expected bill id and Product Code
        * json billSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000)) 
        * match billSentEvent.ProviderId == providerDetails.providerId
        * match billSentEvent.MemberPlanId == memberDetails.memberPlanId
        * match billSentEvent.CreateDate == '#notnull'
        * match billSentEvent.BillingProductCode == 'PAD'
        * assert billSentEvent.ReceivedDate.split('T')[0] == billingResult.ReceivedDateTime.toString().split(' ')[0]
        * match billSentEvent.ProductCode == 'PAD'
        * match billSentEvent.EvaluationId == evaluation.evaluationId
        * match billSentEvent.PdfDeliveryDate contains timestamp
        * match billSentEvent.BillId == billingResult.BillId
        
        Examples:
            | left_float_result | left_normality | right_float_result   | right_normality | determination |
            | 0.3               | "A"            | 0.6                  | "A"             | "A"           |
            | 99                | "U"            | 0.3                  | "A"             | "A"           |
            | 99                | "U"            | 1                    | "N"             | "N"           |
            | 0.3               | "A"            | 1                    | "N"             | "A"           |
            | 1                 | "N"            | 99                   | "U"             | "N"           |
            | 1                 | "N"            | 0.6                  | "A"             | "A"           |

    @TestCaseKey=ANC-T728
    Scenario Outline: PAD Billable - Validate BillRequestAccepted - <determination>
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29560,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 29564,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<left_float_result>"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_float_result>"
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

        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")
        * def evalResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#notnull"
        
        # Validate that the Kafka event has the expected billable status
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))   
        
        * match event.IsBillable == true

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 12345
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['PAD'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry in the PADRCMBilling table
        * def billingResult = PadDb().getBillingResultsWithEvalId(evaluation.evaluationId)[0]
        * match billingResult.BillId != null
        * match billingResult.PADId != null
        * match billingResult.Id != null
        * match billingResult.ProviderId == providerDetails.providerId
        * match billingResult.PADId == evalResult.PADId
        * match billingResult.AddressLineOne == memberDetails.address.address1
        * match billingResult.EvaluationId == evaluation.evaluationId
        * match billingResult.MemberId == memberDetails.memberId
        * match billingResult.CenseoId == memberDetails.censeoId
        * match billingResult.AddressLineTwo == memberDetails.address.address2
        * match billingResult.MemberPlanId == memberDetails.memberPlanId
        * match billingResult.UserName == 'karate'
        * match billingResult.FirstName == memberDetails.firstName
        * match billingResult.ZipCode == memberDetails.address.zipCode
        * match billingResult.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match billingResult.City == memberDetails.address.city
        * match billingResult.MiddleName == memberDetails.middleName
        * match billingResult.AppointmentId == appointment.appointmentId
        * match billingResult.State == memberDetails.address.state
        * match billingResult.LastName == memberDetails.lastName
        * match billingResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the entry in the PDFToClient table
        * def pdfToClientResult = PadDb().getPdfToClientResultsWithEvalId(evaluation.evaluationId)[0]
        * match pdfToClientResult.BatchId == batchId
        * match pdfToClientResult.BatchName == batchName
        * match pdfToClientResult.PADId == evalResult.PADId

        # Validate the statuses from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'BillableEventRecieved'
        * match statusResults[*].StatusCode contains 'BillRequestSent'

        # Validate that the Kafka event - BillRequestSent - has the expected bill id and Product Code
        * json billSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000)) 
        * match billSentEvent.ProviderId == providerDetails.providerId
        * match billSentEvent.MemberPlanId == memberDetails.memberPlanId
        * match billSentEvent.CreateDate == '#notnull'
        * match billSentEvent.BillingProductCode == 'PAD'
        * assert billSentEvent.ReceivedDate.split('T')[0] == billingResult.ReceivedDateTime.toString().split(' ')[0]
        * match billSentEvent.ProductCode == 'PAD'
        * match billSentEvent.EvaluationId == evaluation.evaluationId
        * match billSentEvent.PdfDeliveryDate contains timestamp
        * match billSentEvent.BillId == billingResult.BillId

        # # # UnCommenting out the publish to topic even though T-Checks automatically publishes this event, since there is a delay attimes to the event being published by T-Checks
        # Publish the RCM bill accepted event to rcm_bill 
        * string rcmBillId = billingResult.BillId
        * string productCode = "PAD"
        * string rcmBillTopic = "rcm_bill"
        * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
        * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)'}
        * kafkaProducerHelper.send(rcmBillTopic, "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)

        # Validate that the billing details were updated as expected 
        * def billAcceptedResult = PadDb().getBillingResultsWithEvalId(evaluation.evaluationId)[0]
        * match billAcceptedResult.Accepted == true
        * match billAcceptedResult.AcceptedAt == '#notnull'
        
        Examples:
            | left_float_result | left_normality | right_float_result   | right_normality | determination |
            | 0.3               | "A"            | 0.6                  | "A"             | "A"           |
            | 99                | "U"            | 1                    | "N"             | "N"           |
    
    @TestCaseKey=ANC-T359
    Scenario Outline: PAD Non-Billable
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29560,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 29564,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<left_float_result>"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "<right_float_result>"
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

        * karate.call("classpath:helpers/eval/saveEval.feature")
        * karate.call("classpath:helpers/eval/stopEval.feature")
        * karate.call("classpath:helpers/eval/finalizeEval.feature")
        
        # Validate that the Kafka event has the expected billable status
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))   

        * match event.IsBillable == false

        Examples:
            | left_float_result | left_normality | right_float_result | right_normality | determination |
            | 99                | "U"            | 99                 | "U"             | "U"           |

    @TestCaseKey=ANC-T669
    @TestCaseKey=ANC-T668
    Scenario Outline: PAD Not Performed - BillRequestNotSent Status <reason_type_value>
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 29561,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '2'
                },
                {
                    'AnswerId': <reason_type_answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <reason_type_value>
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
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

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['PAD'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate Exam in NotPerformed table
        * def result = PadDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.LeftScoreAnswerValue == null
        * match result.LeftSeverityAnswerValue == null
        * match result.RightScoreAnswerValue == null
        * match result.RightSeverityAnswerValue == null
        * match result.NotPerformedId != null
        * match result.AnswerId == <answer_id>

        # Validate that the Kafka event details are as expected  
        * json padStatus = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))    
        * match padStatus.ProviderId == providerDetails.providerId
        * match padStatus.ReceivedDate contains dateStamp 
        * match padStatus.ProductCode == 'PAD'
        * match padStatus.ReasonType == <reason_type_value> 
        * match padStatus.CreateDate contains dateStamp 
        * match padStatus.Reason == <expected_reason>
        * match padStatus.MemberPlanId == memberDetails.memberPlanId

        # Validate BillRequestNotSent Status in Kafka
        # Need extra time to remedy kafka message taking an extraordinary long time to be generated in Docker for ADO pipeline runs
        * eval sleep(5000)
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "BillRequestNotSent", 10, 15000))
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.PdfDeliveryDate contains dateStamp
        * match event.CreateDate contains dateStamp
        * match event.ReceivedDate contains dateStamp

        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("PAD_Status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'NotPerformed' && 'BillRequestNotSent'

        # Verify the status from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'PADNotPerformed' && 'BillRequestNotSent'

        Examples:
            |reason_type_answer_id|  reason_type_value | answer_id | answer_value               | expected_reason            |
            |30958                | 'Unable to perform'| 30968     | 'No supplies or equipment' | 'No supplies or equipment' |
            |30957                | 'Member refused'   | 30962     | 'Not interested'           | 'Not interested'           |