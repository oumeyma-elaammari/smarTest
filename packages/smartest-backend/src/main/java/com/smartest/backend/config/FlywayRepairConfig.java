package com.smartest.backend.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.flyway.autoconfigure.FlywayMigrationStrategy;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class FlywayRepairConfig {

    /**
     * Si une migration a échoué une fois (ex. V7), Flyway bloque le démarrage tant que
     * {@code flyway_schema_history} garde success=0. {@code repair()} retire ces entrées
     * et réaligne les checksums. À activer ponctuellement en dev via
     * {@code app.flyway.repair-before-migrate=true}, puis repasser à false.
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
