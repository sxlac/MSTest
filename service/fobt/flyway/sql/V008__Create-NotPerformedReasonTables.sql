-- All reasons why an evaluation with a FOBT product may not be performed
CREATE TABLE IF NOT EXISTS public."NotPerformedReason"
(
    "NotPerformedReasonId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "AnswerId" INTEGER NOT NULL UNIQUE,
    "Reason" VARCHAR(256) NOT NULL 
);

ALTER TABLE public."NotPerformedReason"
    OWNER to fobtsvc;

INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30879, 'Member recently completed'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30879
    );
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30880, 'Scheduled to complete'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30880
    );
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30881, 'Member apprehension'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30881
    );
	 
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30882, 'Not interested'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30882
    );
	 

INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30883, 'Other'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30883
    );
	 

INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30886, 'Technical issue'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30886
    );
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30887, 'Environmental issue'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30887
    );
	 
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30888, 'No supplies or equipment'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30888
    );
	 
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 30889, 'Insufficient training'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 30889
    );
	 
	 
INSERT INTO public."NotPerformedReason"
    ("AnswerId", "Reason")
SELECT 50908, 'Member physically unable'
WHERE
    NOT EXISTS (
        SELECT "AnswerId" FROM public."NotPerformedReason" WHERE "AnswerId" = 50908
    );
	 

-- Details about evaluations where a FOBT exam was not performed
CREATE TABLE IF NOT EXISTS public."FOBTNotPerformed"
(
    "FOBTNotPerformedId" SERIAL PRIMARY KEY NOT NULL,
    "FOBTId" INTEGER NOT NULL UNIQUE REFERENCES "FOBT" ("FOBTId"),
    "NotPerformedReasonId" SMALLINT NOT NULL REFERENCES "NotPerformedReason" ("NotPerformedReasonId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."FOBTNotPerformed"
    OWNER to fobtsvc;

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO fobtsvc;