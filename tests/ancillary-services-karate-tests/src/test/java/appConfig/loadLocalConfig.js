function loadAppConfig(config) {
    try {
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

       if (!appConfig[config.env].kafka.bootstrapServers) {
            throw `The appConfig.json does not contain an object for kafka,
                    Ensure this key exist in the kafka object before attempting to run!`
        } else {
            Java.type('java.lang.System').setProperty(`KAFKA_BOOTSTRAP_SERVERS`, appConfig[config.env].kafka.bootstrapServers)
            Java.type('java.lang.System').setProperty(`KAFKA_GROUP_ID`, appConfig[config.env].kafka.groupID)
        }

        // SMB details get loaded as Java properties so they can be read directly from Java classes
        for (const smb of appConfig[config.env].smb) {
            if (!smb.username || !smb.password) {
                throw `SMB index ${smb} is missing either "username" or "password" in appConfig.json".`
            }

            Java.type('java.lang.System').setProperty('SMB_USERNAME', smb.username)
            Java.type('java.lang.System').setProperty('SMB_PASSWORD', smb.password)
        }

    } catch (err) {
        // We might be running in a Docker container instead of locally
        Java.type('java.lang.System').setProperty(`KAFKA_BOOTSTRAP_SERVERS`, appConfig[config.env].kafka.bootstrapServers)
        Java.type('java.lang.System').setProperty(`KAFKA_GROUP_ID`, appConfig[config.env].kafka.groupID)
    }
    return config
}