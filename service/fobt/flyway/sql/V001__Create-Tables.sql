CREATE TABLE "FOBTStatusCode"
(
    "FOBTStatusCodeId" SERIAL PRIMARY KEY,
    "StatusCode" VARCHAR(250) NOT NULL
);

CREATE TABLE "FOBT"
(
    "FOBTId" SERIAL PRIMARY KEY,
    "EvaluationId" INTEGER NULL,
    "MemberPlanId" INTEGER  NULL,
    "MemberId" INTEGER  NULL,
    "CenseoId" VARCHAR(8) NULL,
    "AppointmentId" INTEGER,
    "ProviderId" INTEGER NULL,
    "DateOfService" TIMESTAMP WITH TIME ZONE NULL,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "ReceivedDateTime" DATE NOT NULL,
	"Barcode" VARCHAR(200) NOT NULL,
    "ClientId" INTEGER NULL,
    "UserName" VARCHAR(250) NOT NULL,
    "ApplicationId" VARCHAR(200) NOT NULL,
	"FirstName" VARCHAR(100)  NULL,
	"MiddleName" VARCHAR(100)  NULL,
	"LastName"	VARCHAR(100)  NULL,
	"DateOfBirth" TIMESTAMP     NULL,
	"AddressLineOne" VARCHAR(200)  NULL,
	"AddressLineTwo" VARCHAR(200)  NULL,
	"City"	VARCHAR(100)  NULL,
	"State"	VARCHAR(2)    NULL,
	"ZipCode" VARCHAR(5)    NULL,
	"NationalProviderIdentifier" VARCHAR(10)   NULL
	
);

CREATE TABLE "FOBTStatus"
(
    "FOBTStatusId" SERIAL PRIMARY KEY,
    "FOBTStatusCodeId" INTEGER NOT NULL REFERENCES "FOBTStatusCode"("FOBTStatusCodeId"),
    "FOBTId" INTEGER NOT NULL REFERENCES "FOBT"("FOBTId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode") VALUES  (1, 'FOBTPerformed');
INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode") VALUES  (2, 'InventoryUpdateRequested');
INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode") VALUES  (3, 'InventoryUpdateSuccess');
INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode") VALUES  (4, 'InventoryUpdateFail');
INSERT INTO "FOBTStatusCode" ("FOBTStatusCodeId", "StatusCode") VALUES  (5, 'BillRequestSent');



GRANT ALL ON DATABASE fobt TO fobtsvc WITH GRANT OPTION;


