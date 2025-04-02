package ancillary_services;

import com.intuit.karate.Results;
import com.intuit.karate.Runner;
import com.intuit.karate.core.ScenarioResult;

import helpers.zephyr.ZipJsonFileAndMove;

import static org.junit.jupiter.api.Assertions.*;

import org.junit.jupiter.api.Test;

import net.masterthought.cucumber.Configuration;
import net.masterthought.cucumber.ReportBuilder;
import org.apache.commons.io.FileUtils;
import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;
import java.util.stream.Collectors;
class TestAncillaryServices {
    private static final int retryCount = 5;

    @Test
    void testParallel() {
            Results results = Runner.path("classpath:ancillary_services")
            .outputJunitXml(true)
            .outputCucumberJson(true)
            .tags("~@ignore", "~@mocks", "~@performance")
            .parallel(10);
            
            System.out.printf("Checking for any failed tests\n");

            if (results.getFailCount() > 0) 
            {
                System.out.printf("%s test failed\n", results.getFailCount());
                 for (ScenarioResult scenarioResult : results.getScenarioResults().collect(Collectors.toList())) 
                    {
                        if (scenarioResult.isFailed()) 
                        {
                            boolean failed = true;
                            System.out.printf("Retrying the following test %s\n", scenarioResult.getScenario().getName());
                            for (int i = 0; i < retryCount; i++) {
                                ScenarioResult retryScenarioResult = results.getSuite().retryScenario(scenarioResult.getScenario());

                                if (!retryScenarioResult.isFailed()) 
                                {
                                    failed = false;
                                    results = results.getSuite().updateResults(retryScenarioResult);
                                    break;
                                }                              
                            } 
                            if(failed)
                            {
                                System.out.printf("After retrying, test %s is still failing. Breaking out as no point in continuing with the retries\n", scenarioResult.getScenario().getName());
                                break;
                            }                                                      
                        }
                    }
                    if (results.getFailCount() == 0){
                        System.out.printf("After retrying, all tests are now successful\n");
                    }    
            } 
            else
            {
                System.out.printf("All tests were successful, generating report\n");
            }  
        generateReport(results.getReportDir(), getClass().getSimpleName());
        assertTrue(results.getFailCount() == 0, results.getErrorMessages());
    }
     public static void generateReport(final String karateOutputPath, String className) {
        final Collection<File> jsonFiles = FileUtils.listFiles(new File(karateOutputPath), new String[] { "json" }, true);
        final List<String> jsonPaths = new ArrayList<String>(jsonFiles.size());
        jsonFiles.forEach(file -> jsonPaths.add(file.getAbsolutePath()));
        final Configuration config = new Configuration(new File("target"), className);
        final ReportBuilder reportBuilder = new ReportBuilder(jsonPaths, config);
        reportBuilder.generateReports();
        zephyrTestCycle();
    }

    private static void zephyrTestCycle() {
        //Zip the Cucumber JSON report file for zephyr reporting
        try {
            ZipJsonFileAndMove.zipJsonFiles();
            ZipJsonFileAndMove.fileMover();
            if(ZipJsonFileAndMove.sendFileToZephyr()){
                System.out.println("Zephyr Test Cycle created");
            }
            else{
                System.out.println("Zephyr Test Cycle not created due to an error!");
            }

        } catch (IOException e) {
            e.printStackTrace();
        }
    }
}