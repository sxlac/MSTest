@ignore
Feature: Create and Return Auth Token

  Background: Background name
    * configure ssl = true
    * configure followRedirects = false

  Scenario: Establish Session and Get Token
    Given url okta.authUrl
    And path 'api/v1/authn'
    And request okta.credentials
    When method POST
    Then status 200

    Given path 'oauth2/default/v1/authorize'
    And param sessionToken = response.sessionToken
    And param client_id = okta.clientId
    And param scope = okta.scope
    And param response_type = "token id_token"
    And param response_mode = "form_post"
    And param state = okta.state
    And param nonce = okta.nonce
    And param redirect_uri = okta.redirectUri
    When method GET
    * def token = karate.extract(response, 'access_token.+value=\\"([^\\"]+)', 1)