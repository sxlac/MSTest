DELETE FROM public."NotPerformedReason";

ALTER SEQUENCE "NotPerformedReason_NotPerformedReasonId_seq"
    RESTART WITH 1;

INSERT  INTO public."NotPerformedReason"
("AnswerId", "Reason")
VALUES  (52476, 'Scheduled to complete'),
        (52477, 'Member apprehension'),
        (52478, 'Not interested'),
        (52472, 'Technical issue'),
        (52473, 'Environmental issue'),
        (52474, 'No supplies or equipment'),
        (52475, 'Insufficient training');   