-- Table emails web (voir ExamenPublie.emailsAutorisesWeb) : Flyway s'exécute avant Hibernate,
-- il faut donc la créer ici si la base n'a jamais tourné avec ddl-auto seul.
-- Creation conditionnelle via INFORMATION_SCHEMA pour éviter le mot-clé EXISTS en DDL.
-- Important : les littéraux SQL dynamiques doivent éviter tout retour chariot (U+000D) dans la chaîne :
-- sous Windows (CRLF), un CREATE TABLE multi-lignes entre quotes provoque :
-- « An illegal character with code point 13 was found in this literal ».

SET @tbl_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'examen_email_web_autorise'
);

SET @sql := IF(@tbl_exists = 0, 'CREATE TABLE examen_email_web_autorise ( examen_id BIGINT NOT NULL, email VARCHAR(320) NOT NULL, PRIMARY KEY (examen_id, email), CONSTRAINT fk_eewa_examen_publie FOREIGN KEY (examen_id) REFERENCES examen_publie (id) ON DELETE CASCADE ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci', 'SELECT 1');

PREPARE stmt_create_eewa FROM @sql;
EXECUTE stmt_create_eewa;
DEALLOCATE PREPARE stmt_create_eewa;

-- Examens visibles sur le web étudiant uniquement après publication explicite (bureau).
-- Compatible MySQL 5.7+ (pas de ADD COLUMN IF NOT EXISTS avant 8.0.12 selon configs).
SET @col_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'examen_publie'
    AND COLUMN_NAME = 'publie_sur_web_le'
);

SET @sql := IF(@col_exists = 0, 'ALTER TABLE examen_publie ADD COLUMN publie_sur_web_le DATETIME(6) NULL', 'SELECT 1');

PREPARE stmt_pub_col FROM @sql;
EXECUTE stmt_pub_col;
DEALLOCATE PREPARE stmt_pub_col;

-- Anciens examens déjà associés à au moins un email web : considérés comme publiés.
UPDATE examen_publie e
INNER JOIN (SELECT DISTINCT examen_id FROM examen_email_web_autorise) a ON a.examen_id = e.id
SET e.publie_sur_web_le = COALESCE(e.date_creation, CURRENT_TIMESTAMP(6));
