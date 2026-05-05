-- Propositions QCM (quiz / publication web) : pas d'étudiant ni de session d'examen.
-- Sans ceci, INSERT des options échoue avec "Column 'etudiant_id' cannot be null".
ALTER TABLE reponse MODIFY COLUMN etudiant_id BIGINT NULL;
ALTER TABLE reponse MODIFY COLUMN session_examen_id BIGINT NULL;
