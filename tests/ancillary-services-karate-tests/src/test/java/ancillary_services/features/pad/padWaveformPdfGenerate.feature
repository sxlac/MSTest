@pad_performance
@envnot=prod
Feature: PAD Waveform PDF's generator(currently through loop) with finalized evaluations

    Background:
        * eval if (env == 'prod') karate.abort();

    Scenario: Create some amount of pdf waveform with finalized EvaluationId
        * def PdfGenerator = 
        """ 
            function(count) {
            // Created out array just for debug
            var out = [];
            for (var i = 0; i < count; i++) {
                // Assigning DataGen and PadFileshare inside the loop.
                DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
                PadFileshare = function() { var PadFileshare = Java.type('helpers.fileshare.PadFileshare'); return new PadFileshare(); }
                memberDetails = karate.call("classpath:helpers/member/createMember.js")

                // As inside loop to pass necessary vars we need to define them in extra lines 
                memberDetails.memberPlanId = memberDetails.memberPlanId
                appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'PAD'], memberDetails: memberDetails}).response
                appointment.appointmentId = appointment.appointmentId
                evaluation = karate.call('classpath:helpers/eval/startEval.feature', { appointment: appointment, memberDetails: memberDetails}).response
                evaluation.answers =

                [
                    {
                        "AnswerId": 29560,
                        "AnsweredDateTime":  DataGen().isoTimestamp(),
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 29564,
                        "AnsweredDateTime": DataGen().isoTimestamp(),
                        "AnswerValue": "1"
                    },
                    {
                        "AnswerId": 30973,
                        "AnsweredDateTime": DataGen().isoTimestamp(),
                        "AnswerValue": "1.4"
                    },
                    {
                        "AnswerId": 22034,
                        "AnsweredDateTime": DataGen().isoDateStamp(),
                        "AnswerValue": DataGen().isoDateStamp()
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

                evaluation = evaluation
                karate.call("classpath:helpers/eval/saveEval.feature", { evaluation: evaluation, memberDetails: memberDetails})
                karate.call("classpath:helpers/eval/stopEval.feature", { evaluation: evaluation})
                karate.call("classpath:helpers/eval/finalizeEval.feature", { evaluation: evaluation})
                fileName = `${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp('MMddyy')}.pdf`

                PadFileshare().addPdfToIncomingDirectory(fileName)
                PadFileshare().verifyDocumentMovesToDirectory(fileName, "Incoming", 120, 500) == true

                out.push(`${memberDetails.lastName}_${memberDetails.memberPlanId}_PAD_BL_${DataGen().formattedDateStamp('MMddyy')}.pdf`);
            }
            return out;
            }
        """
        # here var is 1, for test launching 1000
        * def PdfCheck = call PdfGenerator 1
        Then print 'Pdf file name is - ', fileName
