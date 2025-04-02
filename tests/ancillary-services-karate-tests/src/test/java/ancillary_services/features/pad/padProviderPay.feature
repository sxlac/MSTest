@pad
@envnot=prod
Feature: PAD Provider Pay Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type("helpers.data.DataGen"); return new DataGen(); }
        * def PadDb = function() { var PadDb = Java.type("helpers.database.pad.PadDb"); return new PadDb(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }

        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def pdfDeliveryDate = DataGen().utcTimestamp()
        * def cdiDateTime = DataGen().timestampWithOffset("-05:00", -1)
        * def monthDayYearCdi = DataGen().getMonthDayYear(cdiDateTime)

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call("classpath:helpers/eval/startEval.feature").response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @ignore
    @TestCaseKey=ANC-T698    
    Scenario Outline: PAD Provider Pay
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 12345
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['PAD'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry in the ProviderPay table
        * def providerPayResult = PadDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.PaymentId != null
        * match providerPayResult.PADId != null
        * match providerPayResult.ProviderPayId != null
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.PADId == evalResult.PADId
        * match providerPayResult.AddressLineOne == memberDetails.address.address1
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberId == memberDetails.memberId
        * match providerPayResult.CenseoId == memberDetails.censeoId
        * match providerPayResult.AddressLineTwo == memberDetails.address.address2
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.UserName == 'karate'
        * match providerPayResult.FirstName == memberDetails.firstName
        * match providerPayResult.ZipCode == memberDetails.address.zipCode
        * match providerPayResult.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match providerPayResult.City == memberDetails.address.city
        * match providerPayResult.MiddleName == memberDetails.middleName
        * match providerPayResult.AppointmentId == appointment.appointmentId
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.LastName == memberDetails.lastName
        * match providerPayResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the entry in the PDFToClient table
        * def pdfToClientResult = PadDb().getPdfToClientResultsWithEvalId(evaluation.evaluationId)[0]
        * match pdfToClientResult.BatchId == batchId
        * match pdfToClientResult.BatchName == batchName
        * match pdfToClientResult.PADId == evalResult.PADId

        # Validate the statuses from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'ProviderPayableEventReceived'
        * match statusResults[*].StatusCode contains 'ProviderPayRequestSent'

        # Validate that the Kafka event has the expected payment id and Provider Pay Product Code
        * json padStatus = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000)) 
        * match padStatus.ProviderId == providerDetails.providerId
        * match padStatus.MemberPlanId == memberDetails.memberPlanId
        * match padStatus.CreateDate == '#notnull'
        * match padStatus.ProviderPayProductCode == 'PAD'
        * match padStatus.ReceivedDate == '#notnull'
        * match padStatus.ProductCode == 'PAD'
        * match padStatus.EvaluationId == evaluation.evaluationId
        * assert DataGen().compareUtcDatesString(padStatus.PdfDeliveryDate, pdfDeliveryDate)
        * match padStatus.PaymentId == providerPayResult.PaymentId

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("providerpay_internal", providerPayResult.PaymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == providerPayResult.PaymentId
        * match paymentRequested["event.providerId"] == providerPayResult.ProviderId
        * match paymentRequested["event.productCode"] == 'PAD'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * assert paymentRequested["event.dateOfService"] == utcDateTime.split('T')[0]
        * match paymentRequested["event.personId"] == providerPayResult.CenseoId
        * match paymentRequested["event.commonClientId"] == providerPayResult.ClientId
        * match paymentRequested["event.engagementTypeOptions"] == 1
        * match paymentRequested["timestamp"] == '#notnull'
        
        Examples:
            | left_float_result | left_normality | right_float_result   | right_normality | determination |
            | 0.3               | "A"            | 0.6                  | "A"             | "A"           |
            | 99                | "U"            | 0.3                  | "A"             | "A"           |
            | 0.3               | "A"            | 1                    | "N"             | "A"           |
            | 99                | "U"            | 1                    | "N"             | "N"           |
            | 1                 | "N"            | 99                   | "U"             | "N"           |

    @ignore        
    @TestCaseKey=ANC-T699    
    Scenario Outline: PAD Non-Payable
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

        # Validate that the Kafka event was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000))   
        * match event == {}

        # Validate that a Kafka event related to ProviderPay was not raised
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", evalResult.CenseoId, 10, 5000)
        * assert paymentRequestEvent.length == 0

        Examples:
            | left_float_result | left_normality | right_float_result | right_normality | determination |
            | 99                | "U"            | 99                 | "U"             | "U"           |
            
    @TestCaseKey=ANC-T571
    @TestCaseKey=ANC-T573
    Scenario Outline: PAD Payable (Business rules met) - (CDIPassedEvent) and (CDIFailedEvent With PayProvider: True). Determination - <determination>. cdiEventHeaderName - <cdiEventHeaderName>. payProvider <payProvider>.
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
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"PAD"}]}

        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Validate the entry in the ProviderPay table
        * def providerPayResult = PadDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult.PaymentId != null
        * match providerPayResult.PADId != null
        * match providerPayResult.ProviderPayId != null
        * match providerPayResult.ProviderId == providerDetails.providerId
        * match providerPayResult.PADId == evalResult.PADId
        * match providerPayResult.AddressLineOne == memberDetails.address.address1
        * match providerPayResult.EvaluationId == evaluation.evaluationId
        * match providerPayResult.MemberId == memberDetails.memberId
        * match providerPayResult.CenseoId == memberDetails.censeoId
        * match providerPayResult.AddressLineTwo == memberDetails.address.address2
        * match providerPayResult.MemberPlanId == memberDetails.memberPlanId
        * match providerPayResult.UserName == 'karate'
        * match providerPayResult.FirstName == memberDetails.firstName
        * match providerPayResult.ZipCode == memberDetails.address.zipCode
        * match providerPayResult.NationalProviderIdentifier == providerDetails.nationalProviderIdentifier
        * match providerPayResult.City == memberDetails.address.city
        * match providerPayResult.MiddleName == memberDetails.middleName
        * match providerPayResult.AppointmentId == appointment.appointmentId
        * match providerPayResult.State == memberDetails.address.state
        * match providerPayResult.LastName == memberDetails.lastName
        * match providerPayResult.ApplicationId == 'Signify.Evaluation.Service'

        # Validate the statuses from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'ProviderPayableEventReceived'
        * match statusResults[*].StatusCode contains 'ProviderPayRequestSent'
        * match statusResults[*].StatusCode contains <expectedCdiStatus>

        # Validate that the Kafka event - ProviderPayableEventReceived - was raised
        * json payableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 10, 5000))   
        * match payableEvent.EvaluationId == evaluation.evaluationId
        * match payableEvent.ProviderId == providerDetails.providerId
        * match payableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match payableEvent.MemberPlanId == memberDetails.memberPlanId
        * match payableEvent.Reason == '#notpresent'
        * match payableEvent.ProductCode == 'PAD'
        * match DataGen().RemoveMilliSeconds(payableEvent.CreateDate) == DataGen().RemoveMilliSeconds(DataGen().getUtcDateTimeString(providerPayResult.CreatedDateTime.toString()))
        * assert payableEvent.ReceivedDate.split('T')[0] == providerPayResult.ReceivedDateTime.toString().split(' ')[0]
        
        # Validate that the Kafka event - ProviderPayRequestSent - has the expected payment id and Provider Pay Product Code
        * json requestSentEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 10, 5000)) 
        * match requestSentEvent.ProviderId == providerDetails.providerId
        * match requestSentEvent.MemberPlanId == memberDetails.memberPlanId
        * match DataGen().RemoveMilliSeconds(requestSentEvent.CreateDate) == DataGen().RemoveMilliSeconds(DataGen().getUtcDateTimeString(providerPayResult.CreatedDateTime.toString()))
        * match requestSentEvent.ProviderPayProductCode == 'PAD'
        * assert requestSentEvent.ReceivedDate.split('T')[0] == providerPayResult.ReceivedDateTime.toString().split(' ')[0]
        * match requestSentEvent.ProductCode == 'PAD'
        * match requestSentEvent.EvaluationId == evaluation.evaluationId
        * match requestSentEvent.PaymentId == providerPayResult.PaymentId

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("providerpay_internal", providerPayResult.PaymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == providerPayResult.PaymentId
        * match paymentRequested["event.providerId"] == providerPayResult.ProviderId
        * match paymentRequested["event.productCode"] == 'PAD'
        * string utcDateTime = DataGen().getUtcDateTimeString(providerPayResult.DateOfService.toString())
        * assert paymentRequested["event.dateOfService"] == utcDateTime.split('T')[0]
        * match paymentRequested["event.personId"] == providerPayResult.CenseoId
        * match paymentRequested["event.commonClientId"] == providerPayResult.ClientId
        * match paymentRequested["event.engagementTypeOptions"] == 1
        * match paymentRequested["timestamp"] == '#notnull'
        * match paymentRequested["event.additionalDetails.evaluationId"] == evaluation.evaluationId.toString()
        * match paymentRequested["event.additionalDetails.appointmentId"] == appointment.appointmentId.toString()
        * match paymentRequested["event.additionalDetails.examId"] == evalResult.PADId.toString()

        Examples:
            | left_float_result | left_normality | right_float_result   | right_normality | determination | cdiEventHeaderName| payProvider   | expectedCdiStatus             |
            | 0.3               | "A"            | 0.6                  | "A"             | "A"           | "CDIPassedEvent"  | true          | "CdiPassedReceived"           |
            # | 0.3               | "A"            | 0.6                  | "A"             | "A"           | "CDIFailedEvent"  | true          | "CdiFailedWithPayReceived"    |
            | 99                | "U"            | 0.3                  | "A"             | "A"           | "CDIPassedEvent"  | true          | "CdiPassedReceived"           |
            # | 99                | "U"            | 0.3                  | "A"             | "A"           | "CDIFailedEvent"  | true          | "CdiFailedWithPayReceived"    |
            | 99                | "U"            | 1                    | "N"             | "N"           | "CDIPassedEvent"  | true          | "CdiPassedReceived"           |
            # | 99                | "U"            | 1                    | "N"             | "N"           | "CDIFailedEvent"  | true          | "CdiFailedWithPayReceived"    |
            | 0.3               | "A"            | 1                    | "N"             | "A"           | "CDIPassedEvent"  | true          | "CdiPassedReceived"           |
            # | 0.3               | "A"            | 1                    | "N"             | "A"           | "CDIFailedEvent"  | true          | "CdiFailedWithPayReceived"    |
            | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIPassedEvent"  | true          | "CdiPassedReceived"           |
            # | 1                 | "N"            | 99                   | "U"             | "N"           | "CDIFailedEvent"  | true          | "CdiFailedWithPayReceived"    |


    @TestCaseKey=ANC-T574
    @TestCaseKey=ANC-T575
    Scenario Outline: PAD Non-Payable (Business rules met/not met) - (CDIPassedEvent) and (CDIFailedEvent). Test Scenario - <testScenario>. cdiEventHeaderName - <cdiEventHeaderName>. PayProvider - <payProvider>.
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
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"PAD"}]}

        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)

        # Validate the entry in the ProviderPay table
        * def providerPayResult = PadDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'

        # Validate the statuses from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains <expectedCdiStatus>
        * match statusResults[*].StatusCode contains "ProviderNonPayableEventReceived"

        # Validate that the Kafka event - ProviderNonPayableEventReceived - was raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderNonPayableEventReceived", 10, 5000))   
        * match nonPayableEvent.EvaluationId == evaluation.evaluationId
        * match nonPayableEvent.ProviderId == providerDetails.providerId
        * match nonPayableEvent.ParentCdiEvent == <cdiEventHeaderName>
        * match nonPayableEvent.MemberPlanId == memberDetails.memberPlanId
        * match nonPayableEvent.Reason == <expectedFailReason>
        * match nonPayableEvent.ProductCode == "PAD"
        * match DataGen().RemoveMilliSeconds(nonPayableEvent.CreateDate) == DataGen().RemoveMilliSeconds(DataGen().getUtcDateTimeString(evalResult.CreatedDateTime.toString()))
        * assert nonPayableEvent.ReceivedDate.split('T')[0] == evalResult.ReceivedDateTime.toString().split(' ')[0]


        # Validate that a Kafka event related to ProviderPay was not raised
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", evalResult.CenseoId, 5, 1000)
        * assert paymentRequestEvent.length == 0

        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))   
        * match event == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))   
        * match nonPayableEvent == {}

        Examples:
            | testScenario                          | left_float_result | left_normality    | right_float_result    | right_normality   | determination | cdiEventHeaderName| payProvider   | expectedCdiStatus             | expectedFailReason                                        |
            # | "cdiFailedWithoutPayButRulesMet"      | 0.3               | "A"               | 0.6                   | "A"               | "A"           | "CDIFailedEvent"  | false         | "CdiFailedWithoutPayReceived" | "PayProvider is false for the CDIFailedEvent"             |
            # | "cdiFailedWithoutPayButRulesMet"      | 99                | "U"               | 0.3                   | "A"               | "A"           | "CDIFailedEvent"  | false         | "CdiFailedWithoutPayReceived" | "PayProvider is false for the CDIFailedEvent"             |
            # | "cdiFailedWithoutPayButRulesMet"      | 99                | "U"               | 1                     | "N"               | "N"           | "CDIFailedEvent"  | false         | "CdiFailedWithoutPayReceived" | "PayProvider is false for the CDIFailedEvent"             |
            # | "cdiFailedWithoutPayButRulesMet"      | 0.3               | "A"               | 1                     | "N"               | "A"           | "CDIFailedEvent"  | false         | "CdiFailedWithoutPayReceived" | "PayProvider is false for the CDIFailedEvent"             |
            # | "cdiFailedWithoutPayButRulesMet"      | 1                 | "N"               | 99                    | "U"               | "N"           | "CDIFailedEvent"  | false         | "CdiFailedWithoutPayReceived" | "PayProvider is false for the CDIFailedEvent"             |
            | "cdiPassedButRulesNotMet"             | 99                | "U"               | 99                    | "U"               | "U"           | "CDIPassedEvent"  | true          | "CdiPassedReceived"           | "Both Left and Right Normality Indicator are Undetermined"|
            # | "cdiFailedWithPayButRulesNotMet"      | 99                | "U"               | 99                    | "U"               | "U"           | "CDIFailedEvent"  | true          | "CdiFailedWithPayReceived"    | "Both Left and Right Normality Indicator are Undetermined"|
            # | "cdiFailedWithoutPayButNotRulesMet"   | 99                | "U"               | 99                    | "U"               | "U"           | "CDIFailedEvent"  | false         | "CdiFailedWithoutPayReceived" | "PayProvider is false for the CDIFailedEvent"             |

    @ignore
    @TestCaseKey=ANC-T608
    Scenario Outline: PAD - ProviderPay API is not triggered from the PdfDeliveredToClient event. Determination - <determination>
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

        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 12345
        # Karate is not letting this this string be multi-line, sorry
        * def eventId = DataGen().uuid()
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(pdfDeliveryDate)','BatchName': '#(batchName)','ProductCodes':['PAD'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Validate the entry in the ProviderPay table
        * def providerPayResult = PadDb().getProviderPayByEvalId(evaluation.evaluationId)[0]
        * match providerPayResult == '#null'
        
        # Validate the statuses from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode !contains 'ProviderPayableEventReceived'
        * match statusResults[*].StatusCode !contains 'ProviderPayRequestSent'

        # Validate that a Kafka event related to ProviderPay was not raised
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", evalResult.CenseoId, 5, 1000)
        * assert paymentRequestEvent.length == 0

        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))   
        * match event == {}

        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))   
        * match nonPayableEvent == {}

        Examples:
            | left_float_result | left_normality | right_float_result   | right_normality | determination |
            | 0.3               | "A"            | 1                    | "N"             | "A"           |
            | 99                | "U"            | 1                    | "N"             | "N"           |

    @TestCaseKey=ANC-T607 
    Scenario Outline: PAD Non-Payable - Not Performed. Answer value - <answer_value>
        * def reasonNotes = Faker().randomQuote()
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 29561,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': '2'
                },
                {
                    'AnswerId': 30958,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Unable to perform'
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 30971,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': #(reasonNotes)
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
    
        * def evalResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)[0]
        * match evalResult == "#notnull"

        # Validate that the database details are as expected
        * def result = PadDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
        * match result.LeftScoreAnswerValue == null
        * match result.LeftSeverityAnswerValue == null
        * match result.RightScoreAnswerValue == null
        * match result.RightSeverityAnswerValue == null
        * match result.NotPerformedId != null
        * match result.AnswerId == <answer_id>
        * match result.Notes == reasonNotes
    
        # Validate that the Kafka event details are as expected    
        * json padStatus = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "NotPerformed", 10, 5000))   
        * match padStatus.ProviderId == providerDetails.providerId
        * match padStatus.ReceivedDate contains dateStamp 
        * match padStatus.ReasonNotes == reasonNotes
        * match padStatus.ProductCode == 'PAD'
        * match padStatus.ReasonType == 'Unable to perform' 
        * match padStatus.CreateDate contains dateStamp 
        * match padStatus.Reason == <expected_reason>
        * match padStatus.MemberPlanId == memberDetails.memberPlanId
        
        # Publish the cdi event to the cdi_events topic
        * string cdiEventName = "cdi_events"
        * string cdiEventKey = evaluation.evaluationId + "_" + monthDayYearCdi
        * def eventId = DataGen().uuid()
        * string cdiEventHeader = { 'Type' : <cdiEventHeaderName>}
        * string cdiEventValue = {"RequestId":"#(eventId)","EvaluationId":"#(evaluation.evaluationId)","DateTime":"#(cdiDateTime)","Username":"karateTestUser","ApplicationId":"manual","Reason":"reschedule","PayProvider":<payProvider>,"Products":[{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"HHRA"},{"EvaluationId":"#(evaluation.evaluationId)","ProductCode":"PAD"}]}

        * kafkaProducerHelper.send(cdiEventName, cdiEventKey, cdiEventHeader, cdiEventValue)
        
        # Verify the status from the database
        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'PADNotPerformed'

        # Validate that a Kafka event related to ProviderPay was not raised
        * string paymentRequestEvent = KafkaConsumerHelper.getMessageByTopicAndHeaderAndAChildField("providerpay_internal", "PaymentRequested", evalResult.CenseoId, 5, 1000)
        * assert paymentRequestEvent.length == 0
        
        # Validate that the Kafka event - ProviderPayRequestSent - was not raised
        * json event = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayRequestSent", 5, 1000))   
        * match event == {}
        
        # Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        * json nonPayableEvent = JSON.parse(KafkaConsumerHelper.getKafkaMessageByTopicAndKeyAndHeader("PAD_Status", evaluation.evaluationId + '', "ProviderPayableEventReceived", 5, 1000))   
        * match nonPayableEvent == {}
        
        Examples:
            | answer_id | answer_value                                                     | expected_reason            |
            | 30966     | 'Technical issue (please call Mobile Support at (877) 570-9359)' | 'Technical issue'          |
            | 30967     | 'Environmental issue'                                            | 'Environmental issue'      |
            | 30968     | 'No supplies or equipment'                                       | 'No supplies or equipment' |
            | 30969     | 'Insufficient training'                                          | 'Insufficient training'    |
            | 50917     | 'Member physically unable'                                       | 'Member physically unable' |