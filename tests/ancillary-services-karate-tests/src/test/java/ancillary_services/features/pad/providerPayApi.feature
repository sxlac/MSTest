@pad
@envnot=prod
Feature: ProviderPay API Tests

    Background:
        * eval if (env == 'prod') karate.abort();
        * def KafkaConsumerHelper = Java.type('helpers.kafka.KafkaConsumerHelper')
        * def kafkaProducerHelper = Java.type('helpers.kafka.KafkaProducerHelper')
        * def DataGen = function() { var DataGen = Java.type("helpers.data.DataGen"); return new DataGen(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }

    @TestCaseKey=ANC-T700
    Scenario: New ProviderPayAPI request without "engagement-type"
        * def personId = `X${Faker().randomDigit(7)}` 
        * def providerId = Faker().randomDigit(5) 
        * def dateOfService = DataGen().isoDateStamp()
        * def clientId = Faker().randomDigit(2)
        * def body =
        """
            {
                "providerId": #(providerId),
                "providerProductCode": "PAD",
                "personId": #(personId),
                "dateOfService": #(dateOfService),
                "clientId": #(clientId),
                "additionalDetails": {
                    "additionalProp1": "string",
                    "additionalProp2": "string",
                    "additionalProp3": "string"
                }
            }
        """

        Given url providerPayApi
        And path 'payments'
        And request body     
        When method POST
        
        Then status 202
        Then match response.paymentId == '#uuid'

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("providerpay_internal", response.paymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == response.paymentId
        * match paymentRequested["event.providerId"] == Number(providerId)
        * match paymentRequested["event.productCode"] == 'PAD'
        * match paymentRequested["event.dateOfService"] == dateOfService
        * match paymentRequested["event.personId"] == personId
        * match paymentRequested["event.commonClientId"] == Number(clientId)
        * match paymentRequested["event.engagementTypeOptions"] == 1

    @TestCaseKey=ANC-T701
    Scenario: New ProviderPayAPI request with "engagement-type": "in-home-eval"
        * def personId = `X${Faker().randomDigit(7)}` 
        * def providerId = Faker().randomDigit(5) 
        * def dateOfService = DataGen().isoDateStamp()
        * def clientId = Faker().randomDigit(2)
        * def body =
        """
            {
                "providerId": #(providerId),
                "providerProductCode": "PAD",
                "personId": #(personId),
                "dateOfService": #(dateOfService),
                "clientId": #(clientId),
                "additionalDetails": {
                    "engagement-type": "in-home-eval",
                    "additionalProp2": "string",
                    "additionalProp3": "string"
                }
            }
        """

        Given url providerPayApi
        And path 'payments'
        And request body     
        When method POST
        
        Then status 202
        Then match response.paymentId == '#uuid'

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("providerpay_internal", response.paymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == response.paymentId
        * match paymentRequested["event.providerId"] == Number(providerId)
        * match paymentRequested["event.productCode"] == 'PAD'
        * match paymentRequested["event.dateOfService"] == dateOfService
        * match paymentRequested["event.personId"] == personId
        * match paymentRequested["event.commonClientId"] == Number(clientId)
        * match paymentRequested["event.engagementTypeOptions"] == 1

    @TestCaseKey=ANC-T702
    Scenario: New ProviderPayAPI request without "additionalDetails"
        * def personId = `X${Faker().randomDigit(7)}` 
        * def providerId = Faker().randomDigit(5) 
        * def dateOfService = DataGen().isoDateStamp()
        * def clientId = Faker().randomDigit(2)
        * def body =
        """
            {
                "providerId": #(providerId),
                "providerProductCode": "PAD",
                "personId": #(personId),
                "dateOfService": #(dateOfService),
                "clientId": #(clientId)
            }
        """

        Given url providerPayApi
        And path 'payments'
        And request body     
        When method POST
        
        Then status 202
        Then match response.paymentId == '#uuid'

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("providerpay_internal", response.paymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == response.paymentId
        * match paymentRequested["event.providerId"] == Number(providerId)
        * match paymentRequested["event.productCode"] == 'PAD'
        * match paymentRequested["event.dateOfService"] == dateOfService
        * match paymentRequested["event.personId"] == personId
        * match paymentRequested["event.commonClientId"] == Number(clientId)
        * match paymentRequested["event.engagementTypeOptions"] == 1
   
    @TestCaseKey=ANC-T703
    Scenario: New ProviderPayAPI request with "engagement-type": "stand-alone"
        * def personId = `X${Faker().randomDigit(7)}` 
        * def providerId = Faker().randomDigit(5) 
        * def dateOfService = DataGen().isoDateStamp()
        * def clientId = Faker().randomDigit(2)
        * def body =
        """
            {
                "providerId": #(providerId),
                "providerProductCode": "PAD",
                "personId": #(personId),
                "dateOfService": #(dateOfService),
                "clientId": #(clientId),
                "additionalDetails": {
                    "engagement-type": "stand-alone",
                    "additionalProp2": "string",
                    "additionalProp3": "string"
                }
            }
        """

        Given url providerPayApi
        And path 'payments'
        And request body     
        When method POST
        
        Then status 202
        Then match response.paymentId == '#uuid'

        # Validate that the Kafka event has the expected payment id and product code
        * json paymentRequested = JSON.parse(KafkaConsumerHelper.getMessageByTopicAndKeyAndHeader("providerpay_internal", response.paymentId, "PaymentRequested", 10, 5000)) 
        * match paymentRequested["event.entityId"] == response.paymentId
        * match paymentRequested["event.providerId"] == Number(providerId)
        * match paymentRequested["event.productCode"] == 'PAD'
        * match paymentRequested["event.dateOfService"] == dateOfService
        * match paymentRequested["event.personId"] == personId
        * match paymentRequested["event.commonClientId"] == Number(clientId)
        * match paymentRequested["event.engagementTypeOptions"] == 2
   
    @TestCaseKey=ANC-T704
    Scenario: New ProviderPayAPI request with invalid "engagement-type"
        * def personId = `X${Faker().randomDigit(7)}` 
        * def providerId = Faker().randomDigit(5) 
        * def dateOfService = DataGen().isoDateStamp()
        * def clientId = Faker().randomDigit(2)
        * def body =
        """
            {
                "providerId": #(providerId),
                "providerProductCode": "PAD",
                "personId": #(personId),
                "dateOfService": #(dateOfService),
                "clientId": #(clientId),
                "additionalDetails": {
                    "engagement-type": "Invalid",
                    "additionalProp2": "string",
                    "additionalProp3": "string"
                }
            }
        """

        Given url providerPayApi
        And path 'payments'
        And request body     
        When method POST
        
        Then status 400

    @TestCaseKey=ANC-T705
    Scenario: Duplicate ProviderPayAPI requests
        * def personId = `X${Faker().randomDigit(7)}` 
        * def providerId = Faker().randomDigit(5) 
        * def dateOfService = DataGen().isoDateStamp()
        * def body =
        """
            {
                "providerId": #(providerId),
                "providerProductCode": "PAD",
                "personId": #(personId),
                "dateOfService": #(dateOfService),
                "clientId": #(Faker().randomDigit(2)),
                "additionalDetails": {
                    "additionalProp1": "string",
                    "additionalProp2": "string",
                    "additionalProp3": "string"
                }
            }
        """
        * def bodyDuplicate =
        """
            {
                "providerId": #(providerId),
                "providerProductCode": "PAD",
                "personId": #(personId),
                "dateOfService": #(dateOfService),
                "clientId": #(Faker().randomDigit(2)),
                "additionalDetails": {
                    "additionalProp1": "string",
                    "additionalProp2": "string",
                    "additionalProp3": "string"
                }
            }
        """

        Given url providerPayApi
        And path 'payments'
        And request body     
        When method POST
        
        Then status 202
        And def paymentIdResponse = response.paymentId

        Given url providerPayApi
        And path 'payments'
        And configure followRedirects = false
        And request bodyDuplicate    
        When method POST
        
        Then status 303
        Then match header location == '#notnull'
        Then match header location contains '/providerpay/v1/operations/payments/'
        And def locationHeader = responseHeaders['location']
        And eval locationHeader ??= responseHeaders['Location']
        And def paymentId = locationHeader[0].split('/').pop()
        Then match paymentId == '#uuid'
        Then match paymentId == paymentIdResponse

        # Validate that a duplicate Kafka event is not raised.
        And string paymentRequested = KafkaConsumerHelper.getEventsByTopicAndKey("providerpay_internal", paymentId, 10, 5000)
        And def regex = new RegExp(paymentId, "gi");
        Then match paymentRequested.match(regex).length == 1