package com.smartest.backend.service;

import com.smartest.backend.security.JwtUtil;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import static org.assertj.core.api.Assertions.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("JwtUtil — Tests complets")
class JwtUtilTest {

    private JwtUtil jwtUtil;

    private static final String SECRET =
            "SmArTeSt_S3cr3t_K3y_2026_ENSA_ChangeMe_Must_Be_Long_Enough!";
    private static final long EXPIRATION = 86400000L;

    @BeforeEach
    void setUp() {
        jwtUtil = new JwtUtil();
        ReflectionTestUtils.setField(jwtUtil, "secret", SECRET);
        ReflectionTestUtils.setField(jwtUtil, "expiration", EXPIRATION);
    }

    @Nested
    @DisplayName("generateToken")
    class GenerateTokenTests {

        @Test
        @DisplayName("✅ Token généré non null")
        void generateToken_NotNull() {
            String token = jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR");
            assertThat(token).isNotNull().isNotEmpty();
        }

        @Test
        @DisplayName("✅ Token différent pour emails différents")
        void generateToken_DifferentForDifferentEmails() {
            String token1 = jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR");
            String token2 = jwtUtil.generateToken("nissrine@ump.ac.ma", "ETUDIANT");
            assertThat(token1).isNotEqualTo(token2);
        }

        @Test
        @DisplayName("✅ Token contient 3 parties JWT")
        void generateToken_HasThreeParts() {
            String token = jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR");
            assertThat(token.split("\\.")).hasSize(3);
        }
    }

    @Nested
    @DisplayName("extractEmail")
    class ExtractEmailTests {

        @Test
        @DisplayName("✅ Email extrait correctement — Professeur")
        void extractEmail_Professeur() {
            String token = jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR");
            assertThat(jwtUtil.extractEmail(token)).isEqualTo("ikram@ensa.ma");
        }

        @Test
        @DisplayName("✅ Email extrait correctement — Etudiant")
        void extractEmail_Etudiant() {
            String token = jwtUtil.generateToken("nissrine@ump.ac.ma", "ETUDIANT");
            assertThat(jwtUtil.extractEmail(token)).isEqualTo("nissrine@ump.ac.ma");
        }
    }

    @Nested
    @DisplayName("extractRole")
    class ExtractRoleTests {

        @Test
        @DisplayName("✅ Rôle PROFESSEUR extrait correctement")
        void extractRole_Professeur() {
            String token = jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR");
            assertThat(jwtUtil.extractRole(token)).isEqualTo("PROFESSEUR");
        }

        @Test
        @DisplayName("✅ Rôle ETUDIANT extrait correctement")
        void extractRole_Etudiant() {
            String token = jwtUtil.generateToken("nissrine@ump.ac.ma", "ETUDIANT");
            assertThat(jwtUtil.extractRole(token)).isEqualTo("ETUDIANT");
        }
    }

    @Nested
    @DisplayName("validateToken")
    class ValidateTokenTests {

        @Test
        @DisplayName("✅ Token valide retourne true")
        void validateToken_ValidToken() {
            String token = jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR");
            assertThat(jwtUtil.validateToken(token)).isTrue();
        }

        @Test
        @DisplayName("❌ Token invalide retourne false")
        void validateToken_InvalidToken() {
            assertThat(jwtUtil.validateToken("invalid.token.here")).isFalse();
        }

        @Test
        @DisplayName("❌ Token vide retourne false")
        void validateToken_EmptyToken() {
            assertThatCode(() -> {
                boolean result = jwtUtil.validateToken("");
                assertThat(result).isFalse();
            }).doesNotThrowAnyException();
        }

        @Test
        @DisplayName("❌ Token expiré retourne false")
        void validateToken_ExpiredToken() {
            ReflectionTestUtils.setField(jwtUtil, "expiration", 1L);
            String token = jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR");
            try { Thread.sleep(10); } catch (InterruptedException ignored) {}
            assertThat(jwtUtil.validateToken(token)).isFalse();
        }
    }
}