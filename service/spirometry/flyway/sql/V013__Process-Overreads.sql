ALTER TABLE public."SpirometryExamResults"
RENAME COLUMN "FVC" TO "Fvc";

ALTER TABLE public."SpirometryExamResults"
RENAME COLUMN "FEV1" TO "Fev1";

ALTER TABLE public."SpirometryExamResults"
RENAME COLUMN "FEV1_Over_FVC" TO "Fev1FvcRatio";

ALTER TABLE public."SpirometryExamResults"
RENAME COLUMN "FVCNormalityIndicatorId" TO "FvcNormalityIndicatorId";

ALTER TABLE public."SpirometryExamResults"
RENAME COLUMN "FEV1NormalityIndicatorId" TO "Fev1NormalityIndicatorId";

-- New column to track processed overread result, without having to overwrite and lose POC result.
ALTER TABLE public."SpirometryExamResults"
ADD COLUMN "OverreadFev1FvcRatio" NUMERIC(5,2); -- xxx.xx

-- No need to add new column for ObstructionPerOverread. The existing NormalityIndicator column
-- (Normal/Abnormal/Undetermined) can simply be overwritten, because for all clinically-invalid
-- POC results (ie D/E/F), the Normality is Undetermined. If there's a non-null value in the new
-- OverreadFev1FvcRatio column, we know POC was Undetermined (and can also tell from the Session
-- Grade).

ALTER TABLE public."SpirometryExamResults"
ALTER COLUMN "Fev1FvcRatio" TYPE NUMERIC(5,2); -- Previously 'NUMERIC(3,2)', ie x.xx
-- We occasionally get Performed evals (granted, not billable, because results are invalid),
-- that end up in the error queue because they cannot be inserted into the db due to being
-- outside of this precision. This is due to providers improperly entering the results into
-- the form (for example, the actual result from the test may be 0.89, but the provider
-- enters 89 into the form). I don't believe the form has any validation on the value entered,
-- other than it just needs to be a valid number. This should be resolved once the deep-link
-- is completed and all providers update the app, but I figure we can increase the precision
-- supported in the db since we have to make changes here now anyways, so that these no longer
-- end up in the error queue and they can get processed like normally. As mentioned, though
-- these are not billable, but at least we'd have a record of them.

INSERT  INTO public."StatusCode"
        ("Name")
VALUES  ('Overread Processed');
