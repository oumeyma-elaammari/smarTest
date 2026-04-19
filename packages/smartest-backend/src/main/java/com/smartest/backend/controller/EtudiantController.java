package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ChangePasswordRequest;
import com.smartest.backend.dto.request.UpdateEtudiantRequest;
import com.smartest.backend.dto.response.EtudiantResponse;
import com.smartest.backend.service.EtudiantService;
import jakarta.validation.Valid;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/etudiant")
@CrossOrigin(origins = "*")
public class EtudiantController {

    private final EtudiantService etudiantService;

    public EtudiantController(EtudiantService etudiantService) {
        this.etudiantService = etudiantService;
    }

    // GET /api/etudiant/profil
    @GetMapping("/profil")
    public ResponseEntity<EtudiantResponse> getProfil(
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(
                etudiantService.getProfile(userDetails.getUsername()));
    }

    // PUT /api/etudiant/profil
    @PutMapping("/profil")
    public ResponseEntity<EtudiantResponse> updateProfil(
            @AuthenticationPrincipal UserDetails userDetails,
            @Valid @RequestBody UpdateEtudiantRequest request) {
        return ResponseEntity.ok(
                etudiantService.updateProfile(userDetails.getUsername(), request));
    }

    // PUT /api/etudiant/change-password
    @PutMapping("/change-password")
    public ResponseEntity<String> changePassword(
            @AuthenticationPrincipal UserDetails userDetails,
            @Valid @RequestBody ChangePasswordRequest request) {
        etudiantService.changePassword(userDetails.getUsername(), request);
        return ResponseEntity.ok("Mot de passe modifié avec succès.");
    }

    // DELETE /api/etudiant/compte
    @DeleteMapping("/compte")
    public ResponseEntity<String> deleteCompte(
            @AuthenticationPrincipal UserDetails userDetails) {
        etudiantService.deleteAccount(userDetails.getUsername());
        return ResponseEntity.ok("Compte supprimé avec succès.");
    }
}