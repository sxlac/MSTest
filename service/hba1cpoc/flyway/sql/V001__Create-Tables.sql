CREATE TABLE "HBA1CPOCStatusCode"
(
    "HBA1CPOCStatusCodeId" SERIAL PRIMARY KEY,
    "StatusCode" VARCHAR(250) NOT NULL
);

CREATE TABLE "HBA1CPOC"
(
    "HBA1CPOCId" SERIAL PRIMARY KEY,
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
    "ApplicationId" VARCHAR(200) NOT NULL
);

CREATE TABLE "HBA1CPOCStatus"
(
    "HBA1CPOCStatusId" SERIAL PRIMARY KEY,
    "HBA1CPOCStatusCodeId" INTEGER NOT NULL REFERENCES "HBA1CPOCStatusCode"("HBA1CPOCStatusCodeId"),
    "HBA1CPOCId" INTEGER NOT NULL REFERENCES "HBA1CPOC"("HBA1CPOCId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL
);

INSERT INTO "HBA1CPOCStatusCode" ("HBA1CPOCStatusCodeId", "StatusCode") VALUES  (1, 'HBA1CPOCPerformed');
INSERT INTO "HBA1CPOCStatusCode" ("HBA1CPOCStatusCodeId", "StatusCode") VALUES  (2, 'InventoryUpdateRequested');
INSERT INTO "HBA1CPOCStatusCode" ("HBA1CPOCStatusCodeId", "StatusCode") VALUES  (3, 'InventoryUpdateSuccess');
INSERT INTO "HBA1CPOCStatusCode" ("HBA1CPOCStatusCodeId", "StatusCode") VALUES  (4, 'InventoryUpdateFail');
INSERT INTO "HBA1CPOCStatusCode" ("HBA1CPOCStatusCodeId", "StatusCode") VALUES  (5, 'BillRequestSent');



GRANT ALL ON DATABASE hba1cpoc TO hba1cpocsvc WITH GRANT OPTION;


