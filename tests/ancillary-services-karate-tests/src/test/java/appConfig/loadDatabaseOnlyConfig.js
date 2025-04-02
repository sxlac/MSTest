function loadAppConfig(config) {
    try {
        read('classpath:appConfig/appConfig.json');
    } catch (err) {
        throw new Error (`Unable to find the appConfig.json file!
        Please ensure this file exists in the appConfig folder.
        Request a copy of the file from the DPS team QE if needed.
        ${err}`);
    }

    let appConfig = read('classpath:appConfig/appConfig.json');
    karate.log('Configuring using appConfig/appConfig.json!');

    if (!appConfig[config.env]) {
        throw new Error(`The appConfig.json does not contain an object for the ${config.env} config.environment.
                Ensure that this object exists before attempting to run for this config.environment!`);
    }

    if (!appConfig[config.env].databases) {
        throw new Error (`The appConfig.json does not have databases loaded for ${config.env}.
                An array object of databases containing the name, url, username, and password must be included.`);
    }

    // DB details get loaded as Java properties so they can be read directly in Java classes		
    for (const db of appConfig[config.env].databases) {
        if (!db.url && !db.username && !db.password) {
            throw new Error (`${db} is missing either "url", "username", or "password" in appConfig.json.
                Ensure that these keys exist in the ${db} object before attempting to run!`);
        }
        Java.type('java.lang.System').setProperty(`${db.name.toUpperCase()}_DB_URL`, db.url);
        Java.type('java.lang.System').setProperty(`${db.name.toUpperCase()}_DB_USERNAME`, db.username);
        Java.type('java.lang.System').setProperty(`${db.name.toUpperCase()}_DB_PASSWORD`, db.password);
    }
    return config;
}