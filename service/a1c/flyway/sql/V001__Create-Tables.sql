CREATE TABLE "A1CStatusCode"
(
    "A1CStatusCodeId" SERIAL PRIMARY KEY,
    "StatusCode" VARCHAR(250) NOT NULL
);

CREATE TABLE "A1C"
(
    "A1CId" SERIAL PRIMARY KEY,
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
	"FirstName"                    VARCHAR(100) NULL,
	"MiddleName"                   VARCHAR(100) NULL,
	"LastName"                     VARCHAR(100) NULL,
	"DateOfBirth"                  TIMESTAMP     NULL,
	"AddressLineOne"               VARCHAR(200)  NULL,
	"AddressLineTwo"               VARCHAR(200) NULL,
	"City"                         VARCHAR(100) NULL,
	"State"                        VARCHAR(2)  NULL,
	"ZipCode"                      VARCHAR(5)  NULL,
	"NationalProviderIdentifier"   VARCHAR(10) NULL
);

CREATE TABLE "A1CStatus"
(
    "A1CStatusId" SERIAL PRIMARY KEY,
    "A1CStatusCodeId" INTEGER NOT NULL REFERENCES "A1CStatusCode"("A1CStatusCodeId"),
    "A1CId" INTEGER NOT NULL REFERENCES "A1C"("A1CId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

INSERT INTO "A1CStatusCode" ("A1CStatusCodeId", "StatusCode") VALUES  (1, 'A1CPerformed');
INSERT INTO "A1CStatusCode" ("A1CStatusCodeId", "StatusCode") VALUES  (2, 'InventoryUpdateRequested');
INSERT INTO "A1CStatusCode" ("A1CStatusCodeId", "StatusCode") VALUES  (3, 'InventoryUpdateSuccess');
INSERT INTO "A1CStatusCode" ("A1CStatusCodeId", "StatusCode") VALUES  (4, 'InventoryUpdateFail');
INSERT INTO "A1CStatusCode" ("A1CStatusCodeId", "StatusCode") VALUES  (5, 'LabResultsReceived');
INSERT INTO "A1CStatusCode" ("A1CStatusCodeId", "StatusCode") VALUES  (6, 'BarcodeUpdated');
INSERT INTO "A1CStatusCode" ("A1CStatusCodeId", "StatusCode") VALUES  (7, 'BillRequestSent');

--ALTER TABLE public."Users" OWNER TO signifypostgres;




GRANT ALL ON DATABASE A1C TO a1csvc WITH GRANT OPTION;


