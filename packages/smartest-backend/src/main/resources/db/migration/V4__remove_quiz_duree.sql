-- Suppression durée quiz : le champ n'est plus utilisé (passage web / QR sans timer serveur lié à cette colonne).
ALTER TABLE quiz DROP COLUMN duree;
