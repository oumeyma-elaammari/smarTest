package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateProfesseurRequest;
import com.smartest.backend.dto.response.ProfesseurResponse;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.service.ProfesseurService;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("ProfesseurController — Tests complets")
class ProfesseurControllerTest {

    @Mock private ProfesseurService professeurService;
    @InjectMocks private ProfesseurController professeurController;

    private UserDetails professeurConnecte;

    @BeforeEach
    void setUp() {
        professeurConnecte = User.withUsername("ikram@ensa.ma")
                .password("hashedPassword")
                .roles("PROFESSEUR")
                .build();
    }

    // ══════════════════════════════════════════════════════
    //  GET PROFIL
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("GET /api/professeur/profil")
    class GetProfilTests {

        @Test
        @DisplayName("✅ 200 — Profil retourné correctement")
        void getProfil_Returns200() {
            ProfesseurResponse expected = new ProfesseurResponse(
                    1L, "Ikram", "ikram@ensa.ma", true);
            when(professeurService.getProfile("ikram@ensa.ma"))
                    .thenReturn(expected);

            ResponseEntity<ProfesseurResponse> response =
                    professeurController.getProfil(professeurConnecte);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody()).isNotNull();
            assertThat(response.getBody().getEmail()).isEqualTo("ikram@ensa.ma");
            assertThat(response.getBody().getNom()).isEqualTo("Ikram");
        }

        @Test
        @DisplayName("❌ 404 — Professeur introuvable")
        void getProfil_ThrowsIfNotFound() {
            when(professeurService.getProfile(anyString()))
                    .thenThrow(new AccountNotFoundException("ikram@ensa.ma"));

            assertThatThrownBy(() ->
                    professeurController.getProfil(professeurConnecte))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  UPDATE PROFIL
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("PUT /api/professeur/profil")
    class UpdateProfilTests {

        @Test
        @DisplayName("✅ 200 — Nom mis à jour")
        void updateProfil_Returns200() {
            UpdateProfesseurRequest request = new UpdateProfesseurRequest();
            request.setNom("Ikram Updated");

            ProfesseurResponse expected = new ProfesseurResponse(
                    1L, "Ikram Updated", "ikram@ensa.ma", true);
            when(professeurService.updateProfile(anyString(), any()))
                    .thenReturn(expected);

            ResponseEntity<ProfesseurResponse> response =
                    professeurController.updateProfil(professeurConnecte, request);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody().getNom()).isEqualTo("Ikram Updated");
        }

        @Test
        @DisplayName("✅ Service appelé avec le bon email")
        void updateProfil_CallsServiceWithCorrectEmail() {
            UpdateProfesseurRequest request = new UpdateProfesseurRequest();
            request.setNom("Nouveau Nom");

            when(professeurService.updateProfile(anyString(), any()))
                    .thenReturn(new ProfesseurResponse());

            professeurController.updateProfil(professeurConnecte, request);

            verify(professeurService).updateProfile(
                    eq("ikram@ensa.ma"), any());
        }
    }

    // ══════════════════════════════════════════════════════
    //  CHANGE PASSWORD
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("PUT /api/professeur/change-password")
    class ChangePasswordTests {

        @Test
        @DisplayName("✅ 200 — Mot de passe changé")
        void changePassword_Returns200() {
            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("OldPass2025@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("NewPass2025@");

            doNothing().when(professeurService)
                    .changePassword(anyString(), any());

            ResponseEntity<String> response =
                    professeurController.changePassword(professeurConnecte, request);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody())
                    .isEqualTo("Mot de passe modifié avec succès.");
        }

        @Test
        @DisplayName("❌ Exception si ancien mot de passe incorrect")
        void changePassword_ThrowsIfOldPasswordWrong() {
            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("Mauvais@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("NewPass2025@");

            doThrow(new InvalidPasswordException())
                    .when(professeurService).changePassword(anyString(), any());

            assertThatThrownBy(() ->
                    professeurController.changePassword(professeurConnecte, request))
                    .isInstanceOf(InvalidPasswordException.class);
        }

        @Test
        @DisplayName("❌ Exception si confirmation incorrecte")
        void changePassword_ThrowsIfPasswordMismatch() {
            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("OldPass2025@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("Different@");

            doThrow(new PasswordMismatchException())
                    .when(professeurService).changePassword(anyString(), any());

            assertThatThrownBy(() ->
                    professeurController.changePassword(professeurConnecte, request))
                    .isInstanceOf(PasswordMismatchException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  DELETE COMPTE
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("DELETE /api/professeur/compte")
    class DeleteCompteTests {

        @Test
        @DisplayName("✅ 200 — Compte supprimé")
        void deleteCompte_Returns200() {
            doNothing().when(professeurService).deleteAccount(anyString());

            ResponseEntity<String> response =
                    professeurController.deleteCompte(professeurConnecte);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody())
                    .isEqualTo("Compte supprimé avec succès.");
        }

        @Test
        @DisplayName("✅ Service appelé avec le bon email")
        void deleteCompte_CallsServiceWithCorrectEmail() {
            doNothing().when(professeurService).deleteAccount(anyString());

            professeurController.deleteCompte(professeurConnecte);

            verify(professeurService).deleteAccount("ikram@ensa.ma");
        }
    }
}