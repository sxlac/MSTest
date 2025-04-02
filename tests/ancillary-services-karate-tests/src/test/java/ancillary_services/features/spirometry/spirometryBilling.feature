@spirometry
@envnot=prod
Feature: Spirometry Billing Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def SpiroDb = function() { var SpiroDb = Java.type('helpers.database.spirometry.SpirometryDb'); return new SpiroDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'SPIROMETRY'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')

    @TestCaseKey=ANC-T707
    Scenario Outline: Spirometry Performed with Normal/Abnormal Normality- BillRequestSent Status-Evaluation Finalized Before PDFDelivery Sent
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
                    "AnswerValue": <fvc>
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1>
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1_fvc>
                },
                {
                    "AnswerId": <symptom_support_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <symptom_support_answer_value>
                },        
                {
                    "AnswerId": <risk_factors_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <risk_factors_answer_value>
                },
                {
                    "AnswerId": <comorbidity_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <comorbidity_answer_value>
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['SPIROMETRY'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)
        
        * def evalResult = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Validate the entry in the BillRequestSent table
        * def billingResult = SpiroDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId != null
        * match billingResult.SpirometryExamId == evalResult.SpirometryExamId
        
        # Validate BillRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match event.BillingProductCode == 'SPIROMETRY' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate == '#notnull' 
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreateDate == '#notnull' 
        * match event.ReceivedDate == '#notnull' 
        
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("spirometry_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestSent'

        # Validate Exam Status Update in database
        * def examStatus = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatus[*].Name contains 'Spirometry Exam Performed' && 'Client PDF Delivered' && 'Bill Request Sent'

        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc |symptom_support_answer_id| symptom_support_answer_value | risk_factors_answer_id | risk_factors_answer_value | comorbidity_answer_id |comorbidity_answer_value | normality  |
        | 50938            | "B"                 | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Abnormal" |
        | 50938            | "B"                 | 70  | 100  | 0.7      | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Normal" |
        | 51947            | "A"                 | 70  | 100  | 0.7      | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Normal"   |

    @TestCaseKey=ANC-T708
    Scenario Outline: Spirometry Performed with Normal/Abnormal Normality- BillRequestSent Status-PDFDelivery Sent Before EvaluationFinalized Event
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
                    "AnswerValue": <fvc>
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1>
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1_fvc>
                },
                {
                    "AnswerId": <symptom_support_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <symptom_support_answer_value>
                },        
                {
                    "AnswerId": <risk_factors_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <risk_factors_answer_value>
                },
                {
                    "AnswerId": <comorbidity_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <comorbidity_answer_value>
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['SPIROMETRY'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)
        
        # Finalize Evaluation
        * karate.call('classpath:helpers/eval/finalizeEval.feature')
        * def evalResult = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Validate the entry in the BillRequestSent table
        * def billingResult = SpiroDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId != null
        * match billingResult.SpirometryExamId == evalResult.SpirometryExamId
        
        # Validate BillRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "BillRequestSent", 10, 15000))
        * match event.BillingProductCode == 'SPIROMETRY' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate contains dateStamp 
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreateDate contains dateStamp 
        * match event.ReceivedDate contains dateStamp
        
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("spirometry_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestSent'

        # Validate Exam Status Update in database
        * def examStatus = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatus[*].Name contains 'Spirometry Exam Performed' && 'Client PDF Delivered' && 'Bill Request Sent'

        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc |symptom_support_answer_id| symptom_support_answer_value | risk_factors_answer_id | risk_factors_answer_value | comorbidity_answer_id |comorbidity_answer_value | normality  |
        | 50938            | "B"                 | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Abnormal" |
        | 50938            | "B"                 | 70  | 100  | 0.7      | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Normal"   |
        | 51947            | "A"                 | 70  | 100  | 0.7      | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Normal"   |

    @TestCaseKey=ANC-T709
    Scenario Outline: Spirometry Performed with Undetermined Normality - BillRequestNotSent Status
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
                    "AnswerValue": <fvc>
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1>
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1_fvc>
                },
                {
                    "AnswerId": <symptom_support_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <symptom_support_answer_value>
                },        
                {
                    "AnswerId": <risk_factors_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <risk_factors_answer_value>
                },
                {
                    "AnswerId": <comorbidity_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <comorbidity_answer_value>
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['SPIROMETRY'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

         # Publish the Overread event to the overread_spirometry topic
         * string overrreadEventKey = appointment.appointmentId
         * def OverreadId = DataGen().uuid()
 
         * string overreadHeader = {'Type': 'OverreadProcessed'}
         * string overreadEventValue = {'OverreadId': '#(OverreadId)','MemberId':'','AppointmentId': '#(appointment.appointmentId)','SessionId': '#(OverreadId)','PerformedDateTime': '#(timestamp)','OverreadDateTime': '#(timestamp)','BestTestId': '#(OverreadId)','BestFvcTestId': '#(OverreadId)','BestFvcTestComment': 'TestComment','BestFev1TestId': '#(OverreadId)','BestFev1TestComment': 'TestComment','BestPefTestId': '#(OverreadId)','BestPefTestComment': 'TestComment','Comment': 'TestComment','Fev1FvcRatio':0.5,'OverreadBy': 'JohnDoe','ObstructionPerOverread':"INCONCLUSIVE",'ReceivedDateTime': '#(timestamp)'}
         * kafkaProducerHelper.send("overread_spirometry", overrreadEventKey, overreadHeader, overreadEventValue) 
        
        # Finalize Evaluation
        * karate.call('classpath:helpers/eval/finalizeEval.feature')
    
        # Validate BillRequestNotSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "BillRequestNotSent", 10, 15000))
        * match event.PdfDeliveryDate contains dateStamp
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreateDate contains dateStamp
        * match event.ReceivedDate contains dateStamp
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("spirometry_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestNotSent'

        # Validate Exam Status Update in database
        * def examStatus = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatus[*].Name contains 'Spirometry Exam Performed' && 'Client PDF Delivered' && 'Bill Request Not Sent'


        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc |symptom_support_answer_id| symptom_support_answer_value | risk_factors_answer_id | risk_factors_answer_value | comorbidity_answer_id |comorbidity_answer_value | normality  |
        | 50940            |"D"                  | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Undetermined" |
    
    @parallel=false
    @TestCaseKey=ANC-T710
    Scenario Outline: Spirometry Not Performed - BillRequestNotSent Status <reason_type_value>
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * set evaluation.answers =
            """
            [
                {
                    'AnswerId': 50920,
                    'AnsweredDateTime': '#(timestamp)',
                    "AnswerValue": 'No'
                },
                {
                    'AnswerId':<reason_type_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <reason_type_value>
                },
                {
                    'AnswerId': <answer_id>,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': <answer_value>
                },
                {
                    'AnswerId': 50927,
                    'AnsweredDateTime': '#(timestamp)',
                    'AnswerValue': 'Test Comment'
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['SPIROMETRY'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)

        # Finalize Evaluation
        * karate.call('classpath:helpers/eval/finalizeEval.feature')

        # Validate exam details in ExamNotPerformed table
        * def result = SpiroDb().getNotPerformedByEvaluationId(evaluation.evaluationId)[0]
        * match result.EvaluationId == evaluation.evaluationId
        * match result.MemberPlanId == appointment.memberPlanId
        * match result.ExamNotPerformedId != null
        * match result.NotPerformedReasonId == <expected_not_performed_reason_id>
        * match result.Notes == 'Test Comment'
        * match result.CenseoId == memberDetails.censeoId
        * match result.AppointmentId == appointment.appointmentId
        * match result.ProviderId.toString() == appointment.providerId

        # Validate that the NotPerformed message is published to Kafka
        * json notPerformedEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "NotPerformed", 15, 15000))
        * match notPerformedEvent.ReasonType == <reason_type_value>
        * match notPerformedEvent.Reason == <answer_value>
        * match notPerformedEvent.ProductCode == 'SPIROMETRY'
        * match notPerformedEvent.EvaluationId == evaluation.evaluationId

        # Validate that the BillRequestNotSent message is published to Kafka
        # Need extra time to remedy kafka message taking an extraordinary long time to be generated in Docker for ADO pipeline runs
        * eval sleep(10000)
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "BillRequestNotSent", 15, 60000))
        * match event.PdfDeliveryDate contains dateStamp
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreateDate contains dateStamp
        * match event.ReceivedDate contains dateStamp
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("spirometry_status", evaluation.evaluationId + '', 10, 40000)
        * match headers contains 'NotPerformed' && 'BillRequestNotSent'

        # Validate Exam Status Update in database
        * def examStatus = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatus[*].Name contains 'Spirometry Exam Not Performed' && 'Client PDF Delivered' && 'Bill Request Not Sent'

        Examples:
        |reason_type_id|reason_type_value  | answer_id | answer_value                        | expected_reason             | expected_not_performed_reason_id |
        |50922         |'Unable to perform'| 50928     | 'Technical issue'                   | 'Technical issue'           | 5                                |
        |50921         |'Member refused'   | 50923     | 'Member recently completed'         | 'Member recently completed' | 1                                |


    @parallel=false
    @TestCaseKey=ANC-T843
    Scenario Outline: Spirometry Performed with Normal/Abnormal Normality- BillRequestSent Status-Evaluation Finalized Before PDFDelivery Sent - Validate BillRequestAccepted
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
                    "AnswerValue": <fvc>
                },
                {
                    "AnswerId": 51000,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1>
                },
                {
                    "AnswerId": 51002,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <fev1_fvc>
                },
                {
                    "AnswerId": <symptom_support_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <symptom_support_answer_value>
                },        
                {
                    "AnswerId": <risk_factors_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <risk_factors_answer_value>
                },
                {
                    "AnswerId": <comorbidity_answer_id>,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": <comorbidity_answer_value>
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
        
        # Publish the PDF event to the pdfdelivery topic
        * string pdfEventKey = evaluation.evaluationId
        * string batchName = `Ancillary_Services_Karate_Tests_${DataGen().formattedDateStamp('yyyMMdd')}`
        * def batchId = 2
        * def eventId = DataGen().uuid()
        
        * string pdfDeliveredToClientHeader = {'Type': 'PdfDeliveredToClient'}
        * string pdfEventValue = {'EventId': '#(eventId)','EvaluationId': '#(evaluation.evaluationId)','CreatedDateTime': '#(timestamp)','DeliveryDateTime': '#(timestamp)','BatchName': '#(batchName)','ProductCodes':['SPIROMETRY'],'BatchId': #(batchId)}
        * kafkaProducerHelper.send("pdfdelivery", pdfEventKey, pdfDeliveredToClientHeader, pdfEventValue)
        
        * def evalResult = SpiroDb().getResultsByEvaluationId(evaluation.evaluationId)[0]

        # Validate the entry in the BillRequestSent table
        * def billingResult = SpiroDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billingResult.BillId != null
        * match billingResult.SpirometryExamId == evalResult.SpirometryExamId
        
        # Validate BillRequestSent Status in Kafka
        * json event = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("spirometry_status", evaluation.evaluationId + '', "BillRequestSent", 10, 5000))
        * match event.BillingProductCode == 'SPIROMETRY' 
        * match event.BillId == billingResult.BillId.toString()
        * match event.PdfDeliveryDate == '#notnull' 
        * match event.EvaluationId == evaluation.evaluationId
        * match event.MemberPlanId == appointment.memberPlanId
        * match event.ProviderId.toString() == appointment.providerId
        * match event.CreateDate == '#notnull' 
        * match event.ReceivedDate == '#notnull' 
        
        * string headers = KafkaConsumerHelper.getHeadersByTopicAndKey("spirometry_status", evaluation.evaluationId + '', 10, 5000)
        * match headers contains 'Performed' && 'BillRequestSent'

        # Validate Exam Status Update in database
        * def examStatus = SpiroDb().getExamStatusByEvaluationId(evaluation.evaluationId)
        * match examStatus[*].Name contains 'Spirometry Exam Performed' && 'Client PDF Delivered' && 'Bill Request Sent'

        
        # Publish the RCM bill accepted event to rcm_bill 
        * string rcmBillId = billingResult.BillId
        * string productCode = "SPIROMETRY"
        * string rcmBillTopic = "rcm_bill"
        * string billAcceptedHeader = {'Type': 'BillRequestAccepted'}
        * string billAcceptedValue = {'RCMBillId': '#(rcmBillId)','RCMProductCode': '#(productCode)'}
        * kafkaProducerHelper.send(rcmBillTopic, "bill-" + rcmBillId, billAcceptedHeader, billAcceptedValue)
        
        # Validate that the billing details were updated as expected 
        * def billAcceptedResult = SpiroDb().getBillingResultByEvaluationId(evaluation.evaluationId)[0]
        * match billAcceptedResult.Accepted != null
        * match billAcceptedResult.AcceptedAt != null

        Examples:
        | session_grade_id | session_grade_value | fvc | fev1 | fev1_fvc |symptom_support_answer_id| symptom_support_answer_value | risk_factors_answer_id | risk_factors_answer_value | comorbidity_answer_id |comorbidity_answer_value | normality  |
        | 50938            | "B"                 | 80  | 80   | 0.65     | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Abnormal" |
        | 50938            | "B"                 | 70  | 100  | 0.7      | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Normal" |
        | 51947            | "A"                 | 70  | 100  | 0.7      | 50944                   | "Yes"                        | 50948                  | "No"                      | 50952                 | "Unknown"               | "Normal"   |        