INSERT INTO "NotPerformedReason" ("AnswerId", "Reason")
SELECT 52851, 'Other'
WHERE NOT EXISTS (SELECT "NotPerformedReasonId" FROM public."NotPerformedReason" where "AnswerId" = 52851);