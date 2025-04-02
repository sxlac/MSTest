ALTER TABLE "LookupCKDAnswer"
ADD COLUMN IF NOT EXISTS "Albumin" 						INTEGER NULL,
ADD COLUMN IF NOT EXISTS "Creatinine" 					NUMERIC(2,1) NULL,
ADD COLUMN IF NOT EXISTS "Acr"							VARCHAR(16) NULL,
ADD COLUMN IF NOT EXISTS "NormalityIndicator"			VARCHAR(1) NULL,
ADD COLUMN IF NOT EXISTS "Severity" 					VARCHAR(16) NULL;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='10', "Creatinine"='0.1', "NormalityIndicator"='U'
WHERE "CKDAnswerId" = 20962;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='30', "Creatinine"='0.1',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20963;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='80', "Creatinine"='0.1',"Acr"='> 300', "NormalityIndicator"='A',"Severity"='High'
WHERE "CKDAnswerId" = 20964;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='150', "Creatinine"='0.1',"Acr"='> 300', "NormalityIndicator"='A',"Severity"='High'
WHERE "CKDAnswerId" = 20965;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='10', "Creatinine"='0.5',"Acr"='< 30', "NormalityIndicator"='N'
WHERE "CKDAnswerId" = 20966;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='30', "Creatinine"='0.5',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20967;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='80', "Creatinine"='0.5',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20968;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='150', "Creatinine"='0.5',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20969;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='10', "Creatinine"='1.0',"Acr"='< 30', "NormalityIndicator"='N'
WHERE "CKDAnswerId" = 20970;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='30', "Creatinine"='1.0',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20971;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='80', "Creatinine"='1.0',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20972;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='150', "Creatinine"='1.0',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20973;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='10', "Creatinine"='2.0',"Acr"='< 30', "NormalityIndicator"='N'
WHERE "CKDAnswerId" = 20974;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='30', "Creatinine"='2.0',"Acr"='< 30', "NormalityIndicator"='N'
WHERE "CKDAnswerId" = 20975;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='80', "Creatinine"='2.0',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20976;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='150', "Creatinine"='2.0',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20977;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='10', "Creatinine"='3.0',"Acr"='< 30', "NormalityIndicator"='N'
WHERE "CKDAnswerId" = 20978;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='30', "Creatinine"='3.0',"Acr"='< 30', "NormalityIndicator"='N'
WHERE "CKDAnswerId" = 20979;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='80', "Creatinine"='3.0',"Acr"='< 30', "NormalityIndicator"='N'
WHERE "CKDAnswerId" = 20980;

UPDATE public."LookupCKDAnswer"
SET "Albumin"='150', "Creatinine"='3.0',"Acr"='30-300', "NormalityIndicator"='A'
WHERE "CKDAnswerId" = 20981;

ALTER TABLE "LookupCKDAnswer"
ALTER COLUMN "Albumin" SET NOT NULL;

ALTER TABLE "LookupCKDAnswer"
ALTER COLUMN "Creatinine" SET NOT NULL;

ALTER TABLE "LookupCKDAnswer"
ALTER COLUMN "NormalityIndicator" SET NOT NULL;
