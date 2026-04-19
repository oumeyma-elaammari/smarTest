-- =====================================================
-- DATABASE : smartest_db
-- =====================================================
DROP DATABASE IF EXISTS smartest_db;
CREATE DATABASE smartest_db;
USE smartest_db;

-- =====================================================
-- TABLE : professeur
-- =====================================================
CREATE TABLE professeur (
                            id                          BIGINT PRIMARY KEY AUTO_INCREMENT,
                            nom                         VARCHAR(100)  NOT NULL,
                            email                       VARCHAR(150)  UNIQUE NOT NULL,
                            password                    VARCHAR(255)  NOT NULL,
                            email_verifie               BOOLEAN       NOT NULL DEFAULT FALSE,
                            token_verification          VARCHAR(255)  UNIQUE,
                            token_verification_expiry   DATETIME,
                            reset_password_token        VARCHAR(255)  UNIQUE,
                            reset_password_expiry       DATETIME
);

-- =====================================================
-- TABLE : etudiant
-- =====================================================
CREATE TABLE etudiant (
                          id                    BIGINT PRIMARY KEY AUTO_INCREMENT,
                          nom                   VARCHAR(100)  NOT NULL,
                          email                 VARCHAR(150)  UNIQUE NOT NULL,
                          password              VARCHAR(255)  NOT NULL,
                          email_verifie         BOOLEAN       NOT NULL DEFAULT FALSE,
                          token_verification    VARCHAR(255)  UNIQUE,
                          reset_password_token  VARCHAR(255)  UNIQUE,
                          reset_password_expiry DATETIME
);

-- =====================================================
-- TABLE : cours
-- =====================================================
CREATE TABLE cours (
                       id            BIGINT PRIMARY KEY AUTO_INCREMENT,
                       titre         VARCHAR(200) NOT NULL,
                       contenu       TEXT,
                       professeur_id BIGINT       NOT NULL,
                       FOREIGN KEY (professeur_id) REFERENCES professeur(id)
);

-- =====================================================
-- TABLE : question
-- =====================================================
CREATE TABLE question (
                          id            BIGINT PRIMARY KEY AUTO_INCREMENT,
                          enonce        TEXT         NOT NULL,
                          type          ENUM('QCM', 'VRAI_FAUX', 'OUVERTE'),
                          difficulte    ENUM('FACILE', 'MOYEN', 'DIFFICILE'),
                          professeur_id BIGINT       NOT NULL,
                          cours_id      BIGINT,
                          FOREIGN KEY (professeur_id) REFERENCES professeur(id),
                          FOREIGN KEY (cours_id)      REFERENCES cours(id)
);

-- =====================================================
-- TABLE : quiz
-- =====================================================
CREATE TABLE quiz (
                      id            BIGINT PRIMARY KEY AUTO_INCREMENT,
                      titre         VARCHAR(200) NOT NULL,
                      duree         INT,
                      professeur_id BIGINT       NOT NULL,
                      cours_id      BIGINT,
                      FOREIGN KEY (professeur_id) REFERENCES professeur(id),
                      FOREIGN KEY (cours_id)      REFERENCES cours(id)
);

-- =====================================================
-- TABLE : quiz_question
-- =====================================================
CREATE TABLE quiz_question (
                               quiz_id     BIGINT NOT NULL,
                               question_id BIGINT NOT NULL,
                               ordre       INT    DEFAULT 0,
                               PRIMARY KEY (quiz_id, question_id),
                               FOREIGN KEY (quiz_id)     REFERENCES quiz(id),
                               FOREIGN KEY (question_id) REFERENCES question(id)
);

-- =====================================================
-- TABLE : examen
-- =====================================================
CREATE TABLE examen (
                        id            BIGINT PRIMARY KEY AUTO_INCREMENT,
                        titre         VARCHAR(200) NOT NULL,
                        duree         INT,
                        professeur_id BIGINT       NOT NULL,
                        cours_id      BIGINT,
                        FOREIGN KEY (professeur_id) REFERENCES professeur(id),
                        FOREIGN KEY (cours_id)      REFERENCES cours(id)
);

-- =====================================================
-- TABLE : examen_question
-- =====================================================
CREATE TABLE examen_question (
                                 examen_id   BIGINT NOT NULL,
                                 question_id BIGINT NOT NULL,
                                 ordre       INT    DEFAULT 0,
                                 PRIMARY KEY (examen_id, question_id),
                                 FOREIGN KEY (examen_id)   REFERENCES examen(id),
                                 FOREIGN KEY (question_id) REFERENCES question(id)
);

-- =====================================================
-- TABLE : session_examen
-- =====================================================
CREATE TABLE session_examen (
                                id          BIGINT PRIMARY KEY AUTO_INCREMENT,
                                date_debut  DATETIME,
                                date_fin    DATETIME,
                                statut      ENUM('EN_ATTENTE', 'EN_COURS', 'TERMINEE'),
                                qr_code     VARCHAR(255),
                                examen_id   BIGINT NOT NULL,
                                etudiant_id BIGINT NOT NULL,      -- ← étudiant qui passe l'examen
                                FOREIGN KEY (examen_id)   REFERENCES examen(id),
                                FOREIGN KEY (etudiant_id) REFERENCES etudiant(id)
);

-- =====================================================
-- TABLE : reponse
-- =====================================================
CREATE TABLE reponse (
                         id                BIGINT PRIMARY KEY AUTO_INCREMENT,
                         contenu           TEXT,
                         correcte          BOOLEAN,
                         etudiant_id       BIGINT NOT NULL,   -- ← etudiant au lieu de utilisateur
                         session_examen_id BIGINT NOT NULL,
                         question_id       BIGINT NOT NULL,
                         FOREIGN KEY (etudiant_id)       REFERENCES etudiant(id),
                         FOREIGN KEY (session_examen_id) REFERENCES session_examen(id),
                         FOREIGN KEY (question_id)       REFERENCES question(id)
);

-- =====================================================
-- TABLE : resultat
-- =====================================================
CREATE TABLE resultat (
                          id                BIGINT PRIMARY KEY AUTO_INCREMENT,
                          note              FLOAT,
                          score             FLOAT,
                          etudiant_id       BIGINT NOT NULL,   -- ← etudiant au lieu de utilisateur
                          session_examen_id BIGINT NOT NULL,
                          FOREIGN KEY (etudiant_id)       REFERENCES etudiant(id),
                          FOREIGN KEY (session_examen_id) REFERENCES session_examen(id)
);

-- =====================================================
SHOW TABLES;