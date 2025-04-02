function configForKarate() {
    const env = karate.env || 'uat';
    //This is for Database Only tests like deeDatabaseStructureValidation.feature
    const databaseOnly = false;
 
    Java.type('java.lang.System').setProperty('karate.env', env); // This is here so we can get env from Java files

    /**
     * Tags are passed in through mvn using -Dkarate.options=--tags @tag
     * The regex will match the value after the @ symbol
     * And then will create a list starting with the original match, then the match group (the values between the () symbols)
     * Example: regex.exec('-Dkarate.options=--tags @pad') would return [@pad, pad]
     */
    let tag = '';
    let regex = /@([a-zA-Z]*)/i;
    let tags = regex.exec(java.lang.System.properties.getProperty('karate.options'));
    if (tags != null) { tag = `.${tags[1]}` }; // If tags were found, get the second value from the list so we don't include the @ symbol

    karate.configure('ssl', true);
    
    var LM = Java.type('helpers.oauth.AuthLogModifier');
    karate.configure('logModifier', LM.INSTANCE);

    let config = {
        env: env,
        //Add UACR once product added in UAT
        products: ["CKD", "DEE", "EGFR", "HHRA", "PAD", "SPIROMETRY", "FOBT", "HBA1CPOC","UACR"],
    };

    switch (env)
    {
        case 'local': 
        {
            if (databaseOnly) 
            {
                try {
                    config = karate.callSingle('classpath:appConfig/loadDatabaseOnlyConfig.js', config);
                } catch (err) {
                    karate.log('Unable to load loadDatabaseOnlyConfig...');
                    throw err;
                }
            }
            else 
            {
                const localApiUrl = `https://localhost:7118`;

                config = {
                    env: env,
                    //Add UACR once product added in UAT
                    products: ["CKD", "DEE", "EGFR", "HHRA", "PAD", "SPIROMETRY", "FOBT", "HBA1CPOC","UACR"],
                    formVersionId: 493,
                    providerDetails: {providerId : 42879, nationalProviderIdentifier : '9230239051'},
                    evaluationApi: `${localApiUrl}/evaluation/`,
                    memberApi: `${localApiUrl}/member/`,
                    providerApi: `${localApiUrl}/provider/`,
                    appointmentApi: `${localApiUrl}/`
                };
    
                // Feature Flags
                config.padWaveformsFlag = karate.properties['FeatureFlags:PadWaveforms'] || 'false';
    
                // Load the app configuration into memory (includes connection strings, users, etc)
                // See appConfig/README.md for more information
                try {
                    config = karate.callSingle('classpath:appConfig/loadLocalConfig.js', config);
                } catch (err) {
                    karate.log('Unable to load loadLocalConfig...');
                    throw err;
                }
                
                // Setup Kafka
                karate.log('Creating KafkaConsumerHelper');
                let localKafkaHelper = Java.type('helpers.kafka.KafkaConsumerHelper');
                localKafkaHelper.Instance();
            }
            break;
        }
        case 'prod': 
        {
            if (databaseOnly) 
            {
                try {
                    config = karate.callSingle('classpath:appConfig/loadDatabaseOnlyConfig.js', config);
                } catch (err) {
                    karate.log('Unable to load loadDatabaseOnlyConfig...');
                    throw err;
                }
            }
            else 
            {
                let coreApiUrl = 'https://coreapi.signifyhealth.com';

                config = {
                    env: env,
                    products: ["CKD", "DEE", "EGFR", "HHRA", "PAD", "SPIROMETRY", "FOBT", "HBA1CPOC"],
                    planId: 37,
                    planName: 'CHDemo-TX',
                    formVersionId: 565,
                    providerDetails: {},
                    availabilityApi: `${coreApiUrl}/availability`,
                    evaluationApi: `${coreApiUrl}/evaluation`, 
                    memberApi : `${coreApiUrl}/member`, 
                    productApi : `${coreApiUrl}/product`, 
                    appointmentApi : `https://appointmentapi.signifyhealth.com/`,
                    okta: {
                        authUrl: 'https://login.signifyhealth.com',
                        clientId: '',
                        nonce: 'n-0S6_WzA2Mj',
                        redirectUri: 'https://coreapi.signifyhealth.com/evaluation/swagger/oauth2-redirect.html',
                        scope: 'profile openid availabilityapi clientapi evaluationapi memberapi productapi providerapi appointmentapi',
                        state: 'Af0ifjslDkj',
                        credentials: {}
                    }
                }
                config.providerDetails = {
                    providerId: 26985,
                    nationalProviderIdentifier: '8441956355',
                    firstName: 'Test89167',
                    lastName: 'Test89167',
                    fullName: 'Test89167, Test89167'
                    }

            // Load the app configuration into memory for Prod(includes connection strings, users, etc)
            // See appConfig/README.md for more information
            try {
                config = karate.callSingle('classpath:appConfig/loadProdConfig.js', config)
            } catch (err) {
                karate.log('Unable to load loadProdConfig...')
                throw err
            }

            // Get and set the OKTA token to be used in all requests
            let token = karate.callSingle('classpath:helpers/oauth/createToken.feature', config).token;
            karate.configure('headers', { Authorization: `Bearer ${token}` });
        }
            break;
        }
        default: 
        {
            if (databaseOnly) 
            {
                try {
                    config = karate.callSingle('classpath:appConfig/loadDatabaseOnlyConfig.js', config);
                } catch (err) {
                    karate.log('Unable to load loadDatabaseOnlyConfig...');
                    throw err;
                }
            }
            else 
            {
                const coreApiUrl = `https://coreapi.${env}.signifyhealth.com`;

                config = {
                    env: env,
                    //Add UACR once product added in UAT
                    products: ["CKD", "DEE", "EGFR", "HHRA", "PAD", "SPIROMETRY", "FOBT", "HBA1CPOC","UACR"],
                    planId: 37,
                    planName: 'Automation',
                    formVersionId: 595,
                    providerDetails: {},
                    availabilityApi: `${coreApiUrl}/availability`,
                    capacityApi: `${coreApiUrl}/capacity`,
                    evaluationApi: `${coreApiUrl}/evaluation`,
                    memberApi: `${coreApiUrl}/member`,
                    productApi: `${coreApiUrl}/product`,
                    providerApi: `${coreApiUrl}/provider`,
                    appointmentApi: `https://appointmentapi.${env}.signifyhealth.com/`,
                    providerPayApi: `https://finance.${env}.signifyhealth.com/providerpay/v1`,
                    rcmApi: `https://rcm.${env}.signifyhealth.com/api/`,
                    iris: {
                        authUrl: 'https://login.microsoftonline.com/68d3bc70-138b-4837-99d2-02a0d226259e/oauth2/v2.0/token',
                        apiUrl: 'https://irissignifyapi-qa.retinalscreenings.com/api',
                        clientId: '204d003c-c1db-4699-aa08-58ea49583e0a',
                    },
                    okta: {
                        authUrl: 'https://login-staging.signifyhealth.com/',
                        clientId: '0oaq0wnz6kZOdapot0h7',
                        nonce: 'n-0S6_WzA2Mj',
                        redirectUri: 'http://localhost:5000/swagger/oauth2-redirect.html',
                        scope: 'credentialing profile openid availabilityapi capacityapi clientapi evaluationapi memberapi productapi providerapi appointmentapi providerpayapi',
                        state: 'Af0ifjslDkj',
                        credentials: {}
                    }
                };
        
                // Feature Flags
                config.padWaveformsFlag = karate.properties['FeatureFlags:PadWaveforms'] || 'false';
        
                // Load the app configuration into memory (includes connection strings, users, etc)
                // See appConfig/README.md for more information
                try {
                    config = karate.callSingle('classpath:appConfig/load-config.feature', {config:config}).config;
                } catch (err) {
                    karate.log('Unable to load loadConfig...');
                    throw err;
                }
        
                // Setup Kafka
                karate.log('Creating KafkaConsumerHelper');
                let kafkaHelper = Java.type('helpers.kafka.KafkaConsumerHelper');
                kafkaHelper.Instance();
        
                // Get and set the OKTA token to be used in all requests
                let token = karate.callSingle('classpath:helpers/oauth/createToken.feature', config).token;
                karate.configure('headers', { Authorization: `Bearer ${token}` });
        
                // Create a provider to schedule all our appointments with
                config.providerDetails = karate.callSingle('classpath:helpers/provider/createProvider.feature', config).providerDetails;
            }
            break;
        }
    }
    return config;
}