CREATE TABLE public."OccurrenceFrequency"
(
    "OccurrenceFrequencyId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "Frequency" VARCHAR(128) NOT NULL UNIQUE
);

ALTER TABLE public."OccurrenceFrequency" OWNER TO flywayspirometry;
GRANT SELECT ON public."OccurrenceFrequency" TO svcspirometry;

INSERT INTO public."OccurrenceFrequency"
        ("Frequency")
VALUES  ('Never'),
        ('Rarely'),
        ('Sometimes'),
        ('Often'),
        ('Very often');

--

-- QID 90: "Have you ever smoked tobacco?"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "HasSmokedTobacco" BOOLEAN NULL;

-- QID 90: "Total years smoking?"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "TotalYearsSmoking" INTEGER NULL;

-- QID 221: "Do you produce sputum with your cough?"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "ProducesSputumWithCough" BOOLEAN NULL;

-- QID 100402: "How often do you cough up mucus?"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "CoughMucusOccurrenceFrequencyId" SMALLINT NULL REFERENCES "OccurrenceFrequency" ("OccurrenceFrequencyId");
CREATE INDEX IDX_SpirometryExamResults_CoughMucusOccurrenceFrequencyId ON public."SpirometryExamResults" ("CoughMucusOccurrenceFrequencyId");

-- QID 87: "Have you had wheezing in the past 12 months?"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "HadWheezingPast12moTrileanTypeId" SMALLINT NULL REFERENCES "TrileanType" ("TrileanTypeId");
CREATE INDEX IDX_SpirometryExamResults_HadWheezingPast12moTrileanTypeId ON public."SpirometryExamResults" ("HadWheezingPast12moTrileanTypeId");

-- QID 97: "Do you get short of breath at rest?"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "GetsShortnessOfBreathAtRestTrileanTypeId" SMALLINT NULL REFERENCES "TrileanType" ("TrileanTypeId");
CREATE INDEX IDX_SpirometryExamResults_GetsShortnessOfBreathAtRestTrileanTypeId ON public."SpirometryExamResults" ("GetsShortnessOfBreathAtRestTrileanTypeId");

-- QID 98: "Do you get short of breath with mild exertion"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "GetsShortnessOfBreathWithMildExertionTrileanTypeId" SMALLINT NULL REFERENCES "TrileanType" ("TrileanTypeId");
CREATE INDEX IDX_SpirometryExamResults_GetsShortnessOfBreathWithMildExertionTrileanTypeId ON public."SpirometryExamResults" ("GetsShortnessOfBreathWithMildExertionTrileanTypeId");

-- QID 100403: "How often does your chest sound noisy (wheezy, whistling, rattling) when you breathe?"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "NoisyChestOccurrenceFrequencyId" SMALLINT NULL REFERENCES "OccurrenceFrequency" ("OccurrenceFrequencyId");
CREATE INDEX IDX_SpirometryExamResults_NoisyChestOccurrenceFrequencyId ON public."SpirometryExamResults" ("NoisyChestOccurrenceFrequencyId");

-- QID 100404: "How often do you experience shortness of breath during physical activity (walking up a flight of stairs or walking up an incline without stopping to rest)?"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId" SMALLINT NULL REFERENCES "OccurrenceFrequency" ("OccurrenceFrequencyId");
CREATE INDEX IDX_SpirometryExamResults_ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId ON public."SpirometryExamResults" ("ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId");

-- QID 100405: "Lung function questionnaire score"
ALTER TABLE public."SpirometryExamResults" ADD COLUMN "LungFunctionScore" INTEGER NULL;
