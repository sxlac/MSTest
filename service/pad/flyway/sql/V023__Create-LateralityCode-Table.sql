CREATE TABLE public."LateralityCode"
(
    "LateralityCodeId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "Laterality" VARCHAR(8) UNIQUE NOT NULL
);

INSERT  INTO public."LateralityCode"
        ("Laterality")
VALUES  ('Neither'),
        ('Left'),
        ('Right'),
        ('Both');

CREATE TABLE public."PedalPulseCode"
(
    "PedalPulseCodeId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "PedalPulse" VARCHAR(20) UNIQUE NOT NULL
);

INSERT  INTO public."PedalPulseCode"
        ("PedalPulse")
VALUES  ('Normal'),
        ('Abnormal-Left'),
        ('Abnormal-Right'),
        ('Abnormal-Bilateral'),
        ('Not Performed');

CREATE TABLE public."AoeSymptomSupportResult"
(
    "AoeSymptomSupportResultId" SERIAL PRIMARY KEY NOT NULL,
    "PADId" INTEGER NOT NULL REFERENCES "PAD" ("PADId"),
    "FootPainRestingElevatedLateralityCodeId" SMALLINT NOT NULL REFERENCES "LateralityCode" ("LateralityCodeId"),
    "FootPainDisappearsWalkingOrDangling" BOOLEAN NOT NULL,
    "FootPainDisappearsOtc" BOOLEAN NOT NULL,
    "PedalPulseCodeId" SMALLINT NOT NULL REFERENCES "PedalPulseCode" ("PedalPulseCodeId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);