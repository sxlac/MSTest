# NServiceBus Messenger Tool

This project allows engineers to send messages to different NServiceBus Queues.
[NServiceBus Messenger Tool Wiki Page](https://cvs-hcd.atlassian.net/wiki/spaces/AncillarySvcs/pages/51227401/NServiceBus+Messenger+Tool)

## Setup
This tool is designed to run off the settings in the appsettings.json file.  You can determine which process manager queue, event message and action you want to perform by updating the settings file.

The settings file also contains the settings 

## AppSettings Sections and Descriptions

- Logging & Serilog - Settings for what is displayed in the console when the tool runs

- ConnectionStrings - For now just a connection string to the Azure Service Bus

- CsvSettings - Default settings for reading and writing to a CSV file

- NServiceBusSettings - Setting for what action the tool will take when it runs.

### NServiceBusSettings

As of now, the tool can generate a template file based on any of the messages and send messages to a configured NServiceBus Queue.  We have to setup the settings file in a specific way to tell the tool what we want to accomplish.

Below is a sample of the settings file with the key properties that will allow us to generate a template file.  The reason we want to generate a template file instead of keeping a collection of them is due to the fact that the messages being used by each process manager can change.  Once they change the template will be out of date.  The tool is setup in a way that with any changes made to the messages they will be incorporated in this tool.

```
{
  "NServiceBusSettings": {
    "ProcessManager": "DEE",
    "EventMessage": "ProcessDee",
    "ActionType": "GenerateTemplateFile",
    "OutputFileLocation": "C:\\temp\\",
  }
}
```

This example above will generate a template file for the DEE ProcessDee event message.  We need to set the ProcessManager property to one of the valid Process Manager values.  The following list contains the values for each Process Manager.
- CKD
- DEE
- EGFR
- FOBT
- HBA1CPOC
- HBA1C
- PAD
- Spirometry

The EventMessage property represents the class name of the event we want to generate the template for.  We don't need to provide the entire namespace, just the class name.

The ActionType property needs to be set to "GenerateTemplateFile", otherwise it will not generate a template file.

The OutputFileLocation property tells the tool where to generate the template CSV file.  We don't need to provide a name for the file, that will get generated to the format "{EventMessage Value}_Template_{DateTime value}.csv".


Below is a sample of the settings file with the key properties that will send a message to one of the NServiceBus queue.

```
{
  "NServiceBusSettings": {
    "ProcessManager": "DEE",
    "EventMessage": "CreateDee",
    "ActionType": "SendMessage",
    "InputFileLocationAndName": "C:\\temp\\TestCreateDee.csv",
  }
}
```

This example above will read the values set in the TestCreateDee.csv and create a message for each row and send it to the NServiceBus queue.  We need to set the following properites listed below for this process to work.

The ProcessManager property needs to be set to one of the valid Process Manager values.  The following list contains the values for each Process Manager.
- CKD
- DEE
- EGFR
- FOBT
- HBA1CPOC
- HBA1C
- PAD
- Spirometry

The EventMessage property represents the class name of the event that we want to send to the NServiceBus queue.  We don't need to provide the entire namespace, just the class name.

The ActionType property needs to be set to "SendMessage", otherwise it will not send any messages to the configured NServiceBus queue.

The InputFileLocationAndName property needs to be set to the directory the CSV is stored and the name of that CSV file.

## Execution

To run the tool you can either run it in Visual Studio or build the solution and run the exe (Signify.Tools.MessageQueue.exe).  If you run it by the exe file nothing else needs to be provided when running it.  Just make sure the appsettings file is setup correctly.