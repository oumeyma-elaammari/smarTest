package com.smartest.backend.service;

import com.smartest.backend.dto.request.RegisterRequest;
import com.smartest.backend.dto.request.RegisterEtudiantRequest;
import com.smartest.backend.dto.LoginRequest;
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
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.crypto.password.PasswordEncoder;

import java.util.Optional;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class AuthServiceTest {

    @Mock
    private ProfesseurRepository professeurRepository;

    @Mock
    private UtilisateurRepository utilisateurRepository;

    @Mock
    private PasswordEncoder passwordEncoder;

    @Mock
    private JwtUtil jwtUtil;

    @InjectMocks
    private AuthService authService;

    private RegisterRequest registerRequest;
    private RegisterEtudiantRequest registerEtudiantRequest;
    private LoginRequest loginRequest;
    private Professeur professeur;
    private Utilisateur etudiant;

    @BeforeEach
    void setUp() {
        registerRequest = new RegisterRequest();
        registerRequest.setNom("Chahlal Ikram");
        registerRequest.setEmail("ikram@ensa.ma");
        registerRequest.setPassword("Ensa2025@");
        registerRequest.setConfirmPassword("Ensa2025@");

        registerEtudiantRequest = new RegisterEtudiantRequest();
        registerEtudiantRequest.setNom("Nissrine El Mniai");
        registerEtudiantRequest.setEmail("nissrine@ump.ac.ma");
        registerEtudiantRequest.setPassword("Ensa2025@");
        registerEtudiantRequest.setConfirmPassword("Ensa2025@");

        loginRequest = new LoginRequest();
        loginRequest.setEmail("ikram@ensa.ma");
        loginRequest.setPassword("Ensa2025@");

        professeur = new Professeur();
        professeur.setId(1L);
        professeur.setNom("Chahlal Ikram");
        professeur.setEmail("ikram@ensa.ma");
        professeur.setPassword("$2a$10$hashedPassword");
        professeur.setRole(Role.PROFESSEUR);

        etudiant = new Utilisateur();
        etudiant.setId(1L);
        etudiant.setNom("Nissrine El Mniai");
        etudiant.setEmail("nissrine@ump.ac.ma");
        etudiant.setPassword("$2a$10$hashedPassword");
        etudiant.setRole(Role.ETUDIANT);
    }

    //  REGISTER PROFESSEUR

    @Test
    @DisplayName("✅ Inscription professeur réussie")
    void register_Success() {
        // GIVEN
        when(professeurRepository.existsByEmail(anyString()))
                .thenReturn(false);
        when(passwordEncoder.encode(anyString()))
                .thenReturn("$2a$10$hashedPassword");
        when(professeurRepository.save(any(Professeur.class)))
                .thenReturn(professeur);

        // WHEN
        String result = authService.register(registerRequest);

        // THEN
        assertEquals("Inscription réussie !", result);
        verify(professeurRepository, times(1)).save(any(Professeur.class));
    }

    @Test
    @DisplayName("❌ Inscription professeur — email déjà utilisé")
    void register_EmailAlreadyUsed() {
        // GIVEN
        when(professeurRepository.existsByEmail(anyString()))
                .thenReturn(true);

        assertThrows(EmailAlreadyUsedException.class, () ->
                authService.register(registerRequest)
        );
        verify(professeurRepository, never()).save(any());
    }

    @Test
    @DisplayName("❌ Inscription professeur — passwords différents")
    void register_PasswordMismatch() {

        registerRequest.setConfirmPassword("AutrePass1!");


        assertThrows(PasswordMismatchException.class, () ->
                authService.register(registerRequest)
        );
        verify(professeurRepository, never()).save(any());
    }


    @Test
    @DisplayName("✅ Inscription étudiant réussie")
    void registerEtudiant_Success() {
        // GIVEN
        when(utilisateurRepository.existsByEmail(anyString()))
                .thenReturn(false);
        when(professeurRepository.existsByEmail(anyString()))
                .thenReturn(false);
        when(passwordEncoder.encode(anyString()))
                .thenReturn("$2a$10$hashedPassword");
        when(utilisateurRepository.save(any(Utilisateur.class)))
                .thenReturn(etudiant);

        // WHEN
        String result = authService.registerEtudiant(registerEtudiantRequest);

        // THEN
        assertEquals("Student registration successful !", result);
        verify(utilisateurRepository, times(1)).save(any(Utilisateur.class));
    }

    @Test
    @DisplayName("❌ Inscription étudiant — email déjà utilisé")
    void registerEtudiant_EmailAlreadyUsed() {
        // GIVEN
        when(utilisateurRepository.existsByEmail(anyString()))
                .thenReturn(true);

        // WHEN + THEN
        assertThrows(EmailAlreadyUsedException.class, () ->
                authService.registerEtudiant(registerEtudiantRequest)
        );
        verify(utilisateurRepository, never()).save(any());
    }

    @Test
    @DisplayName("❌ Inscription étudiant — passwords différents")
    void registerEtudiant_PasswordMismatch() {
        // GIVEN
        registerEtudiantRequest.setConfirmPassword("AutrePass1!");

        // WHEN + THEN
        assertThrows(PasswordMismatchException.class, () ->
                authService.registerEtudiant(registerEtudiantRequest)
        );
    }

    // ══════════════════════════════════════════════════════════
    //  LOGIN
    // ══════════════════════════════════════════════════════════

    @Test
    @DisplayName("✅ Login professeur réussi")
    void login_Professor_Success() {
        // GIVEN
        when(professeurRepository.findByEmail(anyString()))
                .thenReturn(Optional.of(professeur));
        when(passwordEncoder.matches(anyString(), anyString()))
                .thenReturn(true);
        when(jwtUtil.generateToken(anyString(), anyString()))
                .thenReturn("eyJhbGci...");

        // WHEN
        AuthResponse response = authService.login(loginRequest);

        // THEN
        assertNotNull(response);
        assertEquals("PROFESSEUR", response.getRole());
        assertEquals("eyJhbGci...", response.getToken());
        assertEquals("ikram@ensa.ma", response.getEmail());
    }

    @Test
    @DisplayName("✅ Login étudiant réussi")
    void login_Student_Success() {
        // GIVEN
        loginRequest.setEmail("nissrine@ump.ac.ma");
        when(professeurRepository.findByEmail(anyString()))
                .thenReturn(Optional.empty());
        when(utilisateurRepository.findByEmail(anyString()))
                .thenReturn(Optional.of(etudiant));
        when(passwordEncoder.matches(anyString(), anyString()))
                .thenReturn(true);
        when(jwtUtil.generateToken(anyString(), anyString()))
                .thenReturn("eyJhbGci...");

        // WHEN
        AuthResponse response = authService.login(loginRequest);

        // THEN
        assertNotNull(response);
        assertEquals("ETUDIANT", response.getRole());
        assertEquals("eyJhbGci...", response.getToken());
    }

    @Test
    @DisplayName("❌ Login — mauvais mot de passe")
    void login_InvalidPassword() {
        // GIVEN
        when(professeurRepository.findByEmail(anyString()))
                .thenReturn(Optional.of(professeur));
        when(passwordEncoder.matches(anyString(), anyString()))
                .thenReturn(false);
        // ↑ simule un mauvais mot de passe

        // WHEN + THEN
        assertThrows(InvalidPasswordException.class, () ->
                authService.login(loginRequest)
        );
    }

    @Test
    @DisplayName("❌ Login — compte introuvable")
    void login_AccountNotFound() {
        // GIVEN
        when(professeurRepository.findByEmail(anyString()))
                .thenReturn(Optional.empty());
        when(utilisateurRepository.findByEmail(anyString()))
                .thenReturn(Optional.empty());
        // ↑ email introuvable dans les deux tables

        // WHEN + THEN
        assertThrows(AccountNotFoundException.class, () ->
                authService.login(loginRequest)
        );
    }

    @Test
    @DisplayName("❌ Login étudiant — mauvais mot de passe")
    void login_Student_InvalidPassword() {
        // GIVEN
        loginRequest.setEmail("nissrine@ump.ac.ma");
        when(professeurRepository.findByEmail(anyString()))
                .thenReturn(Optional.empty());
        when(utilisateurRepository.findByEmail(anyString()))
                .thenReturn(Optional.of(etudiant));
        when(passwordEncoder.matches(anyString(), anyString()))
                .thenReturn(false);

        // WHEN + THEN
        assertThrows(InvalidPasswordException.class, () ->
                authService.login(loginRequest)
        );
    }
}