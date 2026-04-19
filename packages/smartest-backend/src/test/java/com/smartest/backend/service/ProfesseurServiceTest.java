package com.smartest.backend.service;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateProfesseurRequest;
import com.smartest.backend.dto.response.ProfesseurResponse;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.repository.ProfesseurRepository;
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
@DisplayName("ProfesseurService — Tests complets")
class ProfesseurServiceTest {

    @Mock private ProfesseurRepository professeurRepository;
    @Mock private PasswordEncoder passwordEncoder;
    @InjectMocks private ProfesseurService professeurService;

    private Professeur professeur;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setId(1L);
        professeur.setNom("Ikram");
        professeur.setEmail("ikram@ensa.ma");
        professeur.setPassword("hashedOldPass");
        professeur.setEmailVerifie(true);
    }

    @Nested
    @DisplayName("getProfile")
    class GetProfileTests {

        @Test
        @DisplayName("✅ Retourne le profil correctement")
        void getProfile_Success() {
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));

            ProfesseurResponse response =
                    professeurService.getProfile("ikram@ensa.ma");

            assertThat(response.getEmail()).isEqualTo("ikram@ensa.ma");
            assertThat(response.getNom()).isEqualTo("Ikram");
            assertThat(response.isEmailVerifie()).isTrue();
        }

        @Test
        @DisplayName("❌ Lève AccountNotFoundException si email inconnu")
        void getProfile_ThrowsIfNotFound() {
            when(professeurRepository.findByEmail(anyString()))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    professeurService.getProfile("inconnu@ensa.ma"))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    @Nested
    @DisplayName("updateProfile")
    class UpdateProfileTests {

        @Test
        @DisplayName("✅ Nom mis à jour correctement")
        void updateProfile_UpdatesNom() {
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));
            when(professeurRepository.save(any())).thenReturn(professeur);

            UpdateProfesseurRequest request = new UpdateProfesseurRequest();
            request.setNom("Ikram Updated");

            ProfesseurResponse response =
                    professeurService.updateProfile("ikram@ensa.ma", request);

            assertThat(response.getNom()).isEqualTo("Ikram Updated");
            verify(professeurRepository).save(professeur);
        }

        @Test
        @DisplayName("✅ Nom ignoré si vide")
        void updateProfile_IgnoresBlankNom() {
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));
            when(professeurRepository.save(any())).thenReturn(professeur);

            UpdateProfesseurRequest request = new UpdateProfesseurRequest();
            request.setNom("   ");

            professeurService.updateProfile("ikram@ensa.ma", request);

            assertThat(professeur.getNom()).isEqualTo("Ikram"); // inchangé
        }

        @Test
        @DisplayName("❌ Lève AccountNotFoundException si email inconnu")
        void updateProfile_ThrowsIfNotFound() {
            when(professeurRepository.findByEmail(anyString()))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    professeurService.updateProfile("inconnu@ensa.ma",
                            new UpdateProfesseurRequest()))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    @Nested
    @DisplayName("changePassword")
    class ChangePasswordTests {

        @Test
        @DisplayName("✅ Mot de passe changé avec succès")
        void changePassword_Success() {
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));
            when(passwordEncoder.matches("OldPass2025@", "hashedOldPass"))
                    .thenReturn(true);
            when(passwordEncoder.encode("NewPass2025@"))
                    .thenReturn("hashedNewPass");

            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("OldPass2025@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("NewPass2025@");

            professeurService.changePassword("ikram@ensa.ma", request);

            assertThat(professeur.getPassword()).isEqualTo("hashedNewPass");
            verify(professeurRepository).save(professeur);
        }

        @Test
        @DisplayName("❌ Lève PasswordMismatchException si confirmation incorrecte")
        void changePassword_ThrowsIfMismatch() {
            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("OldPass2025@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("Different@");

            assertThatThrownBy(() ->
                    professeurService.changePassword("ikram@ensa.ma", request))
                    .isInstanceOf(PasswordMismatchException.class);

            verify(professeurRepository, never()).save(any());
        }

        @Test
        @DisplayName("❌ Lève InvalidPasswordException si ancien mdp incorrect")
        void changePassword_ThrowsIfOldPasswordWrong() {
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));
            when(passwordEncoder.matches("Mauvais@", "hashedOldPass"))
                    .thenReturn(false);

            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("Mauvais@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("NewPass2025@");

            assertThatThrownBy(() ->
                    professeurService.changePassword("ikram@ensa.ma", request))
                    .isInstanceOf(InvalidPasswordException.class);
        }
    }

    @Nested
    @DisplayName("deleteAccount")
    class DeleteAccountTests {

        @Test
        @DisplayName("✅ Compte supprimé avec succès")
        void deleteAccount_Success() {
            when(professeurRepository.findByEmail("ikram@ensa.ma"))
                    .thenReturn(Optional.of(professeur));
            doNothing().when(professeurRepository).delete(professeur);

            professeurService.deleteAccount("ikram@ensa.ma");

            verify(professeurRepository).delete(professeur);
        }

        @Test
        @DisplayName("❌ Lève AccountNotFoundException si email inconnu")
        void deleteAccount_ThrowsIfNotFound() {
            when(professeurRepository.findByEmail(anyString()))
                    .thenReturn(Optional.empty());

            assertThatThrownBy(() ->
                    professeurService.deleteAccount("inconnu@ensa.ma"))
                    .isInstanceOf(AccountNotFoundException.class);

            verify(professeurRepository, never()).delete(any());
        }
    }
}