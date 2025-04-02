@ignore
Feature: Create Test Members
# Calling this feature requires a censeoId and a externalId to be defined and passed in
# Products are defined in karate-config.js

    Background:
        * def DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
        * def Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }
        * configure retry = { count: 5, interval: 1000 }
        
    Scenario: Create Test Member
        * def memberDetails =
            """
            {
                "planName": "#(planName)",
                "censeoId": "#(censeoId)",
                "gender": "M",
                "firstName": "#(Faker().firstName())",
                "middleName": "KarateTesting",
                "lastName": "#(Faker().lastName())",
                "dateOfBirth": "1980-06-24",
                "emailAddress": "ahr@signifyhealth.com",
                "hicNumber": null,
                "preferredProviderGender": null,
                "preferredProviderLanguage": null,
                "address": {
                    "address1": "4420 Harpers Ferry Dr",
                    "address2": "Mysore",
                    "city": "Grand Prairie",
                    "state": "TX",
                    "zipCode": "75052",
                    "county": "Dallas",
                    "location": {
                        "latitude": 32.6650764,
                        "longitude": -97.0156823
                    }
                },
                "mbi": null,
                "subscriberId": "014245",
                "notes": null,
                "nextCallTime": "2020-04-16T16:51:42.1548254-05:00",
                "pcp_FirstName": "Shaun",
                "pcp_LastName": "Scott",
                "pcp_Address": null,
                "npi": null,
                "phoneNumbers": [
                    {
                        "phoneNumberType": "Mobile",
                        "phoneNumber": "4699995550"
                    }
                ],
                "memberProducts": #(products),
                "memberPlanStatus": "Active",
                "memberUpdateSource": "Gilenya",
                "phoneNumberUpdateSource": "Gilenya",
                "doNotCall": false,
                "censeoDoNotCall": false,
                "isDeafOrHardOfHearing": false,
                "requiresSignLanguage": false,
                "userName": "vastest1",
                "applicationId": "Postman",
                "externalId": "#(externalId)",
                "householdIdentifier": null,
                "isValidAddress": true,
                "hPlan": "14254",
                "formId": 1,
                "memberId": null
            }
            """
        Given url memberApi + 'member/'
        And request memberDetails
        And retry until responseStatus == 200   
        When method Post
        Then status 200
        
        * set memberDetails.memberId = response.memberId
        * set memberDetails.memberPlanId = response.memberPlanId