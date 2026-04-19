ALTER TABLE resultat
    ADD COLUMN score DOUBLE,
ADD COLUMN note DOUBLE,
ADD COLUMN date_passage DATETIME,
ADD COLUMN est_premiere_tentative BOOLEAN,
ADD COLUMN quiz_id BIGINT;

ALTER TABLE quiz
    ADD COLUMN statut VARCHAR(20),
ADD COLUMN date_publication DATETIME;

CREATE TABLE examen_publie (
                               id BIGINT AUTO_INCREMENT PRIMARY KEY,
                               titre VARCHAR(255),
                               duree INT,
                               description TEXT,
                               professeur_id BIGINT,
                               statut VARCHAR(20),
                               date_debut DATETIME,
                               date_fin DATETIME,
                               date_creation DATETIME
);