function loadAppConfig(config) {
    try{
        try {
            var appConfig = read('classpath:appConfig/appConfig.json')
        } catch (err) {
            `Unable to find the appConfig.json file!
            Please ensure this file exists in the appConfig folder.
            Request a copy of the file from the DPS team QE if needed.
            ${err}`
        }
        karate.log('Configuring using appConfig/appConfig.json!')
        // appConfig.json must contain an object for the current running config.env
        if (!appConfig[config.env]) {
            throw `The appConfig.json does not contain an object for the ${config.env} config.environment.
                    Ensure that this object exists before attempting to run for this config.environment!`
        }
        // We must have okta credentials, and load them into karate's config if we do
        if (!appConfig[config.env].okta) {
            throw `The appConfig.json does not contain any okta credentials!
                    Either provide some test credentials or your own in the appConfig.json in this format:
                    "${config.env}": { "okta": { "username": "testUser", "password": "Test123" } }`
        } else {
            config.okta.credentials = appConfig[config.env].okta
            config.okta.clientId = appConfig[config.env].okta.clientId

        }

        if (!appConfig[config.env].databases) {
            throw `The appConfig.json does not have databases loaded for ${config.env}.
                    An array object of databases containing the name, url, username, and password must be included.`
        }
        // DB details get loaded as Java properties so they can be read directly in Java classes		
        for (const db of appConfig[config.env].databases) {
            if (!db.url && !db.username && !db.password) {
                throw `${db} is missing either "url", "username", or "password" in appConfig.json.
                    Ensure that these keys exist in the ${db} object before attempting to run!`
            }
            Java.type('java.lang.System').setProperty(`${db.name.toUpperCase()}_DB_URL`, db.url)
            Java.type('java.lang.System').setProperty(`${db.name.toUpperCase()}_DB_USERNAME`, db.username)
            Java.type('java.lang.System').setProperty(`${db.name.toUpperCase()}_DB_PASSWORD`, db.password)
        }

        if (!appConfig.zephyr) {
            throw `The appConfig.json does not contain an object for zephyr.
                    Ensure the json object exist in the appConfig.json file before attempting to run!`
        } else {     
            Java.type('java.lang.System').setProperty(`ZEPHYR_API_TOKEN`, appConfig.zephyr.token)
            Java.type('java.lang.System').setProperty(`ZEPHYR_API_URL`, appConfig.zephyr.apiUrl)
            Java.type('java.lang.System').setProperty(`CREATE_TEST_CYCLE`, appConfig.zephyr.createTestCycle)
        }
    }
    catch (err) {
        // We might be running in a Docker container instead of locally
        if (java.lang.System.getenv('OKTA_USERNAME') != null) {
            karate.log('Configuring using Docker!')
            // Load variables from Docker instead of the appConfig file
            config.okta.credentials.username = java.lang.System.getenv('OKTA_USERNAME')
            config.okta.credentials.password = java.lang.System.getenv('OKTA_PASSWORD')
            config.okta.clientId = java.lang.System.getenv('OKTA_CLIENT_ID') 
        } else {
            throw err
        }
    }
    
return config
}