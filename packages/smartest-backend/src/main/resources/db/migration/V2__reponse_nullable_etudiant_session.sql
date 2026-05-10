-- Schéma SmarTest si base vide (CREATE IF NOT EXISTS), puis assouplissement des FK sur `reponse`.
-- Propositions QCM (quiz / publication web) : pas d'étudiant ni de session d'examen.
-- Sans les ALTER, INSERT des options échoue avec "Column 'etudiant_id' cannot be null".
-- (Contenu historique V2__sprint2.sql : ALTER quiz/resultat + CREATE examen_publie — déjà couverts par ce schéma.)

CREATE TABLE IF NOT EXISTS professeur (
    id BIGINT NOT NULL AUTO_INCREMENT,
    nom VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL,
    password VARCHAR(255) NOT NULL,
    email_verifie BIT(1) NOT NULL,
    token_verification VARCHAR(255),
    reset_password_token VARCHAR(255),
    reset_password_expiry DATETIME(6),
    token_verification_expiry DATETIME(6),
    PRIMARY KEY (id),
    UNIQUE KEY uk_professeur_email (email),
    UNIQUE KEY uk_professeur_token_verification (token_verification),
    UNIQUE KEY uk_professeur_reset_password_token (reset_password_token)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS etudiant (
    id BIGINT NOT NULL AUTO_INCREMENT,
    nom VARCHAR(100),
    email VARCHAR(150),
    password VARCHAR(255) NOT NULL,
    email_verifie BIT(1) NOT NULL,
    token_verification VARCHAR(255),
    reset_password_token VARCHAR(255),
    reset_password_expiry DATETIME(6),
    PRIMARY KEY (id),
    UNIQUE KEY uk_etudiant_email (email),
    UNIQUE KEY uk_etudiant_token_verification (token_verification),
    UNIQUE KEY uk_etudiant_reset_password_token (reset_password_token)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS examen_publie (
    id BIGINT NOT NULL AUTO_INCREMENT,
    titre VARCHAR(255),
    duree INT,
    description TEXT,
    professeur_id BIGINT,
    statut VARCHAR(255),
    date_debut DATETIME(6),
    date_fin DATETIME(6),
    date_creation DATETIME(6),
    PRIMARY KEY (id),
    CONSTRAINT fk_examen_publie_professeur FOREIGN KEY (professeur_id) REFERENCES professeur (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS session_examen (
    id BIGINT NOT NULL AUTO_INCREMENT,
    date_debut DATETIME(6),
    date_fin DATETIME(6),
    statut VARCHAR(255),
    examen_publie_id BIGINT,
    PRIMARY KEY (id),
    CONSTRAINT fk_session_examen_examen FOREIGN KEY (examen_publie_id) REFERENCES examen_publie (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS quiz (
    id BIGINT NOT NULL AUTO_INCREMENT,
    titre VARCHAR(255),
    statut VARCHAR(255),
    date_publication DATETIME(6),
    professeur_id BIGINT,
    duree INT,
    PRIMARY KEY (id),
    CONSTRAINT fk_quiz_professeur FOREIGN KEY (professeur_id) REFERENCES professeur (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS question (
    id BIGINT NOT NULL AUTO_INCREMENT,
    enonce TEXT NOT NULL,
    type VARCHAR(255),
    difficulte VARCHAR(255),
    explication TEXT,
    professeur_id BIGINT NOT NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_question_professeur FOREIGN KEY (professeur_id) REFERENCES professeur (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS quiz_question (
    quiz_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    PRIMARY KEY (quiz_id, question_id),
    CONSTRAINT fk_qq_quiz FOREIGN KEY (quiz_id) REFERENCES quiz (id),
    CONSTRAINT fk_qq_question FOREIGN KEY (question_id) REFERENCES question (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS examen_question (
    examen_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    PRIMARY KEY (examen_id, question_id),
    CONSTRAINT fk_eq_examen FOREIGN KEY (examen_id) REFERENCES examen_publie (id),
    CONSTRAINT fk_eq_question FOREIGN KEY (question_id) REFERENCES question (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS quiz_email_web_autorise (
    quiz_id BIGINT NOT NULL,
    email VARCHAR(320) NOT NULL,
    PRIMARY KEY (quiz_id, email),
    CONSTRAINT fk_qewa_quiz FOREIGN KEY (quiz_id) REFERENCES quiz (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS reponse (
    id BIGINT NOT NULL AUTO_INCREMENT,
    contenu TEXT,
    correcte BIT(1),
    etudiant_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    session_examen_id BIGINT NOT NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_reponse_etudiant FOREIGN KEY (etudiant_id) REFERENCES etudiant (id),
    CONSTRAINT fk_reponse_question FOREIGN KEY (question_id) REFERENCES question (id),
    CONSTRAINT fk_reponse_session FOREIGN KEY (session_examen_id) REFERENCES session_examen (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS resultat (
    id BIGINT NOT NULL AUTO_INCREMENT,
    question_id BIGINT NOT NULL,
    reponse_id BIGINT NOT NULL,
    correcte BIT(1),
    session_examen_id BIGINT NOT NULL,
    etudiant_id BIGINT NOT NULL,
    score DOUBLE,
    note DOUBLE,
    date_passage DATETIME(6),
    est_premiere_tentative BIT(1),
    quiz_id BIGINT,
    PRIMARY KEY (id),
    CONSTRAINT fk_resultat_question FOREIGN KEY (question_id) REFERENCES question (id),
    CONSTRAINT fk_resultat_reponse FOREIGN KEY (reponse_id) REFERENCES reponse (id),
    CONSTRAINT fk_resultat_session FOREIGN KEY (session_examen_id) REFERENCES session_examen (id),
    CONSTRAINT fk_resultat_etudiant FOREIGN KEY (etudiant_id) REFERENCES etudiant (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS reponse_etudiant (
    id BIGINT NOT NULL AUTO_INCREMENT,
    reponse_texte TEXT,
    est_correcte BIT(1),
    a_recu_correction_immediate BIT(1),
    bonne_reponse_affichee TEXT,
    date_soumission DATETIME(6),
    etudiant_id BIGINT,
    question_id BIGINT,
    resultat_id BIGINT,
    session_examen_id BIGINT,
    PRIMARY KEY (id),
    CONSTRAINT fk_re_etudiant FOREIGN KEY (etudiant_id) REFERENCES etudiant (id),
    CONSTRAINT fk_re_question FOREIGN KEY (question_id) REFERENCES question (id),
    CONSTRAINT fk_re_resultat FOREIGN KEY (resultat_id) REFERENCES resultat (id),
    CONSTRAINT fk_re_session FOREIGN KEY (session_examen_id) REFERENCES session_examen (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS statistique_question (
    id BIGINT NOT NULL AUTO_INCREMENT,
    question_id BIGINT,
    quiz_id BIGINT,
    examen_Publie_id BIGINT,
    session_examen_id BIGINT,
    nombre_reponses INT,
    nombre_correctes INT,
    nombre_incorrectes INT,
    pourcentage_reussite DOUBLE,
    pourcentage_echec DOUBLE,
    a_genere_alerte BIT(1),
    alerte_echec BIT(1),
    dernier_calcul DATETIME(6),
    PRIMARY KEY (id),
    CONSTRAINT fk_sq_question FOREIGN KEY (question_id) REFERENCES question (id),
    CONSTRAINT fk_sq_quiz FOREIGN KEY (quiz_id) REFERENCES quiz (id),
    CONSTRAINT fk_sq_examen FOREIGN KEY (examen_Publie_id) REFERENCES examen_publie (id),
    CONSTRAINT fk_sq_session FOREIGN KEY (session_examen_id) REFERENCES session_examen (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

ALTER TABLE reponse MODIFY COLUMN etudiant_id BIGINT NULL;
ALTER TABLE reponse MODIFY COLUMN session_examen_id BIGINT NULL;
