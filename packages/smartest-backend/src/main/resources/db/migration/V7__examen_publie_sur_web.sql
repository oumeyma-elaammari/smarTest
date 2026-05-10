-- Table emails web (voir ExamenPublie.emailsAutorisesWeb) : Flyway s'exécute avant Hibernate,
-- il faut donc la créer ici si la base n'a jamais tourné avec ddl-auto seul.
CREATE TABLE IF NOT EXISTS examen_email_web_autorise (
    examen_id BIGINT NOT NULL,
    email VARCHAR(320) NOT NULL,
    PRIMARY KEY (examen_id, email),
    CONSTRAINT fk_eewa_examen_publie FOREIGN KEY (examen_id) REFERENCES examen_publie (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Examens visibles sur le web étudiant uniquement après publication explicite (bureau).
-- Compatible MySQL 5.7+ (pas de ADD COLUMN IF NOT EXISTS avant 8.0.12 selon configs).
SET @col_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'examen_publie'
    AND COLUMN_NAME = 'publie_sur_web_le'
);

SET @sql := IF(@col_exists = 0,
  'ALTER TABLE examen_publie ADD COLUMN publie_sur_web_le DATETIME(6) NULL',
  'SELECT 1');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Anciens examens déjà associés à au moins un email web : considérés comme publiés.
UPDATE examen_publie e
SET publie_sur_web_le = COALESCE(e.date_creation, CURRENT_TIMESTAMP(6))
WHERE EXISTS (
    SELECT 1 FROM examen_email_web_autorise a WHERE a.examen_id = e.id
);
