-- Résultats de quiz (web / hors session d'examen supervisé) : pas de session_examen.
ALTER TABLE resultat MODIFY COLUMN session_examen_id BIGINT NULL;
