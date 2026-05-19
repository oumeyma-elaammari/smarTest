-- Barème par question (défini à la création / publication de l'examen depuis le bureau).
SET @mdb := DATABASE();
SET @exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @mdb
      AND TABLE_NAME = 'question'
      AND COLUMN_NAME = 'bareme_points'
);
SET @ddl := IF(@exists = 0,
    'ALTER TABLE question ADD COLUMN bareme_points DOUBLE NULL',
    'SELECT 1');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
