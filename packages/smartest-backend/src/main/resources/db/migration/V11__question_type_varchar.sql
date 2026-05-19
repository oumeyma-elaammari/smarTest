-- Alignement avec JPA @Enumerated(EnumType.STRING) : valeurs possibles incluent
-- QCM, VRAI_FAUX, CASES_A_COCHER, REDACTION, REPONSE_COURTE.
-- Les schémas legacy (ENUM étroit ex. QCM/VRAI_FAUX/OUVERTE) provoquent :
-- "Data truncated for column 'type' at row 1" lors de la publication web d'examens.
ALTER TABLE question MODIFY COLUMN type VARCHAR(255) NULL;
