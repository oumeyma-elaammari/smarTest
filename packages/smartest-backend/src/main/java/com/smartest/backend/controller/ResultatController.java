package com.smartest.backend.controller;

import com.smartest.backend.entity.Resultat;
import com.smartest.backend.service.ResultatService;

import lombok.RequiredArgsConstructor;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/resultats")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class ResultatController {

    private final ResultatService resultatService;

    /**
     * 🔹 Tous les résultats
     */
    @GetMapping
    public ResponseEntity<List<Resultat>> getAll() {
        return ResponseEntity.ok(resultatService.getAll());
    }

    /**
     * 🔹 Résultats d’un étudiant
     */
    @GetMapping("/etudiant/{etudiantId}")
    public ResponseEntity<List<Resultat>> getByEtudiant(@PathVariable Long etudiantId) {
        return ResponseEntity.ok(resultatService.getByEtudiant(etudiantId));
    }

    /**
     * 🔹 Résultats d’une session d’examen
     */
    @GetMapping("/session/{sessionId}")
    public ResponseEntity<List<Resultat>> getBySession(@PathVariable Long sessionId) {
        return ResponseEntity.ok(resultatService.getBySession(sessionId));
    }

    /**
     * 🔹 Score d’un étudiant (quiz ou exam)
     */
    @GetMapping("/score/quiz/{etudiantId}")
    public ResponseEntity<Double> getScoreQuiz(@PathVariable Long etudiantId) {
        return ResponseEntity.ok(resultatService.calculerScoreQuiz(etudiantId));
    }

    /**
     * 🔹 Score d’une session d’examen
     */
    @GetMapping("/score/session/{sessionId}")
    public ResponseEntity<Double> getScoreSession(@PathVariable Long sessionId) {
        return ResponseEntity.ok(resultatService.calculerScoreSession(sessionId));
    }

    /**
     * 🔹 Supprimer un résultat
     */
    @DeleteMapping("/{id}")
    public ResponseEntity<String> delete(@PathVariable Long id) {
        resultatService.delete(id);
        return ResponseEntity.ok("Résultat supprimé");
    }

    @GetMapping("/historique/{etudiantId}/session/{sessionId}")
    public ResponseEntity<List<Resultat>> getHistorique(
            @PathVariable Long etudiantId,
            @PathVariable Long sessionId) {

        return ResponseEntity.ok(
                resultatService.getHistoriqueExamen(etudiantId, sessionId)
        );
    }

    // 🔹 résultats quiz uniquement
    @GetMapping("/etudiant/{id}/quiz")
    public ResponseEntity<List<Resultat>> getResultatsQuiz(@PathVariable Long id) {
        return ResponseEntity.ok(resultatService.getMesResultatsQuiz(id));
    }

    // 🔹 résultats examens uniquement
    @GetMapping("/etudiant/{id}/examens")
    public ResponseEntity<List<Resultat>> getResultatsExamens(@PathVariable Long id) {
        return ResponseEntity.ok(resultatService.getMesResultatsExamens(id));
    }
}