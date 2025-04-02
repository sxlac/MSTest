@ckd
@envnot=prod
Feature: CKD Billing Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def CkdDb = function() { var CkdDb = Java.type('helpers.database.ckd.CkdDb'); return new CkdDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'CKD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
   
    @TestCaseKey=ANC-T330
    Scenario Outline: CKD Billable
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(expirationDate)'
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == <answer_value>

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in CKDRCMBilling table
        * def billingResult = CkdDb().getBillingResultByEvalId(evaluation.evaluationId)[0]
        * match billingResult.ProviderId == providerDetails.providerId
        * match billingResult.AddressLineOne == memberDetails.address.address1
        * match billingResult.CKDId == evalResult.CKDId
        * match billingResult.EvaluationId == evaluation.evaluationId
        * match billingResult.MemberId == memberDetails.memberId
        * match billingResult.CenseoId == memberDetails.censeoId
        * match billingResult.ClientId == 14
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
        * match billingResult.BillId != null
        * match billingResult.LastName == memberDetails.lastName
        * match billingResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 5 = BillRequestSent - Status 6 = BillableEventRecieved
        * match examStatusResults[*].CKDStatusCodeId contains 1 && 5 && 6

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestSent'

        # Validate the entry using CKDAnswerValue in LookupCKDAnswer table
        * def examAnswerIdValues = CkdDb().getAnswerValuesByAnswerId(<answer_id>)[0]

        # Validate that the Kafka event details are as expected for ckd_results
        * json ckdResults = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_results", evaluation.evaluationId + '', "Result", 10, 5000)) 
        * match ckdResults.ProductCode == 'CKD'
        * match ckdResults.EvaluationId == evaluation.evaluationId
        * match ckdResults.PerformedDate != null
        * match ckdResults.ReceivedDate != null
        * match ckdResults.ExpiryDate != null
        * match ckdResults.IsBillable == true
        * match ckdResults.Determination == examAnswerIdValues.NormalityIndicator
        * match ckdResults.Results[0].Type == 'Albumin'
        * match ckdResults.Results[0].Result == examAnswerIdValues.Albumin.toString()
        * match ckdResults.Results[1].Type == 'Creatinine'
        * match ckdResults.Results[1].Result == examAnswerIdValues.Creatinine.toString()
        * match ckdResults.Results[2].Type == 'uAcr'
        * match ckdResults.Results[2].Result == examAnswerIdValues.Acr

        Examples:
            | answer_id | answer_value                                           |
            | 20963     | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'             |  
        
        @TestCaseKey=ANC-T331
        Scenario Outline: CKD Billable - Null Kit Expiration Date
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': ''
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == <answer_value>

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry using EvaluationId in CKDRCMBilling table
        * def billingResult = CkdDb().getBillingResultByEvalId(evaluation.evaluationId)[0]
        * match billingResult.ProviderId == providerDetails.providerId
        * match billingResult.AddressLineOne == memberDetails.address.address1
        * match billingResult.CKDId == evalResult.CKDId
        * match billingResult.EvaluationId == evaluation.evaluationId
        * match billingResult.MemberId == memberDetails.memberId
        * match billingResult.CenseoId == memberDetails.censeoId
        * match billingResult.ClientId == 14
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
        * match billingResult.BillId != null
        * match billingResult.LastName == memberDetails.lastName
        * match billingResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 5 = BillRequestSent - Status 6 = BillableEventRecieved
        * match examStatusResults[*].CKDStatusCodeId contains 1 && 5 && 6

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestSent'

        # Validate the entry using CKDAnswerValue in LookupCKDAnswer table
        * def examAnswerIdValues = CkdDb().getAnswerValuesByAnswerId(<answer_id>)[0]

        # Validate that the Kafka event details are as expected for ckd_results
        * json ckdResults = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_results", evaluation.evaluationId + '', "Result", 10, 5000)) 
        * match ckdResults.ProductCode == 'CKD'
        * match ckdResults.EvaluationId == evaluation.evaluationId
        * match ckdResults.PerformedDate != null
        * match ckdResults.ReceivedDate != null
        * match ckdResults.ExpiryDate == null
        * match ckdResults.IsBillable == true
        * match ckdResults.Determination == examAnswerIdValues.NormalityIndicator
        * match ckdResults.Results[0].Type == 'Exception'
        * match ckdResults.Results[0].Result == 'Invalid Expiry Date'
        * match ckdResults.Results[1].Type == 'Albumin'
        * match ckdResults.Results[1].Result == examAnswerIdValues.Albumin.toString()
        * match ckdResults.Results[2].Type == 'Creatinine'
        * match ckdResults.Results[2].Result == examAnswerIdValues.Creatinine.toString()
        * match ckdResults.Results[3].Type == 'uAcr'
        * match ckdResults.Results[3].Result == examAnswerIdValues.Acr

        Examples:
            | answer_id | answer_value                                           |
            | 20963     | 'Albumin: 30 - Creatinine: 0.1 ; Abnormal'             |     
            
        
        @TestCaseKey=ANC-T333
        Scenario: CKD Non-Billable - Null Kit Expiration Date and Invalid Strip Result
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 20983,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': ''
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': ''
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == null
        * match evalResult.ExpirationDate == null

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate that the Kafka events include the expected event headers and values
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "Performed", 10, 5000))    
        * match event.ProductCode == 'CKD'
        * match event.ProviderId == providerDetails.providerId
        * match event.MemberPlanId == memberDetails.memberPlanId

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed'
        * match headers !contains 'BillRequestSent'

        # Status 6 = BillableEventRecieved takes ~5 seconds to populate in the CKDStatus table
        * eval sleep(8000)

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 5 = BillRequestSent - Status 6 = BillableEventRecieved
        * match examStatusResults[*].CKDStatusCodeId contains 1
        * match examStatusResults[*].CKDStatusCodeId !contains 5
        * match examStatusResults[*].CKDStatusCodeId !contains 6

        # Validate that the Kafka event details are as expected for ckd_results
        * json ckdResults = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_results", evaluation.evaluationId + '', "Result", 10, 5000)) 
        * match ckdResults.ProductCode == 'CKD'
        * match ckdResults.EvaluationId == evaluation.evaluationId
        * match ckdResults.Results[0].Type == 'Exception'
        * match ckdResults.Results[0].Result == 'Invalid Strip Result'
        * match ckdResults.Results[1].Type == 'Exception'
        * match ckdResults.Results[1].Result == 'Invalid Expiry Date'
        * match ckdResults.ReceivedDate != null
        * match ckdResults.ExpiryDate == null
        * match ckdResults.PerformedDate != null
        * match ckdResults.Determination == 'U'
        * match ckdResults.IsBillable == false

    @TestCaseKey=ANC-T332
    Scenario: CKD Non-Billable - Invalid Strip Result
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 20950,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 20983,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': ''
                },
                {
                    'AnswerId': 33263,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '#(expirationDate)'
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
                }
            ]
            """

        * karate.call('classpath:helpers/eval/saveEval.feature')
        * karate.call('classpath:helpers/eval/stopEval.feature')
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate the entry using EvaluationId in CKD table
        * def evalResult = CkdDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult.CKDAnswer == null

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['CKD'],'BatchId': 14245}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate that the Kafka events include the expected event headers and values
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_status", evaluation.evaluationId + '', "Performed", 10, 5000))      
        * match event.ProductCode == 'CKD'
        * match event.ProviderId == providerDetails.providerId
        * match event.MemberPlanId == memberDetails.memberPlanId

        # Validate that the Kafka events include the expected event headers
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("ckd_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed'
        * match headers !contains 'BillRequestSent'

        # Status 6 = BillableEventRecieved takes ~5 seconds to populate in the CKDStatus table
        * eval sleep(8000)

        # Validate the entry using EvaluationId in CKD & CKDStatus tables
        * def examStatusResults = CkdDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = CKDPerformed - Status 5 = BillRequestSent - Status 6 = BillableEventRecieved
        * match examStatusResults[*].CKDStatusCodeId contains 1 
        * match examStatusResults[*].CKDStatusCodeId !contains 5
        * match examStatusResults[*].CKDStatusCodeId !contains 6

        # Validate that the Kafka event details are as expected for ckd_results
        * json ckdResults = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("ckd_results", evaluation.evaluationId + '', "Result", 10, 5000)) 
        * match ckdResults.ProductCode == 'CKD'
        * match ckdResults.EvaluationId == evaluation.evaluationId
        * match ckdResults.Results[0].Type == 'Exception'
        * match ckdResults.Results[0].Result == 'Invalid Strip Result'
        * match ckdResults.ReceivedDate != null
        * match ckdResults.ExpiryDate != null
        * match ckdResults.PerformedDate != null
        * match ckdResults.Determination == 'U'
        * match ckdResults.IsBillable == false



        

    