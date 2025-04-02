@ignore
Feature: Loads the test configurations for karate tests

 Scenario: Load local config
    
    * def config = config
    * string env = config.env
    * karate.log('Using environment \"'+env+'\"')
    * def setOktaProps = 
    """
        function(appConfig,env){
            karate.log('Setting OKTA credentials with appconfig.json file for \"'+env+'\" environment')
            
            // appConfig.json must contain an object for the current running config.env
            if (!appConfig[config.env]) {
                throw `The appConfig.json does not contain an object for the ${config.env} config.environment.
                        Ensure that this object exists before attempting to run for this config.environment!`
            }

            // We must have okta credentials, and load them into karate's config if we do
            if (!appConfig[config.env].okta) {
                throw `The appConfig.json does not contain any okta credentials!
                        Either provide some test credentials or your own in the appConfig.json in this format:
                        "${config.env}": { "okta": { "username": "***", "password": "***" } }`
            } else {
                config.okta.credentials = appConfig[config.env].okta
                //config.okta.clientId = appConfig[env].okta.clientId
            }
                
        }
    """

    * def setDBProps = 
    """
        function(appConfig,env){
            karate.log('Setting DB credentials with appconfig.json file for \"'+env+'\" environment')
            
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
            // SMB details get loaded as Java properties so they can be read directly from Java classes
            for (const smb of appConfig[config.env].smb) {
                if (!smb.username || !smb.password) {
                    throw `SMB index ${smb} is missing either "username" or "password" in appConfig.json".`
                }

                Java.type('java.lang.System').setProperty('SMB_USERNAME', smb.username)
                Java.type('java.lang.System').setProperty('SMB_PASSWORD', smb.password)
            }
        } 
    """

    * def setKafkaProps = 
    """
        function(appConfig,env){
            karate.log('Setting Kafka properties with appconfig.json file for \"'+env+'\" environment')
            if (!appConfig[config.env].kafka.bootstrapServers && !appConfig[config.env].kafka.username && !appConfig[config.env].kafka.password) {
            throw `The appConfig.json does not contain an object for kafka,
                    Or it is missing the username or password.
                    Ensure these keys exist in the kafka object before attempting to run!`
            } else {     
                Java.type('java.lang.System').setProperty(`KAFKA_BOOTSTRAP_SERVERS`, appConfig[config.env].kafka.bootstrapServers)
                Java.type('java.lang.System').setProperty(`KAFKA_GROUP_ID`, appConfig[config.env].kafka.groupID)
                Java.type('java.lang.System').setProperty(`KAFKA_USERNAME`, appConfig[config.env].kafka.username)
                Java.type('java.lang.System').setProperty(`KAFKA_PASSWORD`, appConfig[config.env].kafka.password)  
                Java.type('java.lang.System').setProperty(`KAFKA_PUB_USERNAME`, appConfig[config.env].kafka.automationPublisher.username)
                Java.type('java.lang.System').setProperty(`KAFKA_PUB_PASSWORD`, appConfig[config.env].kafka.automationPublisher.password)   
                Java.type('java.lang.System').setProperty(`KAFKA_HOME_ACCESS_PUB_USERNAME`, appConfig[config.env].kafka.automationPublisher.homeAccessUsername)
                Java.type('java.lang.System').setProperty(`KAFKA_HOME_ACCESS_PUB_PASSWORD`, appConfig[config.env].kafka.automationPublisher.homeAccessPassword)     
            }
        } 
    """

    * def setDockerOktaProps = 
    """
        function(){
            if (java.lang.System.getenv('OKTA_USERNAME')) {
                karate.log('Configuring using Docker!')
                config.okta.credentials.username = java.lang.System.getenv('OKTA_USERNAME')
                config.okta.credentials.password = java.lang.System.getenv('OKTA_PASSWORD')
                //config.okta.clientId = java.lang.System.getenv('OKTA_CLIENT_ID')
            } else {
                karate.log('OKTA_USERNAME not found!')
            }
        }
    """

    * def loadConfig = 
    """
        function(string){
               
            if (java.lang.System.getenv('CREATE_TEST_CYCLE')) {
                return false;
            }else {   
                if(string=='read'){
                    karate.log('Reading appconfig/appconfig.json!')
                }else {
                    karate.log('Configuring '+string+' using appconfig/appconfig.json!')
                }
                return true;
            }
        }
    """

    * def setZephyrProps = 
    """
        function(appConfig){
            Java.type('java.lang.System').setProperty("ZEPHYR_API_TOKEN", appConfig["zephyr"].token);
            Java.type('java.lang.System').setProperty("ZEPHYR_API_URL", appConfig["zephyr"].apiUrl);
            Java.type('java.lang.System').setProperty("CREATE_TEST_CYCLE", appConfig["zephyr"].createTestCycle);
        }
    """
    
    * def setNewRelicProps = 
    """
        function(appConfig){
            Java.type('java.lang.System').setProperty('NEW_RELIC_ID', appConfig[config.env].newRelic.id)
            Java.type('java.lang.System').setProperty('NEW_RELIC_KEY', appConfig[config.env].newRelic.key)
            Java.type('java.lang.System').setProperty('NEW_RELIC_URI', appConfig[config.env].newRelic.uri)
        }
    """


    * def appConfig = loadConfig('read')? karate.read('classpath:appconfig/appconfig.json'):{}
    * if (loadConfig('BD Properties')) setDBProps(appConfig,env)
    * if (loadConfig('Kafka Properties')) setKafkaProps(appConfig,env)
    * if (loadConfig('OKTA Properties')) setOktaProps(appConfig,env)
    * if (loadConfig('Zephyr Properties')) setZephyrProps(appConfig)
    * if (loadConfig('New Relic Properties')) setNewRelicProps(appConfig)
    * setDockerOktaProps()