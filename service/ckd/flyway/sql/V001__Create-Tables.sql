CREATE TABLE "CKDStatusCode"
(
    "CKDStatusCodeId" SERIAL PRIMARY KEY,
    "StatusCode" VARCHAR(250) NOT NULL
);

CREATE TABLE "CKD"
(
    "CKDId" SERIAL PRIMARY KEY,
    "EvaluationId" INTEGER NULL,
    "MemberPlanId" INTEGER  NULL,
    "MemberId" INTEGER  NULL,
    "CenseoId" VARCHAR(8) NULL,
    "AppointmentId" INTEGER,
    "ProviderId" INTEGER NULL,
    "DateOfService" TIMESTAMP WITH TIME ZONE NULL,
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "ReceivedDateTime" DATE NOT NULL,
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
	"NationalProviderIdentifier" VARCHAR(10)   NULL,
	"ExpirationDate"  TIMESTAMP     NULL,
	"CKDAnswer" VARCHAR(200)   NULL

);

CREATE TABLE "CKDStatus"
(
    "CKDStatusId" SERIAL PRIMARY KEY,
    "CKDStatusCodeId" INTEGER NOT NULL REFERENCES "CKDStatusCode"("CKDStatusCodeId"),
    "CKDId" INTEGER NOT NULL REFERENCES "CKD"("CKDId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

INSERT INTO "CKDStatusCode" ("CKDStatusCodeId", "StatusCode") VALUES  (1, 'CKDPerformed');
INSERT INTO "CKDStatusCode" ("CKDStatusCodeId", "StatusCode") VALUES  (2, 'InventoryUpdateRequested');
INSERT INTO "CKDStatusCode" ("CKDStatusCodeId", "StatusCode") VALUES  (3, 'InventoryUpdateSuccess');
INSERT INTO "CKDStatusCode" ("CKDStatusCodeId", "StatusCode") VALUES  (4, 'InventoryUpdateFail');
INSERT INTO "CKDStatusCode" ("CKDStatusCodeId", "StatusCode") VALUES  (5, 'BillRequestSent');



GRANT ALL ON DATABASE ckd TO ckdsvc WITH GRANT OPTION;


