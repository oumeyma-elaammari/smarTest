package com.smartest.backend.controller;

import com.smartest.backend.dto.LoginRequest;
import com.smartest.backend.dto.request.*;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.exception.*;
import com.smartest.backend.service.AuthService;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("AuthController — Tests complets")
class AuthControllerTest {

    @Mock private AuthService authService;
    @InjectMocks private AuthController authController;

    // ══════════════════════════════════════════════════════
    //  REGISTER PROFESSEUR
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("POST /auth/register")
    class RegisterProfesseurTests {

        @Test
        @DisplayName("✅ 201 — Inscription réussie")
        void register_Returns201() {
            when(authService.register(any())).thenReturn("Inscription réussie !");

            RegisterRequest req = new RegisterRequest();
            req.setNom("Ikram Laaroussi");
            req.setEmail("ikram@ensa.ma");
            req.setPassword("Ensa2025@");
            req.setConfirmPassword("Ensa2025@");

            ResponseEntity<String> response = authController.register(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.CREATED);
            assertThat(response.getBody()).isEqualTo("Inscription réussie !");
        }

        @Test
        @DisplayName("❌ 409 — Email déjà utilisé")
        void register_Returns409_EmailAlreadyUsed() {
            when(authService.register(any()))
                    .thenThrow(new EmailAlreadyUsedException("ikram@ensa.ma"));

            RegisterRequest req = new RegisterRequest();
            req.setNom("Ikram Laaroussi");
            req.setEmail("ikram@ensa.ma");
            req.setPassword("Ensa2025@");
            req.setConfirmPassword("Ensa2025@");

            ResponseEntity<String> response = authController.register(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.CONFLICT);
        }

        @Test
        @DisplayName("❌ 400 — Mots de passe différents")
        void register_Returns400_PasswordMismatch() {
            when(authService.register(any()))
                    .thenThrow(new PasswordMismatchException());

            RegisterRequest req = new RegisterRequest();
            req.setNom("Ikram Laaroussi");
            req.setEmail("ikram@ensa.ma");
            req.setPassword("Ensa2025@");
            req.setConfirmPassword("AutrePass1!");

            ResponseEntity<String> response = authController.register(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
        }
    }

    // ══════════════════════════════════════════════════════
    //  REGISTER ETUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("POST /auth/register/etudiant")
    class RegisterEtudiantTests {

        @Test
        @DisplayName("✅ 201 — Inscription réussie")
        void registerEtudiant_Returns201() {
            when(authService.registerEtudiant(any())).thenReturn("Inscription réussie !");

            RegisterEtudiantRequest req = new RegisterEtudiantRequest();
            req.setNom("Nissrine El Aammari");
            req.setEmail("nissrine@ump.ac.ma");
            req.setPassword("Ensa2025@");
            req.setConfirmPassword("Ensa2025@");

            ResponseEntity<String> response = authController.registerEtudiant(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.CREATED);
        }

        @Test
        @DisplayName("❌ 409 — Email déjà utilisé")
        void registerEtudiant_Returns409() {
            when(authService.registerEtudiant(any()))
                    .thenThrow(new EmailAlreadyUsedException("nissrine@ump.ac.ma"));

            RegisterEtudiantRequest req = new RegisterEtudiantRequest();
            req.setNom("Nissrine El Aammari");
            req.setEmail("nissrine@ump.ac.ma");
            req.setPassword("Ensa2025@");
            req.setConfirmPassword("Ensa2025@");

            ResponseEntity<String> response = authController.registerEtudiant(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.CONFLICT);
        }

        @Test
        @DisplayName("❌ 400 — Mots de passe différents")
        void registerEtudiant_Returns400_PasswordMismatch() {
            when(authService.registerEtudiant(any()))
                    .thenThrow(new PasswordMismatchException());

            RegisterEtudiantRequest req = new RegisterEtudiantRequest();
            req.setNom("Nissrine El Aammari");
            req.setEmail("nissrine@ump.ac.ma");
            req.setPassword("Ensa2025@");
            req.setConfirmPassword("AutrePass!");

            ResponseEntity<String> response = authController.registerEtudiant(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
        }
    }

    // ══════════════════════════════════════════════════════
    //  LOGIN
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("POST /auth/login")
    class LoginTests {

        @Test
        @DisplayName("✅ 200 — Login réussi étudiant")
        void login_Returns200_Etudiant() {
            AuthResponse authResponse = new AuthResponse(
                    "jwt-token", "ETUDIANT", "Nissrine", "nissrine@ump.ac.ma");
            when(authService.login(any())).thenReturn(authResponse);

            LoginRequest req = new LoginRequest();
            req.setEmail("nissrine@ump.ac.ma");
            req.setPassword("Ensa2025@");

            ResponseEntity<?> response = authController.login(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            AuthResponse body = (AuthResponse) response.getBody();
            assertThat(body).isNotNull();
            assertThat(body.getToken()).isEqualTo("jwt-token");
            assertThat(body.getRole()).isEqualTo("ETUDIANT");
        }

        @Test
        @DisplayName("✅ 200 — Login réussi professeur")
        void login_Returns200_Professeur() {
            AuthResponse authResponse = new AuthResponse(
                    "jwt-token-prof", "PROFESSEUR", "Ikram", "ikram@ensa.ma");
            when(authService.login(any())).thenReturn(authResponse);

            LoginRequest req = new LoginRequest();
            req.setEmail("ikram@ensa.ma");
            req.setPassword("Ensa2025@");

            ResponseEntity<?> response = authController.login(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            AuthResponse body = (AuthResponse) response.getBody();
            assertThat(body.getRole()).isEqualTo("PROFESSEUR");
        }

        @Test
        @DisplayName("❌ 401 — Mauvais mot de passe")
        void login_Returns401_WrongPassword() {
            when(authService.login(any())).thenThrow(new InvalidPasswordException());

            LoginRequest req = new LoginRequest();
            req.setEmail("nissrine@ump.ac.ma");
            req.setPassword("MauvaisPass1!");

            ResponseEntity<?> response = authController.login(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.UNAUTHORIZED);
        }

        @Test
        @DisplayName("❌ 401 — Compte introuvable")
        void login_Returns401_AccountNotFound() {
            when(authService.login(any()))
                    .thenThrow(new AccountNotFoundException("inconnu@ump.ac.ma"));

            LoginRequest req = new LoginRequest();
            req.setEmail("inconnu@ump.ac.ma");
            req.setPassword("Ensa2025@");

            ResponseEntity<?> response = authController.login(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.UNAUTHORIZED);
        }
    }

    // ══════════════════════════════════════════════════════
    //  VERIFY EMAIL
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("GET /auth/verify-email")
    class VerifyEmailTests {

        @Test
        @DisplayName("✅ 302 — Redirect verified=true")
        void verifyEmail_Success_Redirect() {
            doNothing().when(authService).verifyEmail(anyString(), anyString());

            ResponseEntity<Void> response =
                    authController.verifyEmail("valid-token", "ETUDIANT");

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.FOUND);
            assertThat(response.getHeaders().getLocation().toString())
                    .isEqualTo("http://localhost:5173/login?verified=true");
        }

        @Test
        @DisplayName("❌ 302 — Redirect verified=false si token invalide")
        void verifyEmail_InvalidToken_Redirect() {
            doThrow(new InvalidTokenException())
                    .when(authService).verifyEmail(anyString(), anyString());

            ResponseEntity<Void> response =
                    authController.verifyEmail("invalid-token", "ETUDIANT");

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.FOUND);
            assertThat(response.getHeaders().getLocation().toString())
                    .isEqualTo("http://localhost:5173/login?verified=false");
        }
    }

    // ══════════════════════════════════════════════════════
    //  FORGOT PASSWORD
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("POST /auth/forgot-password")
    class ForgotPasswordTests {

        @Test
        @DisplayName("✅ 200 — Forgot password étudiant")
        void forgotPasswordEtudiant_Returns200() {
            doNothing().when(authService).forgotPasswordEtudiant(anyString());

            ForgotPasswordRequest req = new ForgotPasswordRequest();
            req.setEmail("nissrine@ump.ac.ma");

            ResponseEntity<String> response = authController.forgotPasswordEtudiant(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody()).contains("Si cet email existe");
        }

        @Test
        @DisplayName("✅ 200 — Forgot password professeur")
        void forgotPasswordProfesseur_Returns200() {
            doNothing().when(authService).forgotPasswordProfesseur(anyString());

            ForgotPasswordRequest req = new ForgotPasswordRequest();
            req.setEmail("ikram@ensa.ma");

            ResponseEntity<String> response = authController.forgotPasswordProfesseur(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody()).contains("Si cet email existe");
        }
    }

    // ══════════════════════════════════════════════════════
    //  RESET PASSWORD
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("POST /auth/reset-password")
    class ResetPasswordTests {

        @Test
        @DisplayName("✅ 200 — Reset password étudiant")
        void resetPasswordEtudiant_Returns200() {
            doNothing().when(authService)
                    .resetPasswordEtudiant(anyString(), anyString(), anyString());

            ResetPasswordRequest req = new ResetPasswordRequest();
            req.setToken("valid-token");
            req.setNewPassword("NewPass2025@");
            req.setConfirmPassword("NewPass2025@");

            ResponseEntity<String> response = authController.resetPasswordEtudiant(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody()).isEqualTo("Mot de passe réinitialisé avec succès.");
        }

        @Test
        @DisplayName("✅ 200 — Reset password professeur")
        void resetPasswordProfesseur_Returns200() {
            doNothing().when(authService)
                    .resetPasswordProfesseur(anyString(), anyString(), anyString());

            ResetPasswordRequest req = new ResetPasswordRequest();
            req.setToken("valid-token-prof");
            req.setNewPassword("NewPass2025@");
            req.setConfirmPassword("NewPass2025@");

            ResponseEntity<String> response = authController.resetPasswordProfesseur(req);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody()).isEqualTo("Mot de passe réinitialisé avec succès.");
        }
    }
}