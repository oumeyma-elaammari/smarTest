package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ExamenRequest;
import com.smartest.backend.dto.response.ExamenResponse;
import com.smartest.backend.dto.response.MessageResponse;
import com.smartest.backend.service.ExamenService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/examens")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class ExamenController {

    private final ExamenService examenService;

    /**
     * GET /api/examens - Récupérer tous les examens
     */
    @GetMapping
    public ResponseEntity<List<ExamenResponse>> getAllExamens() {
        return ResponseEntity.ok(examenService.getAllExamens());
    }

    /**
     * GET /api/examens/{id} - Récupérer un examen par son ID
     */
    @GetMapping("/{id}")
    public ResponseEntity<ExamenResponse> getExamenById(@PathVariable Long id) {
        return ResponseEntity.ok(examenService.getExamenById(id));
    }

    /**
     * GET /api/examens/professeur/{professeurId} - Examens d'un professeur
     */
    @GetMapping("/professeur/{professeurId}")
    public ResponseEntity<List<ExamenResponse>> getExamensByProfesseur(@PathVariable Long professeurId) {
        return ResponseEntity.ok(examenService.getExamensByProfesseur(professeurId));
    }

    /**
     * GET /api/examens/cours/{coursId} - Examens d'un cours
     */
    @GetMapping("/cours/{coursId}")
    public ResponseEntity<List<ExamenResponse>> getExamensByCours(@PathVariable Long coursId) {
        return ResponseEntity.ok(examenService.getExamensByCours(coursId));
    }

    /**
     * POST /api/examens - Créer un nouvel examen
     */
    @PostMapping
    public ResponseEntity<ExamenResponse> createExamen(@Valid @RequestBody ExamenRequest request) {
        ExamenResponse createdExamen = examenService.createExamen(request);
        return new ResponseEntity<>(createdExamen, HttpStatus.CREATED);
    }

    /**
     * PUT /api/examens/{id} - Mettre à jour un examen
     */
    @PutMapping("/{id}")
    public ResponseEntity<ExamenResponse> updateExamen(
            @PathVariable Long id,
            @Valid @RequestBody ExamenRequest request) {
        return ResponseEntity.ok(examenService.updateExamen(id, request));
    }

    /**
     * DELETE /api/examens/{id} - Supprimer un examen
     */
    @DeleteMapping("/{id}")
    public ResponseEntity<MessageResponse> deleteExamen(@PathVariable Long id) {
        examenService.deleteExamen(id);
        return ResponseEntity.ok(new MessageResponse("Examen supprimé avec succès", true, 200));
    }

    /**
     * POST /api/examens/{examenId}/questions/{questionId} - Ajouter une question
     */
    @PostMapping("/{examenId}/questions/{questionId}")
    public ResponseEntity<ExamenResponse> addQuestion(
            @PathVariable Long examenId,
            @PathVariable Long questionId) {
        return ResponseEntity.ok(examenService.addQuestionToExamen(examenId, questionId));
    }

    /**
     * DELETE /api/examens/{examenId}/questions/{questionId} - Supprimer une question
     */
    @DeleteMapping("/{examenId}/questions/{questionId}")
    public ResponseEntity<ExamenResponse> removeQuestion(
            @PathVariable Long examenId,
            @PathVariable Long questionId) {
        return ResponseEntity.ok(examenService.removeQuestionFromExamen(examenId, questionId));
    }
}