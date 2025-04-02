UPDATE  public."NotPerformedReason"
   SET  "AnswerId" = 50899 -- Previously was 30899 from V005
 WHERE  "Reason" = 'Member physically unable';

CREATE INDEX IDX_CKDStatus_CKDId ON public."CKDStatus" ("CKDId");
CREATE INDEX IDX_CKDStatus_CKDStatusCodeId ON public."CKDStatus" ("CKDStatusCodeId");
CREATE INDEX IDX_ExamNotPerformed_NotPerformedReasonId ON public."ExamNotPerformed" ("NotPerformedReasonId");
