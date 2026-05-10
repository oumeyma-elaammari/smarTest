-- Idempotent sans ADD COLUMN IF NOT EXISTS (non reconnu selon version MySQL / moteur).
SET @mdb := DATABASE();
SET @exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @mdb
      AND TABLE_NAME = 'question'
      AND COLUMN_NAME = 'duree_secondes_indicative'
);
SET @ddl := IF(@exists = 0,
    'ALTER TABLE question ADD COLUMN duree_secondes_indicative INT NULL',
    'SELECT 1');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
