CREATE TABLE public."LoopbackEnabledProvider"
(
    "ProviderId" INTEGER PRIMARY KEY NOT NULL
);

ALTER TABLE public."LoopbackEnabledProvider" OWNER TO flywayspirometry;
GRANT SELECT, INSERT, DELETE, TRUNCATE ON public."LoopbackEnabledProvider" TO svcspirometry;
