package com.smartest.backend.controller;

import java.util.List;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

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

    // ========================= GET =========================

    @GetMapping
    public ResponseEntity<List<SessionExamenResponse>> getAllSessions() {
        return ResponseEntity.ok(sessionExamenService.getAllSessions());
    }

    @GetMapping("/{id}")
    public ResponseEntity<SessionExamenResponse> getSessionById(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.getSessionById(id));
    }

    // 🔥 CHANGEMENT : examenPublieId
    @GetMapping("/examen-publie/{examenPublieId}")
    public ResponseEntity<List<SessionExamenResponse>> getSessionsByExamenPublie(
            @PathVariable Long examenPublieId) {

        return ResponseEntity.ok(
                sessionExamenService.getSessionsByExamenPublie(examenPublieId)
        );
    }

   /* @GetMapping("/statut/en-cours")
    public ResponseEntity<List<SessionExamenResponse>> getSessionsEnCours() {
        return ResponseEntity.ok(sessionExamenService.getSessionsEnCours());
    }*/

    @GetMapping("/statut/a-venir")
    public ResponseEntity<List<SessionExamenResponse>> getSessionsAFaire() {
        return ResponseEntity.ok(sessionExamenService.getSessionsAFaire());
    }

    // ========================= CREATE =========================

    @PostMapping
    public ResponseEntity<SessionExamenResponse> createSession(
            @Valid @RequestBody SessionExamenRequest request) {

        return ResponseEntity.ok(sessionExamenService.createSession(request));
    }

    // ========================= UPDATE =========================

    @PutMapping("/{id}")
    public ResponseEntity<SessionExamenResponse> updateSession(
            @PathVariable Long id,
            @Valid @RequestBody SessionExamenRequest request) {

        return ResponseEntity.ok(sessionExamenService.updateSession(id, request));
    }

    // ========================= STATUS =========================

    @PatchMapping("/{id}/demarrer")
    public ResponseEntity<SessionExamenResponse> demarrerSession(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.demarrerSession(id));
    }

    @PatchMapping("/{id}/terminer")
    public ResponseEntity<SessionExamenResponse> terminerSession(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.terminerSession(id));
    }

    @PatchMapping("/{id}/annuler")
    public ResponseEntity<SessionExamenResponse> annulerSession(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.annulerSession(id));
    }

    // ========================= DELETE =========================

    @DeleteMapping("/{id}")
    public ResponseEntity<MessageResponse> deleteSession(@PathVariable Long id) {
        sessionExamenService.deleteSession(id);
        return ResponseEntity.ok(
                MessageResponse.success("Session supprimée avec succès")
        );
    }

    // ========================= CHECK =========================

  /*  @GetMapping("/examen-publie/{id}/en-cours")
    public ResponseEntity<Boolean> isExamenEnCours(@PathVariable Long id) {
        return ResponseEntity.ok(sessionExamenService.isExamenEnCours(id));
    }*/

    // ========================= CORRECTION =========================

    @PostMapping("/{sessionId}/corriger")
    public ResponseEntity<Double> corrigerExamen(@PathVariable Long sessionId) {
        return ResponseEntity.ok(sessionExamenService.corrigerExamen(sessionId));
    }
}