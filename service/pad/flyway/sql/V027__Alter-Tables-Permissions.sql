GRANT SELECT ON public."LateralityCode" TO svcpad;
GRANT SELECT ON public."PedalPulseCode" TO svcpad;
GRANT SELECT, INSERT ON public."AoeSymptomSupportResult" TO svcpad;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'postgresql_prd_pad_ro') THEN
        GRANT SELECT ON public."LateralityCode" TO postgresql_prd_pad_ro;
        GRANT SELECT ON public."PedalPulseCode" TO postgresql_prd_pad_ro;
        GRANT SELECT ON public."AoeSymptomSupportResult" TO postgresql_prd_pad_ro;
    END IF;
END $$;