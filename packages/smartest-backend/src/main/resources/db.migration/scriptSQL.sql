-- =====================================================
-- DATABASE : smartest_db
-- =====================================================
CREATE DATABASE IF NOT EXISTS smartest_db;
USE smartest_db;

-- =====================================================
-- TABLE : utilisateur (STUDENT)
-- =====================================================
CREATE TABLE IF NOT EXISTS utilisateur (
                                           id       BIGINT PRIMARY KEY AUTO_INCREMENT,
                                           nom      VARCHAR(100)                      NOT NULL,
    email    VARCHAR(150) UNIQUE               NOT NULL,
    password VARCHAR(255)                      NOT NULL,
    role     ENUM('PROFESSEUR', 'ETUDIANT')    NOT NULL
    );

-- =====================================================
-- TABLE : professeur
-- =====================================================
CREATE TABLE IF NOT EXISTS professeur (
                                          id       BIGINT PRIMARY KEY AUTO_INCREMENT,
                                          nom      VARCHAR(100)                      NOT NULL,
    email    VARCHAR(150) UNIQUE               NOT NULL,
    password VARCHAR(255)                      NOT NULL,
    role     ENUM('PROFESSEUR', 'ETUDIANT')    NOT NULL
    );

-- =====================================================
-- TABLE : cours
-- =====================================================
CREATE TABLE IF NOT EXISTS cours (
                                     id            BIGINT PRIMARY KEY AUTO_INCREMENT,
                                     titre         VARCHAR(200)    NOT NULL,
    contenu       TEXT,
    professeur_id BIGINT          NOT NULL,
    FOREIGN KEY (professeur_id) REFERENCES professeur(id)
    );

-- =====================================================
-- TABLE : question
-- =====================================================
CREATE TABLE IF NOT EXISTS question (
                                        id            BIGINT PRIMARY KEY AUTO_INCREMENT,
                                        enonce        TEXT            NOT NULL,
                                        type          ENUM('QCM', 'VRAI_FAUX', 'OUVERTE'),
    difficulte    ENUM('FACILE', 'MOYEN', 'DIFFICILE'),
    professeur_id BIGINT          NOT NULL,
    cours_id      BIGINT,
    FOREIGN KEY (professeur_id) REFERENCES professeur(id),
    FOREIGN KEY (cours_id)      REFERENCES cours(id)
    );

-- =====================================================
-- TABLE : quiz
-- =====================================================
CREATE TABLE IF NOT EXISTS quiz (
                                    id            BIGINT PRIMARY KEY AUTO_INCREMENT,
                                    titre         VARCHAR(200)    NOT NULL,
    duree         INT,
    professeur_id BIGINT          NOT NULL,
    cours_id      BIGINT,
    FOREIGN KEY (professeur_id) REFERENCES professeur(id),
    FOREIGN KEY (cours_id)      REFERENCES cours(id)
    );

-- =====================================================
-- TABLE : quiz_question
-- =====================================================
CREATE TABLE IF NOT EXISTS quiz_question (
                                             quiz_id     BIGINT  NOT NULL,
                                             question_id BIGINT  NOT NULL,
                                             ordre       INT     DEFAULT 0,
                                             PRIMARY KEY (quiz_id, question_id),
    FOREIGN KEY (quiz_id)     REFERENCES quiz(id),
    FOREIGN KEY (question_id) REFERENCES question(id)
    );

-- =====================================================
-- TABLE : examen
-- =====================================================
CREATE TABLE IF NOT EXISTS examen (
                                      id            BIGINT PRIMARY KEY AUTO_INCREMENT,
                                      titre         VARCHAR(200)    NOT NULL,
    duree         INT,
    professeur_id BIGINT          NOT NULL,
    cours_id      BIGINT,
    FOREIGN KEY (professeur_id) REFERENCES professeur(id),
    FOREIGN KEY (cours_id)      REFERENCES cours(id)
    );

-- =====================================================
-- TABLE : examen_question
-- =====================================================
CREATE TABLE IF NOT EXISTS examen_question (
                                               examen_id   BIGINT  NOT NULL,
                                               question_id BIGINT  NOT NULL,
                                               ordre       INT     DEFAULT 0,
                                               PRIMARY KEY (examen_id, question_id),
    FOREIGN KEY (examen_id)   REFERENCES examen(id),
    FOREIGN KEY (question_id) REFERENCES question(id)
    );

-- =====================================================
-- TABLE : session_examen
-- =====================================================
CREATE TABLE IF NOT EXISTS session_examen (
                                              id         BIGINT PRIMARY KEY AUTO_INCREMENT,
                                              date_debut DATETIME,
                                              date_fin   DATETIME,
                                              statut     ENUM('EN_ATTENTE', 'EN_COURS', 'TERMINEE'),
    qr_code    VARCHAR(255),
    examen_id  BIGINT  NOT NULL,
    FOREIGN KEY (examen_id) REFERENCES examen(id)
    );

-- =====================================================
-- TABLE : reponse
-- =====================================================
CREATE TABLE IF NOT EXISTS reponse (
                                       id               BIGINT PRIMARY KEY AUTO_INCREMENT,
                                       contenu          TEXT,
                                       correcte         BOOLEAN,
                                       utilisateur_id   BIGINT  NOT NULL,
                                       session_examen_id BIGINT NOT NULL,
                                       question_id      BIGINT  NOT NULL,
                                       FOREIGN KEY (utilisateur_id)    REFERENCES utilisateur(id),
    FOREIGN KEY (session_examen_id) REFERENCES session_examen(id),
    FOREIGN KEY (question_id)       REFERENCES question(id)
    );

-- =====================================================
-- TABLE : resultat
-- =====================================================
CREATE TABLE IF NOT EXISTS resultat (
                                        id                BIGINT PRIMARY KEY AUTO_INCREMENT,
                                        note              FLOAT,
                                        score             FLOAT,
                                        utilisateur_id    BIGINT  NOT NULL,
                                        session_examen_id BIGINT  NOT NULL,
                                        FOREIGN KEY (utilisateur_id)    REFERENCES utilisateur(id),
    FOREIGN KEY (session_examen_id) REFERENCES session_examen(id)
    );

-- =====================================================
SHOW TABLES;