-- Add new column
ALTER TABLE public."AoeSymptomSupportResult"
ADD COLUMN IF NOT EXISTS "HasClinicalSupportForAoeWithRestingLegPain" BOOLEAN NULL;
