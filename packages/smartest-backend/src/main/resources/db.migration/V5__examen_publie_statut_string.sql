-- Hibernate mappe StatutExamen en chaînes (PLANIFIE, EN_COURS, EN_PAUSE, TERMINE, ANNULE).
-- Les anciennes bases avaient souvent un ENUM ou VARCHAR trop court → "Data truncated for column 'statut'".
ALTER TABLE examen_publie
    MODIFY COLUMN statut VARCHAR(32) NULL;
