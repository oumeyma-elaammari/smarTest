package com.smartest.backend.controller;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.smartest.backend.dto.request.SessionExamenRequest;
import com.smartest.backend.dto.response.MessageResponse;
import com.smartest.backend.dto.response.SessionExamenResponse;
import com.smartest.backend.service.SessionExamenService;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("/api/sessions-examen")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class SessionExamenController {

    private final SessionExamenService sessionExamenService;

    /**
     * GET /api/sessions-examen - Récupérer toutes les sessions
     */
    @GetMapping
    public ResponseEntity<List<SessionExamenResponse>> getAllSessions() {
        return ResponseEntity.ok(sessionExamenService.getAllSessions());
    }

    /**
     * GET /api/sessions-examen/{id} - Récupérer une session par son ID
     */
    @GetMapping("/{id}")
    public ResponseEntity<SessionExamenResponse> getSessionById(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.getSessionById(id));
    }

    /**
     * GET /api/sessions-examen/examen/{examenId} - Sessions d'un examen
     */
    @GetMapping("/examen/{examenId}")
    public ResponseEntity<List<SessionExamenResponse>> getSessionsByExamen(@PathVariable Long examenId) {
        return ResponseEntity.ok(sessionExamenService.getSessionsByExamen(examenId));
    }

    /**
     * GET /api/sessions-examen/statut/en-cours - Sessions en cours
     */
    @GetMapping("/statut/en-cours")
    public ResponseEntity<List<SessionExamenResponse>> getSessionsEnCours() {
        return ResponseEntity.ok(sessionExamenService.getSessionsEnCours());
    }

    /**
     * GET /api/sessions-examen/statut/a-venir - Sessions à venir
     */
    @GetMapping("/statut/a-venir")
    public ResponseEntity<List<SessionExamenResponse>> getSessionsAFaire() {
        return ResponseEntity.ok(sessionExamenService.getSessionsAFaire());
    }

    /**
     * POST /api/sessions-examen - Créer une nouvelle session
     */
    @PostMapping
    public ResponseEntity<SessionExamenResponse> createSession(@Valid @RequestBody SessionExamenRequest request) {
        SessionExamenResponse createdSession = sessionExamenService.createSession(request);
        return new ResponseEntity<>(createdSession, HttpStatus.CREATED);
    }

    /**
     * PUT /api/sessions-examen/{id} - Mettre à jour une session
     */
    @PutMapping("/{id}")
    public ResponseEntity<SessionExamenResponse> updateSession(@PathVariable Long id, @Valid @RequestBody SessionExamenRequest request) {
        return ResponseEntity.ok(sessionExamenService.updateSession(id, request));
    }

    /**
     * PATCH /api/sessions-examen/{id}/demarrer - Démarrer une session
     */
    @PatchMapping("/{id}/demarrer")
    public ResponseEntity<SessionExamenResponse> demarrerSession(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.demarrerSession(id));
    }

    /**
     * PATCH /api/sessions-examen/{id}/terminer - Terminer une session
     */
    @PatchMapping("/{id}/terminer")
    public ResponseEntity<SessionExamenResponse> terminerSession(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.terminerSession(id));
    }

    /**
     * PATCH /api/sessions-examen/{id}/annuler - Annuler une session
     */
    @PatchMapping("/{id}/annuler")
    public ResponseEntity<SessionExamenResponse> annulerSession(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.annulerSession(id));
    }

    /**
     * DELETE /api/sessions-examen/{id} - Supprimer une session
     */
    @DeleteMapping("/{id}")
    public ResponseEntity<MessageResponse> deleteSession(@PathVariable Long id) {
        sessionExamenService.deleteSession(id);
        return ResponseEntity.ok(MessageResponse.success("Session d'examen supprimée avec succès"));
    }

    /**
     * GET /api/sessions-examen/examen/{examenId}/en-cours - Vérifier si
     * l'examen a une session en cours
     */
    @GetMapping("/examen/{examenId}/en-cours")
    public ResponseEntity<Boolean> isExamenEnCours(@PathVariable Long examenId) {
        return ResponseEntity.ok(sessionExamenService.isExamenEnCours(examenId));
    }
}
