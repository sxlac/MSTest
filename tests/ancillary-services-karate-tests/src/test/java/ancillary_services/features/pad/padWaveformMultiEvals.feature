@pad
@envnot=prod
Feature: PAD Waveform Tests with Multiple Evaluations per MemberId

    Background:
        * eval if (env == 'prod') karate.abort();
        # Only run these tests if the PAD waveform flag is true
        * eval if (padWaveformsFlag != 'true') karate.abort();

        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def PadDb = function() { var PadDb = Java.type("helpers.database.pad.PadDb"); return new PadDb(); }
        * def PadFileshare = function() { var PadFileshare = Java.type('helpers.fileshare.PadFileshare'); return new PadFileshare(); }
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")

    @TestCaseKey=ANC-T371
    Scenario: Waveform is Processed for Correct Eval when Multiple Evals Exist
        * def firstAppointmentMinusDays = -10
        * def secondAppointmentMinusDays = -15
        * def appointments = []
        * def evaluations = []

        # First we create an appointment so we can cancel it
        * set appointments[0] = karate.call('classpath:helpers/appointment/createAppointment.feature', {timeSlot: { startDateTime: DataGen().isoTimestamp(firstAppointmentMinusDays), endDateTime: DataGen().isoTimestamp(firstAppointmentMinusDays)}}).response
        # Then cancel it and wait because if we reschedule too soon it won't know the first one is cancelled
        * karate.call('classpath:helpers/appointment/cancelAppointment.feature', {appointment: appointments[0]})
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * eval sleep(2500)
        
        # Create another appointment for and toss it in the list of appointments
        * set appointments[1] = karate.call('classpath:helpers/appointment/createAppointment.feature', {timeSlot: { startDateTime: DataGen().isoTimestamp(secondAppointmentMinusDays), endDateTime: DataGen().isoTimestamp(secondAppointmentMinusDays)}}).response

        # Create the evaluations for both appointments
        * set evaluations[0] = karate.call('classpath:helpers/eval/startEval.feature', {appointment: appointments[0]}).response
        * set evaluations[0].answers =
        """
            [
                {
                    'AnswerId': 29560,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 29564,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 30973,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))',
                    'AnswerValue': '1.4'
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))',
                    'AnswerValue': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))'
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
        * karate.call('classpath:helpers/eval/saveEval.feature', {evaluation: evaluations[0]})
        * karate.call('classpath:helpers/eval/stopEval.feature', {evaluation: evaluations[0]})
        * karate.call('classpath:helpers/eval/finalizeEval.feature', {evaluation: evaluations[0]})

        # Create an evaluation for the second appointment, but backdate this one so the date of service is different
        * set evaluations[1] = karate.call('classpath:helpers/eval/startEval.feature', {appointment: appointments[1]}).response
        * set evaluations[1].answers =
        """
            [
                {
                    'AnswerId': 29560,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 29564,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 30973,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))',
                    'AnswerValue': '1.4'
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))',
                    'AnswerValue': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))'
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
        * karate.call('classpath:helpers/eval/saveEval.feature', {evaluation: evaluations[1]})
        * karate.call('classpath:helpers/eval/stopEval.feature', {evaluation: evaluations[1]})
        * karate.call('classpath:helpers/eval/finalizeEval.feature', {evaluation: evaluations[1]})

        # This is just here so that we can wait for the delays in finalizing evals.....
        * def padDbResult = PadDb().getResultsByEvaluationId(evaluations[1].evaluationId)
        * match padDbResult == "#notnull"

        # The file will be created using the DOS from the first appointment
        * def fileName = `${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp(firstAppointmentMinusDays, 'MMddyy')}.pdf`
        * PadFileshare().addPdfToIncomingDirectory(fileName)
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, 'PENDING', 120, 500) == true
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, 'PROCESSED', 180, 500) == true

        # Once the PDF is processed, verify the details in the PAD database belong to the first eval
        * def result = PadDb().getWaveformDocsByMemberPlanId(memberDetails.memberPlanId)[0]
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.Filename == fileName
        * match result.DateOfExam.toString() == DataGen().formattedDateStamp(firstAppointmentMinusDays, 'yyyy-MM-dd')
        
        # And the evaluation API should have the PDF for the first evaluation
        Given url evaluationApi
        And path `evaluationdocument/${evaluations[0].evaluationId}`
        When method GET
        Then status 200
        And match response[0].documentType == 'PadWaveform'

        # And the evaluation API but it shouldn't have anything for the second
        Given url evaluationApi
        And path `evaluationdocument/${evaluations[1].evaluationId}`
        When method GET
        Then status 204

        # Then drop another file for the second evaluation and test it
        * def fileName = `${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp(secondAppointmentMinusDays, 'MMddyy')}.pdf`
        * PadFileshare().addPdfToIncomingDirectory(fileName)
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, 'PENDING', 120, 500) == true
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, 'PROCESSED', 120, 500) == true

        # Once the PDF is processed, verify the details in the PAD database belong to the second eval
        * def result = PadDb().getWaveformDocsByMemberPlanId(memberDetails.memberPlanId)[1]
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.Filename == fileName
        * match result.DateOfExam.toString() == DataGen().formattedDateStamp(secondAppointmentMinusDays, 'yyyy-MM-dd')

        # And the evaluation API should also have results for the second eval now
        Given url evaluationApi
        And path `evaluationdocument/${evaluations[1].evaluationId}`
        When method GET
        Then status 200
        And match response[0].documentType == 'PadWaveform'

    @TestCaseKey=ANC-T356
    Scenario: Waveform is Processed for Correct Eval when Multiple Evals Exist and Eval is Finalized after File Creation
        * def firstAppointmentMinusDays = -10
        * def secondAppointmentMinusDays = -15
        * def appointments = []
        * def evaluations = []

        # First we create an appointment so we can cancel it
        * set appointments[0] = karate.call('classpath:helpers/appointment/createAppointment.feature', {timeSlot: { startDateTime: DataGen().isoTimestamp(firstAppointmentMinusDays), endDateTime: DataGen().isoTimestamp(firstAppointmentMinusDays)}}).response
        # Then cancel it and wait because if we reschedule too soon it won't know the first one is cancelled
        * karate.call('classpath:helpers/appointment/cancelAppointment.feature', {appointment: appointments[0]})
        * def sleep = function(pause){ java.lang.Thread.sleep(pause) }
        * eval sleep(2500)
        
        # Create another appointment for and toss it in the list of appointments
        * set appointments[1] = karate.call('classpath:helpers/appointment/createAppointment.feature', {timeSlot: { startDateTime: DataGen().isoTimestamp(secondAppointmentMinusDays), endDateTime: DataGen().isoTimestamp(secondAppointmentMinusDays)}}).response

        # Create the evaluations for both appointments
        * set evaluations[0] = karate.call('classpath:helpers/eval/startEval.feature', {appointment: appointments[0]}).response
        * set evaluations[0].answers =
        """
            [
                {
                    'AnswerId': 29560,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 29564,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 30973,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))',
                    'AnswerValue': '1.4'
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))',
                    'AnswerValue': '#(DataGen().isoTimestamp(firstAppointmentMinusDays))'
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
        * karate.call('classpath:helpers/eval/saveEval.feature', {evaluation: evaluations[0]})
        * karate.call('classpath:helpers/eval/stopEval.feature', {evaluation: evaluations[0]})

        * set evaluations[1] = karate.call('classpath:helpers/eval/startEval.feature', {appointment: appointments[1]}).response
        * set evaluations[1].answers =
        """
            [
                {
                    'AnswerId': 29560,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 29564,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))',
                    'AnswerValue': '1'
                },
                {
                    'AnswerId': 30973,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))',
                    'AnswerValue': '1.4'
                },
                {
                    'AnswerId': 22034,
                    'AnsweredDateTime': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))',
                    'AnswerValue': '#(DataGen().isoTimestamp(secondAppointmentMinusDays))'
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
        * karate.call('classpath:helpers/eval/saveEval.feature', {evaluation: evaluations[1]})
        * karate.call('classpath:helpers/eval/stopEval.feature', {evaluation: evaluations[1]})

        # Drop a file for the second appointment only before finalizing the evaluations
        # Then we should see the file hit the pending directory but no further
        * def fileName = `${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp(secondAppointmentMinusDays, 'MMddyy')}.pdf`
        * PadFileshare().addPdfToIncomingDirectory(fileName)
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, 'PENDING', 120, 500) == true
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, 'PROCESSED', 24, 2500) == false

        # Finalize the evals
        * karate.call('classpath:helpers/eval/finalizeEval.feature', {evaluation: evaluations[0]})
        * karate.call('classpath:helpers/eval/finalizeEval.feature', {evaluation: evaluations[1]})

        # This is just here so that we can wait for the delays in finalizing evals.....
        * def padDbResult = PadDb().getResultsByEvaluationId(evaluations[1].evaluationId)
        * match padDbResult == "#notnull"

        # Once the PDF is processed, verify the details in the PAD database belong to the second eval
        * match PadFileshare().verifyDocumentMovesToDirectory(fileName, 'PROCESSED', 180, 500) == true
        * def result = PadDb().getWaveformDocsByMemberPlanId(memberDetails.memberPlanId)[0]
        * match result.MemberPlanId == memberDetails.memberPlanId
        * match result.Filename == fileName
        * match result.DateOfExam.toString() == DataGen().formattedDateStamp(secondAppointmentMinusDays, 'yyyy-MM-dd')

        # But it should have a PDF for the second evaluation
        Given url evaluationApi
        And path `evaluationdocument/${evaluations[1].evaluationId}`
        When method GET
        Then status 200
        And match response[0].documentType == 'PadWaveform'