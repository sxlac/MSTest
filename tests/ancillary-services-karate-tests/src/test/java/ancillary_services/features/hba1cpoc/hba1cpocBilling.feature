@hba1cpoc
@envnot=prod
Feature: HBA1CPOC Billing Determination Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Hba1cPOCDb = function() { var Hba1cpocDb = Java.type("helpers.database.hba1cpoc.Hba1cpocDb"); return new Hba1cpocDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def expirationDate = DataGen().isoDateStamp(30)
        * def dateStamp = DataGen().isoDateStamp()
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * def memberDetails = karate.call('classpath:helpers/member/createMember.js')
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'HBA1CPOC'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @TestCaseKey=ANC-T326
    Scenario Outline: HBA1CPOC Billable
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 33070,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 33088,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>,
                },
                {
                    "AnswerId": 33264,
                    "AnsweredDateTime": '#(timestamp)',
                    "AnswerValue": '#(expirationDate)'
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
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

        # Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
        * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.A1CPercent == <answer_value>
        * match result.NormalityIndicator == <normality>
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.CenseoId == memberDetails.censeoId
        * match result.AppointmentId == appointment.appointmentId
        * match result.ProviderId == providerDetails.providerId
        * match result.ReceivedDateTime.toString() == dateStamp
        * match result.ExpirationDate.toString() == expirationDate
        * match result.DateOfBirth.toString() == memberDetails.dateOfBirth

        # Validate that the Kafka event has the expected billable status
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))    
        
        * match event.ProductCode == 'HBA1CPOC'
        * match event.IsBillable == true

        # Validate that the Kafka events include the expected event headers 
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("A1CPOC_Results", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'ResultsReceived'

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 12345
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['HBA1CPOC'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate that the billing details are as expected using EvaluationId in HBA1CPOCBilling table
        * def billingResult = Hba1cPOCDb().getBillingResultsByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.ProviderId == providerDetails.providerId
        * match billingResult.AddressLineOne == memberDetails.address.address1
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

        # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = HBA1CPOCPerformed - Status 5 = BillRequestSent - Status 6 = BillableEventRecieved
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 1 && 5 && 6

        # Validate BillRequestSent Message in Kafka
        * json billevent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("A1CPOC_Status", evaluation.evaluationId + '', "BillRequestSent", 10, 15000))
        * match billevent.EvaluationId == evaluation.evaluationId
        * match billevent.MemberPlanId == appointment.memberPlanId
        * match billevent.ProviderId.toString() == appointment.providerId
        * match billevent.PdfDeliveryDate contains dateStamp
        * match billevent.CreateDate contains dateStamp
        * match billevent.ReceivedDate contains dateStamp

        Examples:
            | answer_value | normality |
            | '4'          | 'N'       |

    @TestCaseKey=ANC-T692
    Scenario Outline: HBA1CPOC Bill Accepted with billId present
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 33070,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": 1
                },
                {
                    "AnswerId": 33088,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <answer_value>,
                },
                {
                    "AnswerId": 33264,
                    "AnsweredDateTime": '#(timestamp)',
                    "AnswerValue": '#(expirationDate)'
                },
                {
                    "AnswerId": 22034,
                    "AnsweredDateTime": '#(dateStamp)',
                    "AnswerValue": '#(dateStamp)'
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

        # Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
        * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.A1CPercent == <answer_value>
        * match result.NormalityIndicator == <normality>
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.CenseoId == memberDetails.censeoId
        * match result.AppointmentId == appointment.appointmentId
        * match result.ProviderId == providerDetails.providerId
        * match result.ReceivedDateTime.toString() == dateStamp

        # Validate that the Kafka event has the expected billable status
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))    
        
        * match event.ProductCode == 'HBA1CPOC'
        * match event.IsBillable == true

        # Validate that the Kafka events include the expected event headers 
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("A1CPOC_Results", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'ResultsReceived'

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 12345
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['HBA1CPOC'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate that the billing details are as expected using EvaluationId in HBA1CPOCBilling table
        * def billingResult = Hba1cPOCDb().getBillingResultsByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.ProviderId == providerDetails.providerId
        * match billingResult.AddressLineOne == memberDetails.address.address1
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

        # Publish the RCM bill accepted event to rcm_bill 
        * string rcmBillId = billingResult.BillId
        * string productCode = "HBA1CPOC"
        * string rcmBillTopic = "rcm_bill"
        * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
        * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)'}
        * kafkaProducerHelper.send(rcmBillTopic, "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)

        # Validate that the billing details were updated as expected 
        * def billAcceptedResult = Hba1cPOCDb().getBillingResultsByEvaluationId(evaluation.evaluationId)[0]
        * match billAcceptedResult.Accepted != null
        * match billAcceptedResult.AcceptedAt != null

        # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        # Status 1 = HBA1CPOCPerformed - Status 5 = BillRequestSent - Status 6 = BillableEventRecieved
        * match examStatusResults[*].HBA1CPOCStatusCodeId contains 1 && 5 && 6
        
        Examples:
            | answer_value | normality |
            | '4'          | 'N'       |


        @TestCaseKey=ANC-T693
        @ignore
        Scenario Outline: HBA1CPOC Bill Accepted billId not present
        * set evaluation.answers =
        """
        [
            {
                "AnswerId": 33070,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": 1
            },
            {
                "AnswerId": 33088,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": <answer_value>,
            },
            {
                "AnswerId": 33264,
                "AnsweredDateTime": '#(timestamp)',
                "AnswerValue": '#(expirationDate)'
            },
            {
                "AnswerId": 22034,
                "AnsweredDateTime": '#(dateStamp)',
                "AnswerValue": '#(dateStamp)'
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

    # Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
    * def result = Hba1cPOCDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
    * match result.A1CPercent == <answer_value>
    * match result.NormalityIndicator == <normality>
    * match result.MemberPlanId == memberDetails.memberPlanId
    * match result.CenseoId == memberDetails.censeoId
    * match result.AppointmentId == appointment.appointmentId
    * match result.ProviderId == providerDetails.providerId
    * match result.ReceivedDateTime.toString() == dateStamp

    # Validate that the Kafka event has the expected billable status
    * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("A1CPOC_Results", evaluation.evaluationId + '', "ResultsReceived", 10, 5000))    
    
    * match event.ProductCode == 'HBA1CPOC'
    * match event.IsBillable == true

    # Validate that the Kafka events include the expected event headers 
    * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("A1CPOC_Results", evaluation.evaluationId + '', 10, 5000)
    * match headers contains 'ResultsReceived'

    # Publish the PDF event to the pdfdelivery topic
    * string pdfEventKey = evaluation.evaluationId
    * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
    * def batchId = 12345
    # Karate is not letting this this string be multi-line, sorry
    * def eventId = DataGen().uuid()
    * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
    * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['HBA1CPOC'],'BatchId': #(batchId)}
    * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

    # Validate that the billing details are as expected using EvaluationId in HBA1CPOCBilling table
    * def billingResult = Hba1cPOCDb().getBillingResultsByEvaluationId(evaluation.evaluationId)[0]
    * match billingResult.ProviderId == providerDetails.providerId
    * match billingResult.AddressLineOne == memberDetails.address.address1
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

    # Publish the RCM bill accepted event to rcm_bill 
    * string rcmBillId = DataGen().uuid()
    * string productCode = "HBA1CPOC"
    * string rcmBillTopic = "rcm_bill"
    * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
    * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)'}
    * kafkaProducerHelper.send(rcmBillTopic, "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)

    # Validate that the billing details were updated as expected 
    * def billAcceptedResult = Hba1cPOCDb().getBillingResultsByEvaluationId(evaluation.evaluationId)[0]
    * match billAcceptedResult.Accepted == null
    * match billAcceptedResult.AcceptedAt == null

    # Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
    * def examStatusResults = Hba1cPOCDb().getExamStatusByEvaluationId(evaluation.evaluationId)
    # Status 1 = HBA1CPOCPerformed - Status 5 = BillRequestSent - Status 6 = BillableEventRecieved
    * match examStatusResults[*].HBA1CPOCStatusCodeId contains 1 && 5 && 6
    
    Examples:
        | answer_value | normality |
        | '4'          | 'N'       |