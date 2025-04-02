CREATE TABLE public."EvaluationObjective"
(
    "EvaluationObjectiveId" SMALLINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY NOT NULL,
    "Objective" VARCHAR(128) UNIQUE NOT NULL
);

INSERT INTO public."EvaluationObjective"
("Objective")
VALUES
('Comprehensive'),
('Focused');

ALTER TABLE public."Exam"
ADD COLUMN "EvaluationObjectiveId" SMALLINT NOT NULL DEFAULT 1,
ADD CONSTRAINT FK_Exam_EvaluationObjective
FOREIGN KEY ("EvaluationObjectiveId")
REFERENCES public."EvaluationObjective"("EvaluationObjectiveId");

GRANT SELECT ON public."EvaluationObjective" TO svcdee;

ALTER TABLE public."Exam"
ALTER COLUMN "EvaluationObjectiveId" DROP DEFAULT; -- Require DEE to explicitly set it when inserting new exams