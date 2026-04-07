package com.smartest.backend.security;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

@Configuration
@EnableWebSecurity
public class SecurityConfig {

    private final JwtAuthFilter jwtAuthFilter;

    public SecurityConfig(JwtAuthFilter jwtAuthFilter) {
        this.jwtAuthFilter = jwtAuthFilter;
    }

    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http
                // ── Désactiver CSRF (API REST stateless) ──────────
                .csrf(AbstractHttpConfigurer::disable)

                // ── Règles d'accès par route ───────────────────────
                .authorizeHttpRequests(auth -> auth

                        // Routes libres (sans token)
                        .requestMatchers("/auth/**").permitAll()
                        // ↑ /auth/register et /auth/login accessibles à tous

                        // Routes réservées au professeur
                        .requestMatchers("/api/professeur/**")
                        .hasRole("PROFESSEUR")
                        // ↑ Spring cherche "ROLE_PROFESSEUR" dans le token

                        // Routes réservées à l'étudiant
                        .requestMatchers("/api/etudiant/**")
                        .hasRole("ETUDIANT")

                        // Toutes les autres routes → authentification requise
                        .anyRequest().authenticated()
                )

                // ── Session stateless (JWT, pas de session serveur) ─
                .sessionManagement(session -> session
                        .sessionCreationPolicy(SessionCreationPolicy.STATELESS)
                )
                // ↑ Spring ne crée pas de session HTTP
                // chaque requête doit avoir son token JWT

                // ── Ajouter notre filtre JWT avant Spring Security ──
                .addFilterBefore(jwtAuthFilter,
                        UsernamePasswordAuthenticationFilter.class);
        // ↑ notre filtre s'exécute AVANT le filtre par défaut
        //   de Spring Security

        return http.build();
    }

    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }
}