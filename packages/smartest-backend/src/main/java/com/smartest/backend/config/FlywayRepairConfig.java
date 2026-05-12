package com.smartest.backend.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.flyway.autoconfigure.FlywayMigrationStrategy;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class FlywayRepairConfig {

    /**
     * Si une migration a échoué ({@code success=0}) ou si un script déjà appliqué a été
     * modifié (erreur « checksum mismatch »), {@code flyway.repair()} met à jour l’historique
     * et les checksums. Activer via {@code app.flyway.repair-before-migrate=true} le temps
     * d’un démarrage réussi, puis repasser à {@code false}.
     */
    @Bean
    public FlywayMigrationStrategy flywayMigrationStrategy(
            @Value("${app.flyway.repair-before-migrate:false}") boolean repairBeforeMigrate) {
        return flyway -> {
            if (repairBeforeMigrate) {
                flyway.repair();
            }
            flyway.migrate();
        };
    }
}
