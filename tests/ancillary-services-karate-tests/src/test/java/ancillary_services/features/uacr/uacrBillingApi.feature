@uacr
@ignore
Feature: uACR Billing Api Tests

    Background:
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def UacrDb = function() { var UacrDb = Java.type('helpers.database.uacr.UacrDb'); return new UacrDb(); }
        * def timestamp = DataGen().isoTimestamp()
        * def dateStamp = DataGen().isoDateStamp()
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def KafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def memberDetails = karate.call("classpath:helpers/member/createMember.js")
        #* def appointment = karate.call('classpath:helpers/appointment/createAppointment.feature', { products: ['HHRA', 'UACR'] }).response
        #* def evaluation = karate.call('classpath:helpers/eval/startEval.feature').response

    Scenario: Billing API test
        Given url rcmApi
        And path 'Bills'
        And request 
        """
            {
                "sharedClientId": 14,
                "memberPlanId": #(memberDetails.memberPlanId),
                "dateOfService": "2024-02-01T14:18:01.514Z",
                "usStateOfService": #(memberDetails.address.state),
                "providerId": #(providerDetails.providerId),
                "rcmProductCode": "uACR",
                "applicationId": "Signify.uACR.Svc",
                "correlationId": "#(DataGen().uuid())",
                "additionalDetails": {
                    "additionalProp1": "string",
                    "additionalProp2": "string",
                    "additionalProp3": "string"
                },
                "billableDate": "2024-02-01T14:18:01.514Z",
                "username": "string",
                "rowNumber": 0,
                "notes": "string",
                "bulkFileUploadId": 0
            }
        """
        When method POST
        Then status 202
    