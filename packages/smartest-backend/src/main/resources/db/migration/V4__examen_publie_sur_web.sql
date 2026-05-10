-- Examens visibles sur le web étudiant uniquement après publication explicite (bureau).
ALTER TABLE examen_publie ADD COLUMN publie_sur_web_le DATETIME(6) NULL;

-- Anciens examens déjà associés à au moins un email web : considérés comme publiés.
UPDATE examen_publie e
SET publie_sur_web_le = COALESCE(e.date_creation, CURRENT_TIMESTAMP(6))
WHERE EXISTS (
    SELECT 1 FROM examen_email_web_autorise a WHERE a.examen_id = e.id
);
