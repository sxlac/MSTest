-- All reasons why an evaluation with a HB1CPOC product may not be performed
CREATE TABLE IF NOT EXISTS public."NotPerformedReason"
(
    "NotPerformedReasonId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "AnswerId" INTEGER NOT NULL UNIQUE,
    "Reason" VARCHAR(256) NOT NULL 
);

ALTER TABLE public."NotPerformedReason"
    OWNER to hba1cpocsvc;

INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33074, 'Member recently completed'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33074
    );
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33075, 'Scheduled to complete'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33075
    );
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33076, 'Member apprehension'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33076
    );
	 
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33077, 'Not interested'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33077
    );
	 

INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33078, 'Other'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33078
    );
	 

INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33081, 'Technical issue'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33081
    );
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33082, 'Environmental issue'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33082
    );
	 
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33083, 'No supplies or equipment'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33083
    );
	 
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 33084, 'Insufficient training'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 33084
    );
	 
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 50905, 'Member physically unable'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 50905
    );
	 

-- Details about evaluations where a HB1CPOC exam was not performed
CREATE TABLE IF NOT EXISTS public."HBA1CPOCNotPerformed"
(
    "HBA1CPOCNotPerformedId" SERIAL PRIMARY KEY NOT NULL,
    "HBA1CPOCId" INTEGER NOT NULL UNIQUE REFERENCES "HBA1CPOC" ("HBA1CPOCId"),
    "NotPerformedReasonId" SMALLINT NOT NULL REFERENCES "NotPerformedReason" ("NotPerformedReasonId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."HBA1CPOCNotPerformed"
    OWNER to hba1cpocsvc;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hba1cpocsvc;