CREATE TABLE "PADStatusCode"
(
    "PADStatusCodeId" SERIAL PRIMARY KEY,
    "StatusCode" VARCHAR(250) NOT NULL
);

CREATE TABLE "LookupPADAnswer"
(
	"PADAnswerId" INTEGER PRIMARY KEY NOT NULL,
    "PADAnswerValue" VARCHAR(200) NOT NULL
);

CREATE TABLE "PAD"
(
    "PADId" SERIAL PRIMARY KEY,
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
	"LeftScoreAnswerValue" VARCHAR(200) NOT NULL,
	"LeftSeverityAnswerValue" VARCHAR(200) NOT NULL,
	"RightScoreAnswerValue" VARCHAR(200) NOT NULL,
	"RightSeverityAnswerValue" VARCHAR(200) NOT NULL
);

CREATE TABLE "PADStatus"
(
    "PADStatusId" SERIAL PRIMARY KEY,
    "PADStatusCodeId" INTEGER NOT NULL REFERENCES "PADStatusCode"("PADStatusCodeId"),
    "PADId" INTEGER NOT NULL REFERENCES "PAD"("PADId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

INSERT INTO "PADStatusCode" ("PADStatusCodeId", "StatusCode") VALUES  (1, 'PADPerformed');
INSERT INTO "PADStatusCode" ("PADStatusCodeId", "StatusCode") VALUES  (2, 'BillRequestSent');

INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31042, 'Normal');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31043, 'Mild');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31044, 'Moderate');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31045, 'Significant');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31046, 'Severe');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31047, 'Normal');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31048, 'Mild');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31049, 'Moderate');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31050, 'Significant');
INSERT INTO "LookupPADAnswer" ("PADAnswerId", "PADAnswerValue") VALUES  (31051, 'Severe');

GRANT ALL ON DATABASE pad TO padsvc WITH GRANT OPTION;


