INSERT INTO public."StatusCode"
        ("Name")
VALUES  ('Clarification Flag Created');

CREATE TABLE public."ClarificationFlag"
(
    "ClarificationFlagId" SERIAL PRIMARY KEY NOT NULL,
    "SpirometryExamId" INTEGER NOT NULL UNIQUE REFERENCES "SpirometryExam" ("SpirometryExamId"),
    "CdiFlagId" INTEGER NOT NULL UNIQUE,
    "CreateDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

ALTER TABLE public."ClarificationFlag" OWNER TO flywayspirometry;
GRANT SELECT, INSERT ON public."ClarificationFlag" TO svcspirometry;

CREATE INDEX IDX_ClarificationFlag_SpirometryExamId ON public."ClarificationFlag" ("SpirometryExamId");

GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO svcspirometry;
