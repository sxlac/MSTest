@smoke-egfr
Feature: EGFR Smoke Tests

Background:
    * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
    * def EgfrDb = function() { var EgfrDb = Java.type('helpers.database.egfr.EgfrDb'); return new EgfrDb(); }
    * def timestamp = DataGen().isoTimestamp()
    * def dateStamp = DataGen().isoDateStamp()
    
    * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
    * def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'EGFR'] }).response
    * def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

Scenario: eGFR Not Performed - Unable to Perform
    * set evaluation.answers =
        """
        [
            {
                "AnswerId": 22034,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "#(timestamp)"
            },
            {
                "AnswerId": 51262,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": 0
            },
            {
                "AnswerId": 51263,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "Provider unable to perform"
            },
            {
                "AnswerId": 51267,
                "AnsweredDateTime": "#(timestamp)",
                "AnswerValue": "Environmental issue"
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

    * def result = EgfrDb().getNotPerformedResultsByEvaluationId(evaluation.evaluationId)[0]
    # Verify not performed details
    * match result.AnswerId == 51267
    * match result.Reason == "Environmental issue"
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
    * match result.StatusName == "Exam Not Performed"
    * match result.StatusDateTime != null

