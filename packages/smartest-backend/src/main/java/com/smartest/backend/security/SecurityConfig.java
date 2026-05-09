package com.smartest.backend.security;

import org.springframework.boot.web.servlet.FilterRegistrationBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;
import org.springframework.web.cors.CorsConfiguration;
import org.springframework.web.cors.CorsConfigurationSource;
import org.springframework.web.cors.UrlBasedCorsConfigurationSource;

import java.util.List;

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
                .csrf(AbstractHttpConfigurer::disable)
                .cors(cors -> cors.configurationSource(corsConfigurationSource()))

                .authorizeHttpRequests(auth -> auth
                        .requestMatchers(
                                "/auth/register",
                                "/auth/register/etudiant",
                                "/auth/login",
                                "/auth/verify-email",
                                "/auth/verify-email/code",    // ✅ vérification par code (desktop)
                                "/auth/verify-email/resend",          // ✅ renvoi du code (desktop)
                                "/auth/verify-email/resend/etudiant", // ✅ renvoi du lien (web)
                                "/auth/forgot-password/etudiant",
                                "/auth/forgot-password/professeur",
                                "/auth/reset-password/etudiant",
                                "/auth/reset-password/professeur",
                                "/ws/**"
                        ).permitAll()
                        .requestMatchers(HttpMethod.GET, "/api/quizs/mes-publications-web").hasRole("ETUDIANT")
                        .requestMatchers(HttpMethod.GET, "/api/examens-publies/mes-publications-web").hasRole("ETUDIANT")
                        .requestMatchers(HttpMethod.GET, "/api/examens-publies/*/metadata").hasAnyRole("ETUDIANT", "PROFESSEUR")
                        .requestMatchers(HttpMethod.GET, "/api/quizs/*/passage-web").hasRole("ETUDIANT")
                        .requestMatchers(HttpMethod.GET, "/api/quizs/publies").hasRole("ETUDIANT")
                        .requestMatchers(HttpMethod.POST, "/api/quizs/*/soumettre").hasRole("ETUDIANT")
                        .requestMatchers(HttpMethod.POST, "/api/quizs/*/soumettre-web").hasRole("ETUDIANT")
                        .requestMatchers(HttpMethod.POST, "/api/quizs/*/verifier-question-web").hasRole("ETUDIANT")
                        .requestMatchers(HttpMethod.POST, "/api/quizs/*/publication-web").hasRole("PROFESSEUR")
                        .requestMatchers(HttpMethod.DELETE, "/api/quizs/*").hasRole("PROFESSEUR")
                        .requestMatchers(HttpMethod.PATCH, "/api/quizs/*/publier").hasRole("PROFESSEUR")
                        .requestMatchers(HttpMethod.GET, "/api/statistiques/**").hasRole("PROFESSEUR")
                        .requestMatchers("/api/examens-publies/**").authenticated()
                        .requestMatchers("/api/professeur/**").hasRole("PROFESSEUR")
                        .requestMatchers("/api/etudiant/**").hasRole("ETUDIANT")
                        .anyRequest().authenticated()
                )

                .sessionManagement(session -> session
                        .sessionCreationPolicy(SessionCreationPolicy.STATELESS)
                )

                .addFilterBefore(jwtAuthFilter,
                        UsernamePasswordAuthenticationFilter.class);

        return http.build();
    }

    @Bean
    public FilterRegistrationBean<JwtAuthFilter> jwtFilterRegistration(
            JwtAuthFilter filter) {
        FilterRegistrationBean<JwtAuthFilter> registration =
                new FilterRegistrationBean<>(filter);
        registration.setEnabled(false);
        return registration;
    }

    @Bean
    public CorsConfigurationSource corsConfigurationSource() {
        CorsConfiguration config = new CorsConfiguration();

        // ✅ Autorise React web (localhost:5173) + app desktop WPF (sans Origin header)
        config.setAllowedOriginPatterns(List.of("*"));
        config.setAllowedMethods(List.of("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"));
        config.setAllowedHeaders(List.of("*"));
        config.setAllowCredentials(false); // false car desktop n'envoie pas de cookies

        UrlBasedCorsConfigurationSource source = new UrlBasedCorsConfigurationSource();
        source.registerCorsConfiguration("/**", config);
        return source;
    }

    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }
}