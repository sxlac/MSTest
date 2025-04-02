ALTER TABLE public."FOBT"
ALTER COLUMN "ReceivedDateTime"
set Data type TIMESTAMP;

UPDATE public."FOBTStatusCode"
SET "StatusCode" = 'FOBT-Left'
where "FOBTStatusCodeId" = 12;

UPDATE public."FOBTStatusCode"
SET "StatusCode" = 'FOBT-Results'
where "FOBTStatusCodeId" = 13;
