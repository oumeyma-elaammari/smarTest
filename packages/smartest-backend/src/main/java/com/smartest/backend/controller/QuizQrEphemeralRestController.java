package com.smartest.backend.controller;

import com.smartest.backend.dto.qr.QrLivePublicReponseRequest;
import com.smartest.backend.dto.qr.QrLiveSessionCreatedResponse;
import com.smartest.backend.dto.qr.QrLiveSubmitAnswerResponse;
import com.smartest.backend.dto.qr.QrLiveStreamEnvelope;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.service.QuizSessionManager;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;
import jakarta.validation.Valid;

@RestController
@RequestMapping("/api/qr-live")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class QuizQrEphemeralRestController {

    private final QuizSessionManager quizSessionManager;
    private final ProfesseurRepository professeurRepository;

    // Ouvre une session QR
    @PostMapping("/sessions")
    public ResponseEntity<QrLiveSessionCreatedResponse> createSession(
            @AuthenticationPrincipal UserDetails userDetails) {
        Professeur prof = professeurRepository
                .findByEmail(userDetails.getUsername().trim().toLowerCase())
                .orElseThrow(() -> new org.springframework.web.server.ResponseStatusException(
                        org.springframework.http.HttpStatus.FORBIDDEN, "Professeur introuvable"));
        String token = quizSessionManager.createSession(prof.getId(), prof.getEmail());
        return ResponseEntity.status(HttpStatus.CREATED).body(new QrLiveSessionCreatedResponse(token));
    }

    // Clôture : plus aucune donnée session en mémoire 
    @DeleteMapping("/sessions/{token}")
    public ResponseEntity<Void> closeSession(@PathVariable String token) {
        if (!quizSessionManager.forceCloseSession(token)) {
            return ResponseEntity.notFound().build();
        }
        return ResponseEntity.noContent().build();
    }

    
     // Permet aux clients sans JWT de charger l’instantané courant ou d’afficher une salle d’attente.
  
    @GetMapping("/public/{token}/snapshot")
    public ResponseEntity<QrLiveStreamEnvelope> snapshot(@PathVariable String token) {
        return quizSessionManager.snapshotEnvelope(token)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    
    // Soumission réponse participant (sans JWT) 
    @PostMapping("/sessions/{token}/reponses")
    public ResponseEntity<QrLiveSubmitAnswerResponse> reponsePublique(
            @PathVariable String token,
            @RequestBody @Valid QrLivePublicReponseRequest body) {
        return quizSessionManager
                .submitAnswerByReponseId(
                        token,
                        body.getParticipantId().trim(),
                        body.getCorrelationId().trim(),
                        body.getQuestionId(),
                        body.getReponseId())
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }
}
