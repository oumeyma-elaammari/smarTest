package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateProfesseurRequest;
import com.smartest.backend.dto.response.QuizProfesseurListeResponse;
import com.smartest.backend.dto.response.ProfesseurResponse;
import com.smartest.backend.service.ProfesseurService;
import jakarta.validation.Valid;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/professeur")
@CrossOrigin(origins = "*")
public class ProfesseurController {

    private final ProfesseurService professeurService;

    public ProfesseurController(ProfesseurService professeurService) {
        this.professeurService = professeurService;
    }

    // GET /api/professeur/profil
    @GetMapping("/profil")
    public ResponseEntity<ProfesseurResponse> getProfil(
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(
                professeurService.getProfile(userDetails.getUsername()));
    }

    /**
     * Quiz du professeur sur le serveur (table {@code quiz}) — aide le bureau à corréler
     * avant {@code DELETE /api/quizs/{id}} quand {@code BackendQuizId} n’est pas enregistré en local.
     */
    @GetMapping("/mes-quiz")
    public ResponseEntity<List<QuizProfesseurListeResponse>> mesQuiz(
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(
                professeurService.listMesQuiz(userDetails.getUsername()));
    }

    // PUT /api/professeur/profil
    @PutMapping("/profil")
    public ResponseEntity<ProfesseurResponse> updateProfil(
            @AuthenticationPrincipal UserDetails userDetails,
            @Valid @RequestBody UpdateProfesseurRequest request) {
        return ResponseEntity.ok(
                professeurService.updateProfile(userDetails.getUsername(), request));
    }

    // PUT /api/professeur/change-password
    @PutMapping("/change-password")
    public ResponseEntity<String> changePassword(
            @AuthenticationPrincipal UserDetails userDetails,
            @Valid @RequestBody ChangePasswordRequest request) {
        professeurService.changePassword(userDetails.getUsername(), request);
        return ResponseEntity.ok("Mot de passe modifié avec succès.");
    }

    // DELETE /api/professeur/compte
    @DeleteMapping("/compte")
    public ResponseEntity<String> deleteCompte(
            @AuthenticationPrincipal UserDetails userDetails) {
        professeurService.deleteAccount(userDetails.getUsername());
        return ResponseEntity.ok("Compte supprimé avec succès.");
    }
}