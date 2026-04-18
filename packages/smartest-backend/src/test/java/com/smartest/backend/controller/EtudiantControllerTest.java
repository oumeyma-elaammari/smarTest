package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateEtudiantRequest;
import com.smartest.backend.dto.response.EtudiantResponse;
import com.smartest.backend.exception.AccountNotFoundException;
import com.smartest.backend.exception.InvalidPasswordException;
import com.smartest.backend.exception.PasswordMismatchException;
import com.smartest.backend.service.EtudiantService;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;

import java.util.List;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("EtudiantController — Tests complets")
class EtudiantControllerTest {

    @Mock private EtudiantService etudiantService;
    @InjectMocks private EtudiantController etudiantController;

    // UserDetails simulant un étudiant connecté
    private UserDetails etudiantConnecte;

    @BeforeEach
    void setUp() {
        etudiantConnecte = User.withUsername("nissrine@ump.ac.ma")
                .password("hashedPassword")
                .roles("ETUDIANT")
                .build();
    }

    // ══════════════════════════════════════════════════════
    //  GET PROFIL
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("GET /api/etudiant/profil")
    class GetProfilTests {

        @Test
        @DisplayName("✅ 200 — Profil retourné correctement")
        void getProfil_Returns200() {
            EtudiantResponse expected = new EtudiantResponse(
                    1L, "Nissrine", "nissrine@ump.ac.ma", true);
            when(etudiantService.getProfile("nissrine@ump.ac.ma"))
                    .thenReturn(expected);

            ResponseEntity<EtudiantResponse> response =
                    etudiantController.getProfil(etudiantConnecte);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody()).isNotNull();
            assertThat(response.getBody().getEmail()).isEqualTo("nissrine@ump.ac.ma");
            assertThat(response.getBody().getNom()).isEqualTo("Nissrine");
        }

        @Test
        @DisplayName("❌ 404 — Étudiant introuvable")
        void getProfil_ThrowsIfNotFound() {
            when(etudiantService.getProfile(anyString()))
                    .thenThrow(new AccountNotFoundException("nissrine@ump.ac.ma"));

            assertThatThrownBy(() ->
                    etudiantController.getProfil(etudiantConnecte))
                    .isInstanceOf(AccountNotFoundException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  UPDATE PROFIL
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("PUT /api/etudiant/profil")
    class UpdateProfilTests {

        @Test
        @DisplayName("✅ 200 — Nom mis à jour")
        void updateProfil_Returns200() {
            UpdateEtudiantRequest request = new UpdateEtudiantRequest();
            request.setNom("Nissrine Updated");

            EtudiantResponse expected = new EtudiantResponse(
                    1L, "Nissrine Updated", "nissrine@ump.ac.ma", true);
            when(etudiantService.updateProfile(anyString(), any()))
                    .thenReturn(expected);

            ResponseEntity<EtudiantResponse> response =
                    etudiantController.updateProfil(etudiantConnecte, request);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody().getNom()).isEqualTo("Nissrine Updated");
        }

        @Test
        @DisplayName("✅ Service appelé avec le bon email")
        void updateProfil_CallsServiceWithCorrectEmail() {
            UpdateEtudiantRequest request = new UpdateEtudiantRequest();
            request.setNom("Nouveau Nom");

            when(etudiantService.updateProfile(anyString(), any()))
                    .thenReturn(new EtudiantResponse());

            etudiantController.updateProfil(etudiantConnecte, request);

            verify(etudiantService).updateProfile(
                    eq("nissrine@ump.ac.ma"), any());
        }
    }

    // ══════════════════════════════════════════════════════
    //  CHANGE PASSWORD
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("PUT /api/etudiant/change-password")
    class ChangePasswordTests {

        @Test
        @DisplayName("✅ 200 — Mot de passe changé")
        void changePassword_Returns200() {
            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("OldPass2025@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("NewPass2025@");

            doNothing().when(etudiantService)
                    .changePassword(anyString(), any());

            ResponseEntity<String> response =
                    etudiantController.changePassword(etudiantConnecte, request);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody())
                    .isEqualTo("Mot de passe modifié avec succès.");
        }

        @Test
        @DisplayName("❌ Exception si ancien mot de passe incorrect")
        void changePassword_ThrowsIfOldPasswordWrong() {
            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("MauvaisAncien@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("NewPass2025@");

            doThrow(new InvalidPasswordException())
                    .when(etudiantService).changePassword(anyString(), any());

            assertThatThrownBy(() ->
                    etudiantController.changePassword(etudiantConnecte, request))
                    .isInstanceOf(InvalidPasswordException.class);
        }

        @Test
        @DisplayName("❌ Exception si nouveaux mots de passe différents")
        void changePassword_ThrowsIfPasswordMismatch() {
            ChangePasswordRequest request = new ChangePasswordRequest();
            request.setOldPassword("OldPass2025@");
            request.setNewPassword("NewPass2025@");
            request.setConfirmPassword("Different2025@");

            doThrow(new PasswordMismatchException())
                    .when(etudiantService).changePassword(anyString(), any());

            assertThatThrownBy(() ->
                    etudiantController.changePassword(etudiantConnecte, request))
                    .isInstanceOf(PasswordMismatchException.class);
        }
    }

    // ══════════════════════════════════════════════════════
    //  DELETE COMPTE
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("DELETE /api/etudiant/compte")
    class DeleteCompteTests {

        @Test
        @DisplayName("✅ 200 — Compte supprimé")
        void deleteCompte_Returns200() {
            doNothing().when(etudiantService).deleteAccount(anyString());

            ResponseEntity<String> response =
                    etudiantController.deleteCompte(etudiantConnecte);

            assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
            assertThat(response.getBody())
                    .isEqualTo("Compte supprimé avec succès.");
        }

        @Test
        @DisplayName("✅ Service appelé avec le bon email")
        void deleteCompte_CallsServiceWithCorrectEmail() {
            doNothing().when(etudiantService).deleteAccount(anyString());

            etudiantController.deleteCompte(etudiantConnecte);

            verify(etudiantService).deleteAccount("nissrine@ump.ac.ma");
        }
    }
}