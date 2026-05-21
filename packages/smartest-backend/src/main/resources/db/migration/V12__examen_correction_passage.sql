-- Corrections détaillées examen web (persistant) + résultat par étudiant (validation prof, visibilité note).

CREATE TABLE IF NOT EXISTS examen_passage_resultat (
    id BIGINT NOT NULL AUTO_INCREMENT,
    examen_publie_id BIGINT NOT NULL,
    etudiant_id BIGINT NOT NULL,
    note_proposee DOUBLE NOT NULL DEFAULT 0,
    note_finale DOUBLE NULL,
    bareme_reference DOUBLE NOT NULL DEFAULT 20,
    remarque TEXT NULL,
    validee_par_prof BIT(1) NOT NULL DEFAULT 0,
    date_validation DATETIME(6) NULL,
    note_visible_etudiant BIT(1) NOT NULL DEFAULT 0,
    date_publication_note_etudiant DATETIME(6) NULL,
    date_synchronisation_workbench DATETIME(6) NULL,
    date_creation DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    date_mise_a_jour DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_epr_examen_etudiant (examen_publie_id, etudiant_id),
    CONSTRAINT fk_epr_examen FOREIGN KEY (examen_publie_id) REFERENCES examen_publie (id) ON DELETE CASCADE,
    CONSTRAINT fk_epr_etudiant FOREIGN KEY (etudiant_id) REFERENCES etudiant (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS examen_correction_ligne (
    id BIGINT NOT NULL AUTO_INCREMENT,
    examen_publie_id BIGINT NOT NULL,
    etudiant_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    reponse_json TEXT NOT NULL,
    score_partiel DOUBLE NULL,
    note_finale_ligne DOUBLE NULL,
    corrige_par_ia BIT(1) NOT NULL DEFAULT 0,
    groq_statut VARCHAR(20) NULL,
    date_correction DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (id),
    UNIQUE KEY uk_ecl_examen_etu_q (examen_publie_id, etudiant_id, question_id),
    CONSTRAINT fk_ecl_examen FOREIGN KEY (examen_publie_id) REFERENCES examen_publie (id) ON DELETE CASCADE,
    CONSTRAINT fk_ecl_etudiant FOREIGN KEY (etudiant_id) REFERENCES etudiant (id) ON DELETE CASCADE,
    CONSTRAINT fk_ecl_question FOREIGN KEY (question_id) REFERENCES question (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
