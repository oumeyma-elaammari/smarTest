-- Table dédiée aux questions d'un examen publié web (évite l'ambiguïté avec d'éventuelles tables legacy "examen_question").
CREATE TABLE IF NOT EXISTS examen_publie_question (
    examen_publie_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    PRIMARY KEY (examen_publie_id, question_id),
    CONSTRAINT fk_epq_examen_publie FOREIGN KEY (examen_publie_id) REFERENCES examen_publie (id) ON DELETE CASCADE,
    CONSTRAINT fk_epq_question FOREIGN KEY (question_id) REFERENCES question (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
