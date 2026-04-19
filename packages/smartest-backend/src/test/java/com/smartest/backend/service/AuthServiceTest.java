package com.smartest.backend.service;

import java.time.LocalDateTime;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.argThat;
import static org.mockito.ArgumentMatchers.eq;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.crypto.password.PasswordEncoder;

import com.smartest.backend.dto.request.LoginRequest;
import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.dto.response.AuthResponse;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.EmailAlreadyUsedException;
import com.smartest.backend.exception.EmailNotVerifiedException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.InvalidTokenException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.security.JwtUtil;

@ExtendWith(MockitoExtension.class)
@DisplayName("AuthService — Tests complets")
class AuthServiceTest {

    @Mock private ProfesseurRepository professeurRepository;
    @Mock private EtudiantRepository etudiantRepository;
    @Mock private PasswordEncoder passwordEncoder;
    @Mock private JwtUtil jwtUtil;
    @Mock private EmailService emailService;

    @InjectMocks private AuthService authService;

    private RegisterRequest validProfRequest;
    private RegisterEtudiantRequest validEtudiantRequest;
    private LoginRequest loginProfRequest;
    private LoginRequest loginEtudiantRequest;
    private Professeur professeur;
    private Etudiant etudiant;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setNom("Ikram Laaroussi");
        professeur.setEmail("ikram@ensa.ma");
        professeur.setPassword("hashedPassword");
        professeur.setEmailVerifie(true);
        professeur.setResetPasswordToken(null);
        professeur.setResetPasswordExpiry(null);

        etudiant = new Etudiant();
        etudiant.setNom("Nissrine El Aammari");
        etudiant.setEmail("nissrine@ump.ac.ma");
        etudiant.setPassword("hashedPassword");
        etudiant.setEmailVerifie(true);
        etudiant.setResetPasswordToken(null);
        etudiant.setResetPasswordExpiry(null);

        validProfRequest = new RegisterRequest();
        validProfRequest.setNom("Ikram Laaroussi");
        validProfRequest.setEmail("ikram@ensa.ma");
        validProfRequest.setPassword("Ensa2025@");
        validProfRequest.setConfirmPassword("Ensa2025@");

        validEtudiantRequest = new RegisterEtudiantRequest();
        validEtudiantRequest.setNom("Nissrine El Aammari");
        validEtudiantRequest.setEmail("nissrine@ump.ac.ma");
        validEtudiantRequest.setPassword("Ensa2025@");
        validEtudiantRequest.setConfirmPassword("Ensa2025@");

        loginProfRequest = new LoginRequest();
        loginProfRequest.setEmail("ikram@ensa.ma");
        loginProfRequest.setPassword("Ensa2025@");

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
            when(etudiantRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            String result = authService.register(validProfRequest);

            assertThat(result).contains("Inscription réussie");
            verify(professeurRepository).save(any(Professeur.class));
            // ✅ Professeur → sendVerificationCode (code 6 chiffres, pas de lien web)
            verify(emailService).sendVerificationCode(eq("ikram@ensa.ma"), anyString());
        }

        @Test
        @DisplayName("❌ Email déjà utilisé par un professeur")
        void register_EmailAlreadyUsed_ByProf() {
            when(professeurRepository.existsByEmail("ikram@ensa.ma")).thenReturn(true);

            assertThatThrownBy(() -> authService.register(validProfRequest))
                    .isInstanceOf(EmailAlreadyUsedException.class);

            verify(professeurRepository, never()).save(any());
            verify(emailService, never()).sendVerificationCode(any(), any());
        }

        @Test
        @DisplayName("❌ Email déjà utilisé par un étudiant")
        void register_EmailAlreadyUsed_ByEtudiant() {
            when(professeurRepository.existsByEmail("ikram@ensa.ma")).thenReturn(false);
            when(etudiantRepository.existsByEmail("ikram@ensa.ma")).thenReturn(true);

            assertThatThrownBy(() -> authService.register(validProfRequest))
                    .isInstanceOf(EmailAlreadyUsedException.class);

            verify(professeurRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Mots de passe différents")
        void register_PasswordMismatch() {
            validProfRequest.setConfirmPassword("AutrePassword1!");

            assertThatThrownBy(() -> authService.register(validProfRequest))
                    .isInstanceOf(PasswordMismatchException.class);

            verify(professeurRepository, never()).save(any());
        }

        @Test
        @DisplayName("✅ emailVerifie = false à la création")
        void register_EmailNotVerifiedByDefault() {
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(etudiantRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            authService.register(validProfRequest);

            verify(professeurRepository).save(argThat(p ->
                    !p.isEmailVerifie() && p.getTokenVerification() != null
            ));
        }

        @Test
        @DisplayName("✅ Code envoyé par email (sans lien web)")
        void register_EmailSentWithCorrectRole() {
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(etudiantRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            authService.register(validProfRequest);

            // ✅ Professeur → sendVerificationCode (code 6 chiffres, pas de lien web)
            verify(emailService).sendVerificationCode(eq("ikram@ensa.ma"), anyString());
        }
    }

    // ══════════════════════════════════════════════════════
    //  REGISTER ETUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Register Etudiant")
    class RegisterEtudiantTests {

        @Test
        @DisplayName("✅ Inscription réussie")
        void registerEtudiant_Success() {
            when(etudiantRepository.existsByEmail(anyString())).thenReturn(false);
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            String result = authService.registerEtudiant(validEtudiantRequest);

            assertThat(result).contains("Inscription réussie");
            verify(etudiantRepository).save(any(Etudiant.class));
            // Etudiant → conserve l'ancien système (lien web)
            verify(emailService).sendVerificationEmail(eq("nissrine@ump.ac.ma"), anyString(), eq("ETUDIANT"));
        }

        @Test
        @DisplayName("❌ Email déjà utilisé par un étudiant")
        void registerEtudiant_EmailUsedByEtudiant() {
            when(etudiantRepository.existsByEmail("nissrine@ump.ac.ma")).thenReturn(true);

            assertThatThrownBy(() -> authService.registerEtudiant(validEtudiantRequest))
                    .isInstanceOf(EmailAlreadyUsedException.class);

            verify(etudiantRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Email déjà utilisé par un professeur")
        void registerEtudiant_EmailUsedByProfesseur() {
            when(etudiantRepository.existsByEmail(anyString())).thenReturn(false);
            when(professeurRepository.existsByEmail("nissrine@ump.ac.ma")).thenReturn(true);

            assertThatThrownBy(() -> authService.registerEtudiant(validEtudiantRequest))
                    .isInstanceOf(EmailAlreadyUsedException.class);

            verify(etudiantRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Mots de passe différents")
        void registerEtudiant_PasswordMismatch() {
            validEtudiantRequest.setConfirmPassword("AutrePassword1!");

            assertThatThrownBy(() -> authService.registerEtudiant(validEtudiantRequest))
                    .isInstanceOf(PasswordMismatchException.class);

            verify(etudiantRepository, never()).save(any());
        }

        @Test
        @DisplayName("✅ emailVerifie = false à la création")
        void registerEtudiant_EmailNotVerifiedByDefault() {
            when(etudiantRepository.existsByEmail(anyString())).thenReturn(false);
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(passwordEncoder.encode(anyString())).thenReturn("hashedPassword");

            authService.registerEtudiant(validEtudiantRequest);

            verify(etudiantRepository).save(argThat(e ->
                    !e.isEmailVerifie() && e.getTokenVerification() != null
            ));
        }

        @Test
        @DisplayName("✅ Email envoyé avec rôle ETUDIANT")
        void registerEtudiant_EmailSentWithCorrectRole() {
            when(etudiantRepository.existsByEmail(anyString())).thenReturn(false);
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
        @DisplayName("✅ Vérification Etudiant réussie")
        void verifyEmail_Etudiant_Success() {
            etudiant.setEmailVerifie(false);
            etudiant.setTokenVerification("valid-token-etudiant");
            when(etudiantRepository.findByTokenVerification("valid-token-etudiant"))
                    .thenReturn(Optional.of(etudiant));

            authService.verifyEmail("valid-token-etudiant", "ETUDIANT");

            assertThat(etudiant.isEmailVerifie()).isTrue();
            assertThat(etudiant.getTokenVerification()).isNull();
            verify(etudiantRepository).save(etudiant);
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
        @DisplayName("❌ Token invalide Etudiant")
        void verifyEmail_Etudiant_InvalidToken() {
            when(etudiantRepository.findByTokenVerification("invalid-token"))
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
            when(professeurRepository.findByEmail("ikram@ensa.ma")).thenReturn(Optional.of(professeur));
            when(passwordEncoder.matches("Ensa2025@", "hashedPassword")).thenReturn(true);
            when(jwtUtil.generateToken("ikram@ensa.ma", "PROFESSEUR")).thenReturn("jwt-token-prof");

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
            when(professeurRepository.findByEmail("ikram@ensa.ma")).thenReturn(Optional.of(professeur));

            assertThatThrownBy(() -> authService.login(loginProfRequest))
                    .isInstanceOf(EmailNotVerifiedException.class);
        }

        @Test
        @DisplayName("❌ Mauvais mot de passe")
        void login_Professeur_WrongPassword() {
            when(professeurRepository.findByEmail("ikram@ensa.ma")).thenReturn(Optional.of(professeur));
            when(passwordEncoder.matches("Ensa2025@", "hashedPassword")).thenReturn(false);

            assertThatThrownBy(() -> authService.login(loginProfRequest))
                    .isInstanceOf(InvalidPasswordException.class);
        }

        @Test
        @DisplayName("❌ Compte introuvable")
        void login_Professeur_AccountNotFound() {
            when(professeurRepository.findByEmail("inconnu@ensa.ma")).thenReturn(Optional.empty());
            when(etudiantRepository.findByEmail("inconnu@ensa.ma")).thenReturn(Optional.empty());

            LoginRequest req = new LoginRequest();
            req.setEmail("inconnu@ensa.ma");
            req.setPassword("Ensa2025@");

            assertThatThrownBy(() -> authService.login(req))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  LOGIN ETUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Login Etudiant")
    class LoginEtudiantTests {

        @Test
        @DisplayName("✅ Connexion réussie")
        void login_Etudiant_Success() {
            when(professeurRepository.findByEmail("nissrine@ump.ac.ma")).thenReturn(Optional.empty());
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma")).thenReturn(Optional.of(etudiant));
            when(passwordEncoder.matches("Ensa2025@", "hashedPassword")).thenReturn(true);
            when(jwtUtil.generateToken("nissrine@ump.ac.ma", "ETUDIANT")).thenReturn("jwt-token-etudiant");

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
            when(professeurRepository.findByEmail("nissrine@ump.ac.ma")).thenReturn(Optional.empty());
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma")).thenReturn(Optional.of(etudiant));

            assertThatThrownBy(() -> authService.login(loginEtudiantRequest))
                    .isInstanceOf(EmailNotVerifiedException.class);
        }

        @Test
        @DisplayName("❌ Mauvais mot de passe")
        void login_Etudiant_WrongPassword() {
            when(professeurRepository.findByEmail("nissrine@ump.ac.ma")).thenReturn(Optional.empty());
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma")).thenReturn(Optional.of(etudiant));
            when(passwordEncoder.matches("Ensa2025@", "hashedPassword")).thenReturn(false);

            assertThatThrownBy(() -> authService.login(loginEtudiantRequest))
                    .isInstanceOf(InvalidPasswordException.class);
        }

        @Test
        @DisplayName("❌ Compte introuvable")
        void login_Etudiant_AccountNotFound() {
            when(professeurRepository.findByEmail("inconnu@ump.ac.ma")).thenReturn(Optional.empty());
            when(etudiantRepository.findByEmail("inconnu@ump.ac.ma")).thenReturn(Optional.empty());

            LoginRequest req = new LoginRequest();
            req.setEmail("inconnu@ump.ac.ma");
            req.setPassword("Ensa2025@");

            assertThatThrownBy(() -> authService.login(req))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  FORGOT PASSWORD ETUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Forgot Password Etudiant")
    class ForgotPasswordEtudiantTests {

        @Test
        @DisplayName("✅ Email reset envoyé à l'étudiant")
        void forgotPasswordEtudiant_Success() {
            when(professeurRepository.existsByEmail("nissrine@ump.ac.ma")).thenReturn(false);
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma")).thenReturn(Optional.of(etudiant));

            authService.forgotPasswordEtudiant("nissrine@ump.ac.ma");

            verify(etudiantRepository).save(argThat(e ->
                    e.getResetPasswordToken() != null &&
                            e.getResetPasswordExpiry() != null
            ));
            verify(emailService).sendResetPasswordEmail(
                    eq("nissrine@ump.ac.ma"), anyString(), eq("ETUDIANT"));
        }

        @Test
        @DisplayName("❌ Email professeur → silencieux")
        void forgotPasswordEtudiant_ProfEmail_Silent() {
            when(professeurRepository.existsByEmail("ikram@ensa.ma")).thenReturn(true);

            authService.forgotPasswordEtudiant("ikram@ensa.ma");

            verify(etudiantRepository, never()).findByEmail(any());
            verify(etudiantRepository, never()).save(any());
            verify(emailService, never()).sendResetPasswordEmail(any(), any(), any());
        }

        @Test
        @DisplayName("❌ Email inconnu → silencieux")
        void forgotPasswordEtudiant_UnknownEmail_Silent() {
            when(professeurRepository.existsByEmail("inconnu@ump.ac.ma")).thenReturn(false);
            when(etudiantRepository.findByEmail("inconnu@ump.ac.ma")).thenReturn(Optional.empty());

            authService.forgotPasswordEtudiant("inconnu@ump.ac.ma");

            verify(etudiantRepository, never()).save(any());
            verify(emailService, never()).sendResetPasswordEmail(any(), any(), any());
        }

        @Test
        @DisplayName("✅ Token et expiry générés")
        void forgotPasswordEtudiant_TokenGenerated() {
            when(professeurRepository.existsByEmail(anyString())).thenReturn(false);
            when(etudiantRepository.findByEmail(anyString())).thenReturn(Optional.of(etudiant));

            authService.forgotPasswordEtudiant("nissrine@ump.ac.ma");

            verify(etudiantRepository).save(argThat(e ->
                    e.getResetPasswordToken() != null &&
                            !e.getResetPasswordToken().isEmpty() &&
                            e.getResetPasswordExpiry() != null &&
                            e.getResetPasswordExpiry().isAfter(LocalDateTime.now())
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
            when(etudiantRepository.existsByEmail("ikram@ensa.ma")).thenReturn(false);
            when(professeurRepository.findByEmail("ikram@ensa.ma")).thenReturn(Optional.of(professeur));

            authService.forgotPasswordProfesseur("ikram@ensa.ma");

            verify(professeurRepository).save(argThat(p ->
                    p.getResetPasswordToken() != null &&
                            p.getResetPasswordExpiry() != null
            ));
            verify(emailService).sendResetPasswordEmail(
                    eq("ikram@ensa.ma"), anyString(), eq("PROFESSEUR"));
        }

        @Test
        @DisplayName("❌ Email étudiant → silencieux")
        void forgotPasswordProfesseur_EtudiantEmail_Silent() {
            when(etudiantRepository.existsByEmail("nissrine@ump.ac.ma")).thenReturn(true);

            authService.forgotPasswordProfesseur("nissrine@ump.ac.ma");

            verify(professeurRepository, never()).findByEmail(any());
            verify(professeurRepository, never()).save(any());
            verify(emailService, never()).sendResetPasswordEmail(any(), any(), any());
        }

        @Test
        @DisplayName("❌ Email inconnu → silencieux")
        void forgotPasswordProfesseur_UnknownEmail_Silent() {
            when(etudiantRepository.existsByEmail("inconnu@ensa.ma")).thenReturn(false);
            when(professeurRepository.findByEmail("inconnu@ensa.ma")).thenReturn(Optional.empty());

            authService.forgotPasswordProfesseur("inconnu@ensa.ma");

            verify(professeurRepository, never()).save(any());
            verify(emailService, never()).sendResetPasswordEmail(any(), any(), any());
        }
    }

    // ══════════════════════════════════════════════════════
    //  RESET PASSWORD ETUDIANT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("Reset Password Etudiant")
    class ResetPasswordEtudiantTests {

        @Test
        @DisplayName("✅ Réinitialisation réussie")
        void resetPasswordEtudiant_Success() {
            etudiant.setResetPasswordToken("valid-token");
            etudiant.setResetPasswordExpiry(LocalDateTime.now().plusMinutes(10));
            when(etudiantRepository.findByResetPasswordToken("valid-token"))
                    .thenReturn(Optional.of(etudiant));
            when(passwordEncoder.encode("NewPass2025@")).thenReturn("newHashedPassword");

            authService.resetPasswordEtudiant("valid-token", "NewPass2025@", "NewPass2025@");

            verify(etudiantRepository).save(argThat(e ->
                    e.getPassword().equals("newHashedPassword") &&
                            e.getResetPasswordToken() == null &&
                            e.getResetPasswordExpiry() == null
            ));
        }

        @Test
        @DisplayName("❌ Token invalide")
        void resetPasswordEtudiant_InvalidToken() {
            when(etudiantRepository.findByResetPasswordToken("invalid-token"))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    authService.resetPasswordEtudiant("invalid-token", "NewPass2025@", "NewPass2025@")
            ).isInstanceOf(InvalidTokenException.class);

            verify(etudiantRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Token expiré")
        void resetPasswordEtudiant_ExpiredToken() {
            etudiant.setResetPasswordToken("expired-token");
            etudiant.setResetPasswordExpiry(LocalDateTime.now().minusMinutes(5));
            when(etudiantRepository.findByResetPasswordToken("expired-token"))
                    .thenReturn(Optional.of(etudiant));

            assertThatThrownBy(() ->
                    authService.resetPasswordEtudiant("expired-token", "NewPass2025@", "NewPass2025@")
            ).isInstanceOf(InvalidTokenException.class);

            verify(etudiantRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Mots de passe différents")
        void resetPasswordEtudiant_PasswordMismatch() {
            assertThatThrownBy(() ->
                    authService.resetPasswordEtudiant("valid-token", "NewPass2025@", "AutrePass2025@")
            ).isInstanceOf(PasswordMismatchException.class);

            verify(etudiantRepository, never()).findByResetPasswordToken(any());
            verify(etudiantRepository, never()).save(any());
        }

        @Test
        @DisplayName("✅ Token supprimé après reset")
        void resetPasswordEtudiant_TokenClearedAfterReset() {
            etudiant.setResetPasswordToken("valid-token");
            etudiant.setResetPasswordExpiry(LocalDateTime.now().plusMinutes(10));
            when(etudiantRepository.findByResetPasswordToken("valid-token"))
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
            when(passwordEncoder.encode("NewPass2025@")).thenReturn("newHashedPassword");

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