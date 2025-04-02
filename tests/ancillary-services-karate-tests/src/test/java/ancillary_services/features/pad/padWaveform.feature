@pad
@envnot=prod
Feature: PAD Waveform Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        # Only run these tests if the PAD waveform flag is true
        * eval if (padWaveformsFlag != 'true') karate.abort();

        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def PadDb = function() { var PadDb = Java.type("helpers.database.pad.PadDb"); return new PadDb(); }
        * def PadFileshare = function() { var PadFileshare = Java.type('helpers.fileshare.PadFileshare'); return new PadFileshare(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()

        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'] }).response
        * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

    @TestCaseKey=ANC-T362
    Scenario: Waveform Document is Processed when Evaluation is Performed
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
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1.4"
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

        # This is just here so that we can wait for the delays in finalizing evals.....
        * def padDbResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)
        * match padDbResult == "#notnull"

        # Upload the PDF to the fileshare then make sure it follows the correct path through
        * def fileName = `${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp('MMddyy')}.PDF`
        * PadFileshare().addPdfToIncomingDirectory(fileName)
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PENDING", 120, 500) == true
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PROCESSED", 130, 500) == true

        # Once the PDF is processed, verify the details in the PAD database
        * def waveformDocumentResult = PadDb().getWaveformDocsByMemberPlanId(memberDetails.memberPlanId)[0]
        * match waveformDocumentResult.MemberPlanId == memberDetails.memberPlanId
        * match waveformDocumentResult.Filename == fileName

        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'WaveformDocumentDownloaded'
        * match statusResults[*].StatusCode contains 'WaveformDocumentUploaded'
        
        # And the evaluation API should have the PDF as well...
        Given url evaluationApi
        And path `evaluationdocument/${evaluation.evaluationId}`
        When method GET
        Then status 200
        And match response[0].documentType == "PadWaveform"

    @TestCaseKey=ANC-T366
    Scenario: Waveform Document is Processed when Evaluation is Not Performed
        * set evaluation.answers =
            """
            [
                {
                    "AnswerId": 29561,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "2"
                },
                {
                    "AnswerId": 30957,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Member refused"
                },
                {
                    "AnswerId": 30959,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "Member recently completed"
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

        # This is just here so that we can wait for the delays in finalizing evals.....
        * def padDbResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)
        * match padDbResult == "#notnull"

        # Upload the PDF to the fileshare then make sure it follows the correct path through
        * def fileName = `${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp('MMddyy')}.pdf`
        * PadFileshare().addPdfToIncomingDirectory(fileName)
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PENDING", 120, 500) == true
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PROCESSED", 120, 500) == true

        # Once the PDF is processed, verify the details in the PAD database
        * def result = PadDb().getWaveformDocsByMemberPlanId(memberDetails.memberPlanId)[0]
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.Filename == fileName
        
        # And the evaluation API should have the PDF as well...
        Given url evaluationApi
        And path `evaluationdocument/${evaluation.evaluationId}`
        When method GET
        Then status 200
        And match response[0].documentType == "PadWaveform"

    @TestCaseKey=ANC-T369
    Scenario: Waveform is Created Before Eval Finalized
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
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1.4"
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

        # Upload the PDF to the fileshare then make sure it follows the correct path through
        * def fileName = `${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp('MMddyy')}.pdf`
        * PadFileshare().addPdfToIncomingDirectory(fileName)
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PENDING", 120, 500) == true

        * karate.call('classpath:helpers/eval/finalizeEval.feature')
        # This is just here so that we can wait for the delays in finalizing evals.....
        * def padDbResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)
        * match padDbResult == "#notnull"

        # Once the eval is processed, verify the waveform is processed
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PROCESSED", 120, 500) == true

        # Then check the waveform details in the database
        * def result = PadDb().getWaveformDocsByMemberPlanId(memberDetails.memberPlanId)[0]
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.Filename == fileName
        
        # And the evaluation API should have the PDF as well...
        Given url evaluationApi
        And path `evaluationdocument/${evaluation.evaluationId}`
        When method GET
        Then status 200
        And match response[0].documentType == "PadWaveform"


    Scenario: Duplicate Waveform Document is moved to Failed folder.
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
                    "AnswerValue": "1"
                },
                {
                    "AnswerId": 30973,
                    "AnsweredDateTime": "#(timestamp)",
                    "AnswerValue": "1.4"
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

        # Upload the PDF to the fileshare then make sure it follows the correct path through
        * def fileName = `${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp('MMddyy')}.PDF`
        * PadFileshare().addPdfToIncomingDirectory(fileName)
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PENDING", 120, 200) == true
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PROCESSED", 260, 500) == true

        # This is just here so that we can wait for the delays in finalizing evals.....
        * def padDbResult = PadDb().getResultsByEvaluationId(evaluation.evaluationId)
        * match padDbResult == "#notnull"

        * def statusResults = PadDb().getPadStatusByEvaluationId(evaluation.evaluationId)
        * match statusResults[*].StatusCode contains 'WaveformDocumentDownloaded'
        * match statusResults[*].StatusCode contains 'WaveformDocumentUploaded'

        # Once the PDF is processed, verify the details in the PAD database
        * def waveformDocumentResult = PadDb().getWaveformDocsByMemberPlanId(memberDetails.memberPlanId)[0]
        * match waveformDocumentResult.MemberPlanId == memberDetails.memberPlanId
        * match waveformDocumentResult.Filename == fileName

        # Upload the PDF to the fileshare then make sure it follows the correct path through

        * PadFileshare().addPdfToIncomingDirectory(fileName)
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, "PENDING", 150, 200) == true
        * PadFileshare().checkFileMovedToFailedFolder(fileName, 130, 250)
