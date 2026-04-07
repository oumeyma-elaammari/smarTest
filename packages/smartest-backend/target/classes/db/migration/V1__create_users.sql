CREATE TABLE users (
                       id         BIGINT AUTO_INCREMENT PRIMARY KEY,
                       email      VARCHAR(255) NOT NULL UNIQUE,
                       password   VARCHAR(255) NOT NULL,
                       first_name VARCHAR(100) NOT NULL,
                       last_name  VARCHAR(100) NOT NULL,
                       role       ENUM('PROFESSOR','STUDENT') NOT NULL,
                       created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);