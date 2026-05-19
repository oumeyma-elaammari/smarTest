-- Image de question (publication web examen / quiz).

SET @mdb := DATABASE();

SET @exists_b64 := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @mdb AND TABLE_NAME = 'question' AND COLUMN_NAME = 'image_base64'
);
SET @ddl_b64 := IF(@exists_b64 = 0,
    'ALTER TABLE question ADD COLUMN image_base64 LONGTEXT NULL',
    'SELECT 1');
PREPARE stmt_b64 FROM @ddl_b64;
EXECUTE stmt_b64;
DEALLOCATE PREPARE stmt_b64;

SET @exists_type := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @mdb AND TABLE_NAME = 'question' AND COLUMN_NAME = 'image_type'
);
SET @ddl_type := IF(@exists_type = 0,
    'ALTER TABLE question ADD COLUMN image_type VARCHAR(32) NULL',
    'SELECT 1');
PREPARE stmt_type FROM @ddl_type;
EXECUTE stmt_type;
DEALLOCATE PREPARE stmt_type;
