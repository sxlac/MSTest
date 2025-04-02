# Ancillary Services Karate Tests
This project runs Karate tests againsts the various Ancillary Services (Process Managers).

## Project Requirements and Dependencies
### Must be installed
- [JDK](https://www.oracle.com/java/technologies/javase-jdk11-downloads.html) - Required for Karate
- [Maven](https://maven.apache.org/users/index.html) - Required for Karate
- [Docker](https://www.docker.com/)
- [src/test/java/appConfig/appConfig.json](src/test/java/appConfig/) - Ask a team member for a copy of this file and drop it in this directory

### Used by the project but requires no install
- [Karate](https://github.com/intuit/karate) - The core automation framework
- [KafkaDoge](https://dev.azure.com/signifyhealth/HCC/_git/testops?path=/KafkaDoge/KafkaDoge.API) - Provides a RESTful interface for Kafka

### Recommended IDE and Extensions
- [VSCode](https://code.visualstudio.com/) - Preferred IDE due to extension support
- [Karate Runner Extension](https://marketplace.visualstudio.com/items?itemName=kirkslota.karate-runner) - Provides interface for running and debugging Karate tests
- [Cucumber (Gherkin) Full Support](https://marketplace.visualstudio.com/items?itemName=kirkslota.karate-runner) - Provides support for Feature files
- [Extension Pack for Java](https://marketplace.visualstudio.com/items?itemName=vscjava.vscode-java-pack) - Provides support for Java

## Setup
Install the requirements mentioned avove.

Karate Runner extension need to be configured 
1. enable `Karate Runner › Karate Cli: Override Karate Runner` checkbox
2. set number of threads to use. If we want to run tests in multiple threads, set `Karate Runner › Karate Cli: Command Line Args` to `-T x` where `x` is number of threads to use. Ex: `-T 10`. By default this field is empty and local karate test uses 1 thread.

Ensure that the appConfig.json file is provided in the appConfig folder or the project will not execute.
Please request a copy of this file from the current QE members of the DPS team.

## Execution
The environment can be passed in using `-Dkarate.env="env"` else it will default to karate-config.js.
A specific process manager can be run by providing `"-Dkarate.options=--tags @tag"` wheretag is replaced with the tag from the feature file.
Feature flags can be passed in by providing `-DFeatureFlags.FlagName=boolean` but the flag must be implemented in karate-config.js and supporting feature files.

Inorder to run Karate tests from local machine VS Code, click on the `Karate: Run` button above a scenario or at top of each feature file

### Karate Outside of Docker
**You must start the KafkaDoge container or you will experience failures!**
1. Start [KafkaDoge](https://dev.azure.com/signifyhealth/HCC/_git/testops?path=/KafkaDoge/KafkaDoge.API/Readme.md):
    Follow the instructions on KafkaDoge repo's README.md page
    or run
    `docker-compose up` - KafkaDoge will be running on [localhost](http://localhost:5013) and docs are [here](http://localhost:5013/swagger/index.html)
    Update the architecture of your machine in the docker-compose.yml file before running the command locally.

2. Run tests without Docker (multiple examples, only one is required):
    `mvn test -Dkarate.env="uat"` - would run all tests against UAT
    `mvn test -Dkarate.env="uat" "-Dkarate.options=--tags @pad"` - Would run tests against UAT for PAD only

### Karate Inside of Docker
Same option and tagging system as running locally applies here.

`docker-compose run ancillary-karate-tests mvn test -Dkarate.env="uat"` - Would run all tests against UAT

`docker-compose run ancillary-karate-tests mvn test -Dkarate.env="uat" "-Dkarate.options=--tags @pad"` - Would run tests against UAT for PAD only
    
`docker-compose run -e <ENV_VAR1> -e <ENV_VAR2>...-e <ENV_VARn> ancillary-karate-tests mvn test -Dkarate.env="$(karate.env)" "-Dkarate.options=--tags @spirometry"` - Would run all the spirometry tests when the credentials to DB, Kafka etc are passed as environment variables

### Debugging
1. Click `Karate: Debug` Codelens in any feature file
2. Click `Karate (debug)` option from popup
3. `launch.json` file within `.vscode` folder opens up
4. Click on `Add Configuration` button on the bottom right
5. Select `Karate (debug): Maven`  and delete any other configurations present
6. Inorder to run tests in multiple threads during debug, set `"karateOptions": "-T x"` where `x` is the number of threads to use ex: `-T 10`
7. Go to any test and hit `Karate: Debug` to start debugging. 

For more details visit `Karate Runner` extension `Details` page