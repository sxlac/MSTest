ALTER TABLE public."SpirometryExamResults" ALTER COLUMN "SessionGradeId" DROP NOT NULL;
ALTER TABLE public."SpirometryExamResults" ALTER COLUMN "FVC" DROP NOT NULL;
ALTER TABLE public."SpirometryExamResults" ALTER COLUMN "FEV1" DROP NOT NULL;
ALTER TABLE public."SpirometryExamResults" ALTER COLUMN "FEV1_Over_FVC" DROP NOT NULL;
ALTER TABLE public."SpirometryExamResults" ALTER COLUMN "HasHighSymptomTrileanTypeId" DROP NOT NULL;
ALTER TABLE public."SpirometryExamResults" ALTER COLUMN "HasEnvOrExpRiskTrileanTypeId" DROP NOT NULL;
ALTER TABLE public."SpirometryExamResults" ALTER COLUMN "HasHighComorbidityTrileanTypeId" DROP NOT NULL;
