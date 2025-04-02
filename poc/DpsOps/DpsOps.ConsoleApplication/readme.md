# Summary

This application is intended to be used by a member of the DPS Fulfillment Operations
team that handles provider re-supply requests for various consumable supplies (ex
alcohol prep pads, gloves, FOBT and KED kits, Spirometry turbines and sanitizing wipes
(see [RequestableSupplies.json](https://dev.azure.com/signifyhealth/HCC/_git/careconsult?path=%2FDatastore%2FResources%2FRequestableSupplies.json&_a=contents&version=GBmaster)
for a complete list)). The purpose of this application is to help automate matching
mailing addresses that providers manually enter into their iPad when requesting supplies
to a master "Ship To" identifier that is needed for Fulfillment Operations to submit
an order for re-supply.

# Application Instructions

## DPS Fulfillment Operations' Instructions

See [Instructions.txt](https://dev.azure.com/signifyhealth/HCC/_git/ancillary?path=/poc/DpsOps/DpsOps.ConsoleApplication/Instructions.txt).
This file is included in the published directory alongside the generated
.exe and is to be shared with the Fulfillment Operations associate that
will use this application.

## Simplified Developer Instructions

This program accepts the following optional arguments:

1) Provider Supply Request file path (defaults to `provider.txt`)
2) Master List (of provider Ship To addresses) file path (defaults to `master.txt`)
3) Output file path (defaults to `output_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt`)
4) Delimiter (defaults to Tab)

Note, the paths can be relative or absolute paths.

The output file will be based on the Provider Supply Request contents,
with matched Ship To Numbers added.

### Sharing the application with the Fulfillment Operations team

This application can be published in a self-contained deployment mode,
so it can easily be deployed directly to a DPS/Fulfillment Operations
team member's (Windows) workstation for their self-service use.

Right-click the Console Application project in your IDE, click Publish
and select an output directory. The application will be packaged in an
.exe and the Instructions.txt file will be copied to the same location.
These can be compressed in a .zip file and shared via Slack or email to
the Fulfillment Operations team.
