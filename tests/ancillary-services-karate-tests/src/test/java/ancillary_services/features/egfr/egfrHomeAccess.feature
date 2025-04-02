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

    @TestCaseKey=ANC-T545        
    Scenario Outline: eGFR Lab Results E2E 
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

        * match result.CenseoId == memberDetails.censeoId
        * match result.DateOfService != null
        
        # Create valid eGFR Lab Results file with CenseoId and DateOfService from above finalized evaluation
        * def eGFRLabResultsFileShare = function() { var HomeAccessFileshare = Java.type('helpers.endToEndHelpers.eGFRLabResultsHelpers'); return new HomeAccessFileshare(); }
        * def fileName = `Test_eGFR_LabResults_${DataGen().randomInteger(00000, 99999)}.xlsx`
        * def SVC_AccessionedDate = "08/08/2022" 
        * def ProperDateOfService = DataGen().getUtcDateTimeString(result.DateOfService.toString()).split('T')[0]
        * def SVC_CollectionDate = eGFRLabResultsFileShare().changeFormatForExcelFile(ProperDateOfService)
        * def MBR_SubscriberID = result.CenseoId

        # Drop file to Pending folder
        * eGFRLabResultsFileShare().createAndDropEGFRLabResultsToPendingFolder(fileName, SVC_AccessionedDate, SVC_CollectionDate, MBR_SubscriberID, "Kit Mailings", CMP_eGFRResult)
        # Check if file successfully moved to Complete folder
        * eGFRLabResultsFileShare().checkEGFRMovedToCompleteFolder(fileName, 200, 300)

        # Get kafka event by CenseoId and egfr_lab_results topic and ensure that dates are as intended
        And json kafkaEvent = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("egfr_lab_results", result.CenseoId, "EgfrLabResult", 10, 5000))
        * match kafkaEvent["CenseoId"] == result.CenseoId
        * match kafkaEvent.MailDate.toString().split('T')[1] == "00:00:00+00:00"
        * match kafkaEvent.AccessionedDate.toString().split('T')[1] == "00:00:00+00:00"
        * match kafkaEvent.CollectionDate.toString().split('T')[1] == "00:00:00+00:00"

        # Verify in Lab Results that record is created with correct CenseoId and CollectionDate
        * def QuestlabResults = EgfrDb().checkLabResultsRecordPresentByCenseoId(result.CenseoId)[0]
        * match QuestlabResults.CenseoId == memberDetails.censeoId
        * match QuestlabResults.CollectionDate == result.DateOfService
        * match QuestlabResults.CreatinineResult == kafkaEvent.CreatinineResult
        * match QuestlabResults.eGFRResult == kafkaEvent.EgfrResult
        * match QuestlabResults.VendorLabTestId == kafkaEvent.VendorLabTestId
        * match QuestlabResults.VendorLabTestNumber == kafkaEvent.VendorLabTestNumber
        * assert DataGen().getUtcDateTimeString(QuestlabResults.AccessionedDate.toString()).split('T')[0] == kafkaEvent.AccessionedDate.toString().split('T')[0]
        * assert DataGen().getUtcDateTimeString(QuestlabResults.MailDate.toString()).split('T')[0] == kafkaEvent.MailDate.toString().split('T')[0]
        * assert DataGen().getUtcDateTimeString(QuestlabResults.CollectionDate.toString()).split('T')[0] == kafkaEvent.CollectionDate.toString().split('T')[0]

        * def labResultsNotPresent = EgfrDb().getLabResultsRecordByEvaluationId(evaluation.evaluationId)[0]
        * match labResultsNotPresent == null

        # Additional check for normal/abnormal result. If CMP_eGFRResult 60 >, Normal indicator, 60 < Abnormal indicator expected 
        * match QuestlabResults.NormalityCode == expected_normality_indicator
        * match QuestlabResults.Normality == expected_normality

        # Check that status code is Performed and Lab Result received
        * def result = EgfrDb().queryExamWithStatusList(evaluation.evaluationId,["Exam Performed","Lab Results Received"])
        * match result[*].ExamStatusCodeId contains [1, 6]
  
        Examples:
            | CMP_eGFRResult | expected_normality_indicator | expected_normality |
            | 65             | N                            | Normal             |
            | 45             | A                            | Abnormal           |
            | 0              | U                            | Undetermined       |
            |                | U                            | Undetermined       |
            | -1             | U                            | Undetermined       |