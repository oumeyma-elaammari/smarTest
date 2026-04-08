package com.smartest.backend.service;

import com.smartest.backend.dto.LoginRequest;
import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Utilisateur;
import com.smartest.backend.entity.enumeration.Role;
import com.smartest.backend.exception.*;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.UtilisateurRepository;
import com.smartest.backend.security.JwtUtil;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.crypto.password.PasswordEncoder;

import java.time.LocalDateTime;
import java.util.Optional;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("AuthService — Tests complets")
class AuthServiceTest {

    @Mock private ProfesseurRepository professeurRepository;
    @Mock private UtilisateurRepository utilisateurRepository;
    @Mock private PasswordEncoder passwordEncoder;
    @Mock private JwtUtil jwtUtil;
    @Mock private EmailService emailService;

    @InjectMocks
    private AuthService authService;

    private RegisterRequest validProfRequest;
    private RegisterEtudiantRequest validEtudiantRequest;
    private LoginRequest loginProfRequest;
    private LoginRequest loginEtudiantRequest;
    private Professeur professeur;
    private Utilisateur etudiant;

    @BeforeEach
    void setUp() {

        // ── Entité Professeur ─────────────────────────────
        // ✅ initialisés EN PREMIER
        professeur = new Professeur();
        professeur.setNom("Ikram Laaroussi");
        professeur.setEmail("ikram@ensa.ma");
        professeur.setPassword("hashedPassword");
        professeur.setRole(Role.PROFESSEUR);
        professeur.setEmailVerifie(true);
        professeur.setResetPasswordToken(null);
        professeur.setResetPasswordExpiry(null);

        // ── Entité Étudiant ───────────────────────────────
        etudiant = new Utilisateur();
        etudiant.setNom("Nissrine El Aammari");
        etudiant.setEmail("nissrine@ump.ac.ma");
        etudiant.setPassword("hashedPassword");
        etudiant.setRole(Role.ETUDIANT);
        etudiant.setEmailVerifie(true);
        etudiant.setResetPasswordToken(null);
        etudiant.setResetPasswordExpiry(null);

        // ── Register Professeur ───────────────────────────
        validProfRequest = new RegisterRequest();
        validProfRequest.setNom("Ikram Laaroussi");
        validProfRequest.setEmail("ikram@ensa.ma");
        validProfRequest.setPassword("Ensa2025@");
        validProfRequest.setConfirmPassword("Ensa2025@");

        // ── Register Étudiant ─────────────────────────────
        validEtudiantRequest = new RegisterEtudiantRequest();
        validEtudiantRequest.setNom("Nissrine El Aammari");
        validEtudiantRequest.setEmail("nissrine@ump.ac.ma");
        validEtudiantRequest.setPassword("Ensa2025@");
        validEtudiantRequest.setConfirmPassword("Ensa2025@");

        // ── Login Professeur ──────────────────────────────
        loginProfRequest = new LoginRequest();
        loginProfRequest.setEmail("ikram@ensa.ma");
        loginProfRequest.setPassword("Ensa2025@");

        // ── Login Étudiant ────────────────────────────────
        loginEtudiantRequest = new LoginRequest();
        loginEtudiantRequest.setEmail("nissrine@ump.ac.ma");
        loginEtudiantRequest.setPassword("Ensa2025@");
    }

    // ══════════════════════════════════════════════════════
    //  REGISTER PROFESSEUR
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Register Professeur")
    class RegisterProfesseurTests {

        @Test
        @DisplayName("✅ Inscription réussie")
        void register_Success() {
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            String result = authService.register(validProfRequest);

            assertThat(result).contains("Inscription réussie");
            verify(professeurRepository).save(any(Professeur.class));
            verify(emailService).sendVerificationEmail(
                    eq("ikram@ensa.ma"), anyString(), eq("PROFESSEUR"));
        }

        @Test
        @DisplayName("❌ Email déjà utilisé")
        void register_EmailAlreadyUsed() {
            when(professeurRepository.existsByEmail("ikram@ensa.ma")).thenReturn(true);

            assertThatThrownBy(() -> authService.register(validProfRequest))
                    .isInstanceOf(EmailAlreadyUsedException.class);

            verify(professeurRepository, never()).save(any());
            verify(emailService, never()).sendVerificationEmail(any(), any(), any());
        }

        @Test
        @DisplayName("❌ Mots de passe différents")
        void register_PasswordMismatch() {
            validProfRequest.setConfirmPassword("AutrePassword1!");

            assertThatThrownBy(() -> authService.register(validProfRequest))
                    .isInstanceOf(PasswordMismatchException.class);

            verify(professeurRepository, never()).save(any());
            verify(emailService, never()).sendVerificationEmail(any(), any(), any());
        }

        @Test
        @DisplayName("✅ Token de vérification généré")
        void register_TokenGenerated() {
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            authService.register(validProfRequest);

            verify(professeurRepository).save(argThat(prof ->
                    prof.getTokenVerification() != null &&
                            !prof.getTokenVerification().isEmpty() &&
                            !prof.isEmailVerifie()
            ));
        }

        @Test
        @DisplayName("✅ Email envoyé avec rôle PROFESSEUR")
        void register_EmailSentWithCorrectRole() {
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            authService.register(validProfRequest);

            verify(emailService).sendVerificationEmail(
                    eq("ikram@ensa.ma"), anyString(), eq("PROFESSEUR"));
        }
    }

    // ══════════════════════════════════════════════════════
    //  REGISTER ÉTUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Register Étudiant")
    class RegisterEtudiantTests {

        @Test
        @DisplayName("✅ Inscription réussie")
        void registerEtudiant_Success() {
            when(utilisateurRepository.existsByEmail(anyString())).thenReturn(false);
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            String result = authService.registerEtudiant(validEtudiantRequest);

            assertThat(result).contains("Inscription réussie");
            verify(utilisateurRepository).save(any(Utilisateur.class));
            verify(emailService).sendVerificationEmail(
                    eq("nissrine@ump.ac.ma"), anyString(), eq("ETUDIANT"));
        }

        @Test
        @DisplayName("❌ Email déjà utilisé par un étudiant")
        void registerEtudiant_EmailUsedByEtudiant() {
            when(utilisateurRepository.existsByEmail("nissrine@ump.ac.ma")).thenReturn(true);

            assertThatThrownBy(() -> authService.registerEtudiant(validEtudiantRequest))
                    .isInstanceOf(EmailAlreadyUsedException.class);

            verify(utilisateurRepository, never()).save(any());
            verify(emailService, never()).sendVerificationEmail(any(), any(), any());
        }

        @Test
        @DisplayName("❌ Email déjà utilisé par un professeur")
        void registerEtudiant_EmailUsedByProfesseur() {
            when(utilisateurRepository.existsByEmail(anyString())).thenReturn(false);
            when(professeurRepository.existsByEmail("nissrine@ump.ac.ma")).thenReturn(true);

            assertThatThrownBy(() -> authService.registerEtudiant(validEtudiantRequest))
                    .isInstanceOf(EmailAlreadyUsedException.class);

            verify(utilisateurRepository, never()).save(any());
            verify(emailService, never()).sendVerificationEmail(any(), any(), any());
        }

        @Test
        @DisplayName("❌ Mots de passe différents")
        void registerEtudiant_PasswordMismatch() {
            validEtudiantRequest.setConfirmPassword("AutrePassword1!");

            assertThatThrownBy(() -> authService.registerEtudiant(validEtudiantRequest))
                    .isInstanceOf(PasswordMismatchException.class);

            verify(utilisateurRepository, never()).save(any());
            verify(emailService, never()).sendVerificationEmail(any(), any(), any());
        }

        @Test
        @DisplayName("✅ emailVerifie = false à la création")
        void registerEtudiant_EmailNotVerifiedByDefault() {
            when(utilisateurRepository.existsByEmail(anyString())).thenReturn(false);
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            authService.registerEtudiant(validEtudiantRequest);

            verify(utilisateurRepository).save(argThat(u ->
                    !u.isEmailVerifie() && u.getTokenVerification() != null
            ));
        }

        @Test
        @DisplayName("✅ Email envoyé avec rôle ETUDIANT")
        void registerEtudiant_EmailSentWithCorrectRole() {
            when(utilisateurRepository.existsByEmail(anyString())).thenReturn(false);
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            authService.registerEtudiant(validEtudiantRequest);

            verify(emailService).sendVerificationEmail(
                    eq("nissrine@ump.ac.ma"), anyString(), eq("ETUDIANT"));
        }
    }

    // ══════════════════════════════════════════════════════
    //  VERIFY EMAIL
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Verify Email")
    class VerifyEmailTests {

        @Test
        @DisplayName("✅ Vérification Professeur réussie")
        void verifyEmail_Professeur_Success() {
            professeur.setEmailVerifie(false);
            professeur.setTokenVerification("valid-token-prof");
            when(professeurRepository.findByTokenVerification("valid-token-prof"))
                    .thenReturn(Optional.of(professeur));

            authService.verifyEmail("valid-token-prof", "PROFESSEUR");

            assertThat(professeur.isEmailVerifie()).isTrue();
            assertThat(professeur.getTokenVerification()).isNull();
            verify(professeurRepository).save(professeur);
        }

        @Test
        @DisplayName("✅ Vérification Étudiant réussie")
        void verifyEmail_Etudiant_Success() {
            etudiant.setEmailVerifie(false);
            etudiant.setTokenVerification("valid-token-etudiant");
            when(utilisateurRepository.findByTokenVerification("valid-token-etudiant"))
                    .thenReturn(Optional.of(etudiant));

            authService.verifyEmail("valid-token-etudiant", "ETUDIANT");

            assertThat(etudiant.isEmailVerifie()).isTrue();
            assertThat(etudiant.getTokenVerification()).isNull();
            verify(utilisateurRepository).save(etudiant);
        }

        @Test
        @DisplayName("❌ Token invalide Professeur")
        void verifyEmail_Professeur_InvalidToken() {
            when(professeurRepository.findByTokenVerification("invalid-token"))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() -> authService.verifyEmail("invalid-token", "PROFESSEUR"))
                    .isInstanceOf(InvalidTokenException.class);
        }

        @Test
        @DisplayName("❌ Token invalide Étudiant")
        void verifyEmail_Etudiant_InvalidToken() {
            when(utilisateurRepository.findByTokenVerification("invalid-token"))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() -> authService.verifyEmail("invalid-token", "ETUDIANT"))
                    .isInstanceOf(InvalidTokenException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  LOGIN PROFESSEUR
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Login Professeur")
    class LoginProfesseurTests {

        @Test
        @DisplayName("✅ Connexion réussie")
        void login_Professeur_Success() {
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));
            when(passwordEncoder.matches("Ensa2025@", "hashedPassword"))
                    .thenReturn(true);
            when(jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR"))
                    .thenReturn("jwt-token-prof");

            AuthResponse response = authService.login(loginProfRequest);

            assertThat(response.getToken()).isEqualTo("jwt-token-prof");
            assertThat(response.getRole()).isEqualTo("PROFESSEUR");
            assertThat(response.getNom()).isEqualTo("Ikram Laaroussi");
            assertThat(response.getEmail()).isEqualTo("ikram@ensa.ma");
        }

        @Test
        @DisplayName("❌ Email non vérifié")
        void login_Professeur_EmailNotVerified() {
            professeur.setEmailVerifie(false);
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));

            assertThatThrownBy(() -> authService.login(loginProfRequest))
                    .isInstanceOf(EmailNotVerifiedException.class);
        }

        @Test
        @DisplayName("❌ Mauvais mot de passe")
        void login_Professeur_WrongPassword() {
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));
            when(passwordEncoder.matches("Ensa2025@", "hashedPassword"))
                    .thenReturn(false);

            assertThatThrownBy(() -> authService.login(loginProfRequest))
                    .isInstanceOf(InvalidPasswordException.class);
        }

        @Test
        @DisplayName("❌ Compte introuvable")
        void login_Professeur_AccountNotFound() {
            when(professeurRepository.findByEmail("inconnu@ensa.ma"))
                    .thenReturn(Optional.empty());
            when(utilisateurRepository.findByEmail("inconnu@ensa.ma"))
                    .thenReturn(Optional.empty());

            LoginRequest req = new LoginRequest();
            req.setEmail("inconnu@ensa.ma");
            req.setPassword("Ensa2025@");

            assertThatThrownBy(() -> authService.login(req))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  LOGIN ÉTUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Login Étudiant")
    class LoginEtudiantTests {

        @Test
        @DisplayName("✅ Connexion réussie")
        void login_Etudiant_Success() {
            when(professeurRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.empty());
            when(utilisateurRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));
            when(passwordEncoder.matches("Ensa2025@", "hashedPassword"))
                    .thenReturn(true);
            when(jwtUtil.generateToken("nissrine@ump.ac.ma", "ETUDIANT"))
                    .thenReturn("jwt-token-etudiant");

            AuthResponse response = authService.login(loginEtudiantRequest);

            assertThat(response.getToken()).isEqualTo("jwt-token-etudiant");
            assertThat(response.getRole()).isEqualTo("ETUDIANT");
            assertThat(response.getNom()).isEqualTo("Nissrine El Aammari");
            assertThat(response.getEmail()).isEqualTo("nissrine@ump.ac.ma");
        }

        @Test
        @DisplayName("❌ Email non vérifié")
        void login_Etudiant_EmailNotVerified() {
            etudiant.setEmailVerifie(false);
            when(professeurRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.empty());
            when(utilisateurRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));

            assertThatThrownBy(() -> authService.login(loginEtudiantRequest))
                    .isInstanceOf(EmailNotVerifiedException.class);
        }

        @Test
        @DisplayName("❌ Mauvais mot de passe")
        void login_Etudiant_WrongPassword() {
            when(professeurRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.empty());
            when(utilisateurRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));
            when(passwordEncoder.matches("Ensa2025@", "hashedPassword"))
                    .thenReturn(false);

            assertThatThrownBy(() -> authService.login(loginEtudiantRequest))
                    .isInstanceOf(InvalidPasswordException.class);
        }

        @Test
        @DisplayName("❌ Compte introuvable")
        void login_Etudiant_AccountNotFound() {
            when(professeurRepository.findByEmail("inconnu@ump.ac.ma"))
                    .thenReturn(Optional.empty());
            when(utilisateurRepository.findByEmail("inconnu@ump.ac.ma"))
                    .thenReturn(Optional.empty());

            LoginRequest req = new LoginRequest();
            req.setEmail("inconnu@ump.ac.ma");
            req.setPassword("Ensa2025@");

            assertThatThrownBy(() -> authService.login(req))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  FORGOT PASSWORD ÉTUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Forgot Password Étudiant")
    class ForgotPasswordEtudiantTests {

        @Test
        @DisplayName("✅ Email reset envoyé à l'étudiant")
        void forgotPasswordEtudiant_Success() {
            when(professeurRepository.existsByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(false);
            when(utilisateurRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));

            authService.forgotPasswordEtudiant("nissrine@ump.ac.ma");

            verify(utilisateurRepository).save(argThat(u ->
                    u.getResetPasswordToken() != null &&
                            u.getResetPasswordExpiry() != null
            ));
            verify(emailService).sendResetPasswordEmail(
                    eq("nissrine@ump.ac.ma"), anyString());
        }

        @Test
        @DisplayName("❌ Email professeur → silencieux")
        void forgotPasswordEtudiant_ProfEmail_Silent() {
            when(professeurRepository.existsByEmail("ikram@ensa.ma"))
                    .thenReturn(true);

            authService.forgotPasswordEtudiant("ikram@ensa.ma");

            verify(utilisateurRepository, never()).findByEmail(any());
            verify(utilisateurRepository, never()).save(any());
            verify(emailService, never()).sendResetPasswordEmail(any(), any());
        }

        @Test
        @DisplayName("❌ Email inconnu → silencieux")
        void forgotPasswordEtudiant_UnknownEmail_Silent() {
            when(professeurRepository.existsByEmail("inconnu@ump.ac.ma"))
                    .thenReturn(false);
            when(utilisateurRepository.findByEmail("inconnu@ump.ac.ma"))
                    .thenReturn(Optional.empty());

            authService.forgotPasswordEtudiant("inconnu@ump.ac.ma");

            verify(utilisateurRepository, never()).save(any());
            verify(emailService, never()).sendResetPasswordEmail(any(), any());
        }

        @Test
        @DisplayName("✅ Token et expiry bien générés")
        void forgotPasswordEtudiant_TokenGenerated() {
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(utilisateurRepository.findByEmail(anyString()))
                    .thenReturn(Optional.of(etudiant));

            authService.forgotPasswordEtudiant("nissrine@ump.ac.ma");

            verify(utilisateurRepository).save(argThat(u ->
                    u.getResetPasswordToken() != null &&
                            !u.getResetPasswordToken().isEmpty() &&
                            u.getResetPasswordExpiry() != null &&
                            u.getResetPasswordExpiry().isAfter(LocalDateTime.now())
            ));
        }
    }

    // ══════════════════════════════════════════════════════
    //  FORGOT PASSWORD PROFESSEUR
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Forgot Password Professeur")
    class ForgotPasswordProfesseurTests {

        @Test
        @DisplayName("✅ Email reset envoyé au professeur")
        void forgotPasswordProfesseur_Success() {
            when(utilisateurRepository.existsByEmail("ikram@ensa.ma"))
                    .thenReturn(false);
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));

            authService.forgotPasswordProfesseur("ikram@ensa.ma");

            verify(professeurRepository).save(argThat(p ->
                    p.getResetPasswordToken() != null &&
                            p.getResetPasswordExpiry() != null
            ));
            verify(emailService).sendResetPasswordEmail(
                    eq("ikram@ensa.ma"), anyString());
        }

        @Test
        @DisplayName("❌ Email étudiant → silencieux")
        void forgotPasswordProfesseur_EtudiantEmail_Silent() {
            when(utilisateurRepository.existsByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(true);

            authService.forgotPasswordProfesseur("nissrine@ump.ac.ma");

            verify(professeurRepository, never()).findByEmail(any());
            verify(professeurRepository, never()).save(any());
            verify(emailService, never()).sendResetPasswordEmail(any(), any());
        }

        @Test
        @DisplayName("❌ Email inconnu → silencieux")
        void forgotPasswordProfesseur_UnknownEmail_Silent() {
            when(utilisateurRepository.existsByEmail("inconnu@ensa.ma"))
                    .thenReturn(false);
            when(professeurRepository.findByEmail("inconnu@ensa.ma"))
                    .thenReturn(Optional.empty());

            authService.forgotPasswordProfesseur("inconnu@ensa.ma");

            verify(professeurRepository, never()).save(any());
            verify(emailService, never()).sendResetPasswordEmail(any(), any());
        }
    }

    // ══════════════════════════════════════════════════════
    //  RESET PASSWORD ÉTUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Reset Password Étudiant")
    class ResetPasswordEtudiantTests {

        @Test
        @DisplayName("✅ Réinitialisation réussie")
        void resetPasswordEtudiant_Success() {
            etudiant.setResetPasswordToken("valid-token");
            etudiant.setResetPasswordExpiry(LocalDateTime.now().plusMinutes(10));
            when(utilisateurRepository.findByResetPasswordToken("valid-token"))
                    .thenReturn(Optional.of(etudiant));
            when(passwordEncoder.encode("NewPass2025@"))
                    .thenReturn("newHashedPassword");

            authService.resetPasswordEtudiant("valid-token", "NewPass2025@", "NewPass2025@");

            verify(utilisateurRepository).save(argThat(u ->
                    u.getPassword().equals("newHashedPassword") &&
                            u.getResetPasswordToken() == null &&
                            u.getResetPasswordExpiry() == null
            ));
        }

        @Test
        @DisplayName("❌ Token invalide")
        void resetPasswordEtudiant_InvalidToken() {
            when(utilisateurRepository.findByResetPasswordToken("invalid-token"))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    authService.resetPasswordEtudiant("invalid-token", "NewPass2025@", "NewPass2025@")
            ).isInstanceOf(InvalidTokenException.class);

            verify(utilisateurRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Token expiré")
        void resetPasswordEtudiant_ExpiredToken() {
            etudiant.setResetPasswordToken("expired-token");
            etudiant.setResetPasswordExpiry(LocalDateTime.now().minusMinutes(5));
            when(utilisateurRepository.findByResetPasswordToken("expired-token"))
                    .thenReturn(Optional.of(etudiant));

            assertThatThrownBy(() ->
                    authService.resetPasswordEtudiant("expired-token", "NewPass2025@", "NewPass2025@")
            ).isInstanceOf(InvalidTokenException.class);

            verify(utilisateurRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Mots de passe différents")
        void resetPasswordEtudiant_PasswordMismatch() {
            assertThatThrownBy(() ->
                    authService.resetPasswordEtudiant("valid-token", "NewPass2025@", "AutrePass2025@")
            ).isInstanceOf(PasswordMismatchException.class);

            verify(utilisateurRepository, never()).findByResetPasswordToken(any());
            verify(utilisateurRepository, never()).save(any());
        }

        @Test
        @DisplayName("✅ Token supprimé après reset")
        void resetPasswordEtudiant_TokenClearedAfterReset() {
            etudiant.setResetPasswordToken("valid-token");
            etudiant.setResetPasswordExpiry(LocalDateTime.now().plusMinutes(10));
            when(utilisateurRepository.findByResetPasswordToken("valid-token"))
                    .thenReturn(Optional.of(etudiant));
            when(passwordEncoder.encode(anyString())).thenReturn("newHashedPassword");

            authService.resetPasswordEtudiant("valid-token", "NewPass2025@", "NewPass2025@");

            assertThat(etudiant.getResetPasswordToken()).isNull();
            assertThat(etudiant.getResetPasswordExpiry()).isNull();
        }
    }

    // ══════════════════════════════════════════════════════
    //  RESET PASSWORD PROFESSEUR
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Reset Password Professeur")
    class ResetPasswordProfesseurTests {

        @Test
        @DisplayName("✅ Réinitialisation réussie")
        void resetPasswordProfesseur_Success() {
            professeur.setResetPasswordToken("valid-token-prof");
            professeur.setResetPasswordExpiry(LocalDateTime.now().plusMinutes(10));
            when(professeurRepository.findByResetPasswordToken("valid-token-prof"))
                    .thenReturn(Optional.of(professeur));
            when(passwordEncoder.encode("NewPass2025@"))
                    .thenReturn("newHashedPassword");

            authService.resetPasswordProfesseur("valid-token-prof", "NewPass2025@", "NewPass2025@");

            verify(professeurRepository).save(argThat(p ->
                    p.getPassword().equals("newHashedPassword") &&
                            p.getResetPasswordToken() == null &&
                            p.getResetPasswordExpiry() == null
            ));
        }

        @Test
        @DisplayName("❌ Token invalide")
        void resetPasswordProfesseur_InvalidToken() {
            when(professeurRepository.findByResetPasswordToken("invalid-token"))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    authService.resetPasswordProfesseur("invalid-token", "NewPass2025@", "NewPass2025@")
            ).isInstanceOf(InvalidTokenException.class);

            verify(professeurRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Token expiré")
        void resetPasswordProfesseur_ExpiredToken() {
            professeur.setResetPasswordToken("expired-token-prof");
            professeur.setResetPasswordExpiry(LocalDateTime.now().minusMinutes(5));
            when(professeurRepository.findByResetPasswordToken("expired-token-prof"))
                    .thenReturn(Optional.of(professeur));

            assertThatThrownBy(() ->
                    authService.resetPasswordProfesseur("expired-token-prof", "NewPass2025@", "NewPass2025@")
            ).isInstanceOf(InvalidTokenException.class);

            verify(professeurRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Mots de passe différents")
        void resetPasswordProfesseur_PasswordMismatch() {
            assertThatThrownBy(() ->
                    authService.resetPasswordProfesseur("valid-token", "NewPass2025@", "AutrePass2025@")
            ).isInstanceOf(PasswordMismatchException.class);

            verify(professeurRepository, never()).findByResetPasswordToken(any());
            verify(professeurRepository, never()).save(any());
        }

        @Test
        @DisplayName("✅ Token supprimé après reset")
        void resetPasswordProfesseur_TokenClearedAfterReset() {
            professeur.setResetPasswordToken("valid-token-prof");
            professeur.setResetPasswordExpiry(LocalDateTime.now().plusMinutes(10));
            when(professeurRepository.findByResetPasswordToken("valid-token-prof"))
                    .thenReturn(Optional.of(professeur));
            when(passwordEncoder.encode(anyString())).thenReturn("newHashedPassword");

            authService.resetPasswordProfesseur("valid-token-prof", "NewPass2025@", "NewPass2025@");

            assertThat(professeur.getResetPasswordToken()).isNull();
            assertThat(professeur.getResetPasswordExpiry()).isNull();
        }
    }
}