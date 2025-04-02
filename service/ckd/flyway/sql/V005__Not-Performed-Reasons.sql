-- All reasons why an evaluation with a CKD product may not be performed
CREATE TABLE public."NotPerformedReason"
(
    "NotPerformedReasonId" SMALLSERIAL PRIMARY KEY NOT NULL,
    "AnswerId" INTEGER NOT NULL UNIQUE,
    "Reason" VARCHAR(256) NOT NULL UNIQUE
);

INSERT  INTO public."NotPerformedReason"
        ("AnswerId", "Reason")
VALUES  (30863, 'Member recently completed'),
        (30864, 'Scheduled to complete'),
        (30865, 'Member apprehension'),
        (30866, 'Not interested'),
        (30867, 'Other'),
        (30870, 'Technical issue'),
		(30871, 'Environmental issue'),
		(30872, 'No supplies or equipment'),
		(30873, 'Insufficient training'),
		(30899, 'Member physically unable');

-- Details about evaluations where a CKD exam was not performed
CREATE TABLE public."ExamNotPerformed"
(
    "ExamNotPerformedId" SERIAL PRIMARY KEY NOT NULL,
    "CKDId" INTEGER NOT NULL UNIQUE REFERENCES "CKD" ("CKDId"),
    "NotPerformedReasonId" SMALLINT NOT NULL REFERENCES "NotPerformedReason" ("NotPerformedReasonId"),
    "CreatedDateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
