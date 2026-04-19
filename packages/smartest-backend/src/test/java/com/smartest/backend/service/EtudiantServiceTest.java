package com.smartest.backend.service;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateEtudiantRequest;
import com.smartest.backend.dto.response.EtudiantResponse;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.repository.EtudiantRepository;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.crypto.password.PasswordEncoder;

import java.util.Optional;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("EtudiantService — Tests complets")
class EtudiantServiceTest {

    @Mock private EtudiantRepository etudiantRepository;
    @Mock private PasswordEncoder passwordEncoder;
    @InjectMocks private EtudiantService etudiantService;

    private Etudiant etudiant;

    @BeforeEach
    void setUp() {
        etudiant = new Etudiant();
        etudiant.setId(1L);
        etudiant.setNom("Nissrine");
        etudiant.setEmail("nissrine@ump.ac.ma");
        etudiant.setPassword("hashedOldPass");
        etudiant.setEmailVerifie(true);
    }

    // ══════════════════════════════════════════════════════
    //  GET PROFILE
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("getProfile")
    class GetProfileTests {

        @Test
        @DisplayName("✅ Retourne le profil correctement")
        void getProfile_Success() {
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));

            EtudiantResponse response =
                    etudiantService.getProfile("nissrine@ump.ac.ma");

            assertThat(response.getEmail()).isEqualTo("nissrine@ump.ac.ma");
            assertThat(response.getNom()).isEqualTo("Nissrine");
            assertThat(response.isEmailVerifie()).isTrue();
        }

        @Test
        @DisplayName("❌ Lève AccountNotFoundException si email inconnu")
        void getProfile_ThrowsIfNotFound() {
            when(etudiantRepository.findByEmail(anyString()))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    etudiantService.getProfile("inconnu@ump.ac.ma"))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  UPDATE PROFILE
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("updateProfile")
    class UpdateProfileTests {

        @Test
        @DisplayName("✅ Nom mis à jour correctement")
        void updateProfile_UpdatesNom() {
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));
            when(etudiantRepository.save(any())).thenReturn(etudiant);

            UpdateEtudiantRequest request = new UpdateEtudiantRequest();
            request.setNom("Nissrine Updated");

            EtudiantResponse response =
                    etudiantService.updateProfile("nissrine@ump.ac.ma", request);

            assertThat(response.getNom()).isEqualTo("Nissrine Updated");
            verify(etudiantRepository).save(etudiant);
        }

        @Test
        @DisplayName("✅ Nom ignoré si vide")
        void updateProfile_IgnoresBlankNom() {
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));
            when(etudiantRepository.save(any())).thenReturn(etudiant);

            UpdateEtudiantRequest request = new UpdateEtudiantRequest();
            request.setNom("  ");

            etudiantService.updateProfile("nissrine@ump.ac.ma", request);

            assertThat(etudiant.getNom()).isEqualTo("Nissrine"); // inchangé
        }

        @Test
        @DisplayName("❌ Lève AccountNotFoundException si email inconnu")
        void updateProfile_ThrowsIfNotFound() {
            when(etudiantRepository.findByEmail(anyString()))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    etudiantService.updateProfile("inconnu@ump.ac.ma",
                            new UpdateEtudiantRequest()))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  CHANGE PASSWORD
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("changePassword")
    class ChangePasswordTests {

        @Test
        @DisplayName("✅ Mot de passe changé avec succès")
        void changePassword_Success() {
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));
            when(passwordEncoder.matches("OldPass2025@", "hashedOldPass"))
                    .thenReturn(true);
            when(passwordEncoder.encode("NewPass2025@"))
                    .thenReturn("hashedNewPass");

            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("OldPass2025@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("NewPass2025@");

            etudiantService.changePassword("nissrine@ump.ac.ma", request);

            assertThat(etudiant.getPassword()).isEqualTo("hashedNewPass");
            verify(etudiantRepository).save(etudiant);
        }

        @Test
        @DisplayName("❌ Lève PasswordMismatchException si confirmation incorrecte")
        void changePassword_ThrowsIfMismatch() {
            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("OldPass2025@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("Different@");

            assertThatThrownBy(() ->
                    etudiantService.changePassword("nissrine@ump.ac.ma", request))
                    .isInstanceOf(PasswordMismatchException.class);

            verify(etudiantRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Lève InvalidPasswordException si ancien mdp incorrect")
        void changePassword_ThrowsIfOldPasswordWrong() {
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));
            when(passwordEncoder.matches("MauvaisAncien@", "hashedOldPass"))
                    .thenReturn(false);

            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("MauvaisAncien@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("NewPass2025@");

            assertThatThrownBy(() ->
                    etudiantService.changePassword("nissrine@ump.ac.ma", request))
                    .isInstanceOf(InvalidPasswordException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  DELETE ACCOUNT
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("deleteAccount")
    class DeleteAccountTests {

        @Test
        @DisplayName("✅ Compte supprimé avec succès")
        void deleteAccount_Success() {
            when(etudiantRepository.findByEmail("nissrine@ump.ac.ma"))
                    .thenReturn(Optional.of(etudiant));
            doNothing().when(etudiantRepository).delete(etudiant);

            etudiantService.deleteAccount("nissrine@ump.ac.ma");

            verify(etudiantRepository).delete(etudiant);
        }

        @Test
        @DisplayName("❌ Lève AccountNotFoundException si email inconnu")
        void deleteAccount_ThrowsIfNotFound() {
            when(etudiantRepository.findByEmail(anyString()))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    etudiantService.deleteAccount("inconnu@ump.ac.ma"))
                    .isInstanceOf(AccountNotFoundException.class);

            verify(etudiantRepository, never()).delete(any());
        }
    }
}