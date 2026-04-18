package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ReponseEtudiantRequest;
import com.smartest.backend.dto.response.CorrectionResponse;
import com.smartest.backend.service.QuizCorrectionService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/quiz-correction")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class QuizCorrectionController {

    private final QuizCorrectionService quizCorrectionService;

    /**
     * POST /api/quiz-correction/question
     * Corriger la réponse d'un étudiant à une question unique
     * Retourne immédiatement correct/incorrect avec explication
     */
    @PostMapping("/question")
    public ResponseEntity<CorrectionResponse> corrigerReponse(
            @Valid @RequestBody ReponseEtudiantRequest request) {
        CorrectionResponse correction = quizCorrectionService.corrigerReponse(request);
        return ResponseEntity.ok(correction);
    }

    /**
     * POST /api/quiz-correction/quiz
     * Corriger toutes les réponses d'un étudiant pour un quiz complet
     */
    @PostMapping("/quiz")
    public ResponseEntity<List<CorrectionResponse>> corrigerQuiz(
            @Valid @RequestBody List<ReponseEtudiantRequest> requests) {
        List<CorrectionResponse> corrections = quizCorrectionService.corrigerToutesLesReponses(requests);
        return ResponseEntity.ok(corrections);
    }

    /**
     * POST /api/quiz-correction/score
     * Calculer et retourner le score final de l'étudiant (en pourcentage)
     */
    @PostMapping("/score")
    public ResponseEntity<Map<String, Object>> calculerScore(
            @Valid @RequestBody List<ReponseEtudiantRequest> requests) {
        double score = quizCorrectionService.calculerScore(requests);
        List<CorrectionResponse> corrections = quizCorrectionService.corrigerToutesLesReponses(requests);
        long bonnesReponses = corrections.stream().filter(CorrectionResponse::isCorrect).count();

        return ResponseEntity.ok(Map.of(
                "score", score,
                "bonnesReponses", bonnesReponses,
                "totalQuestions", requests.size(),
                "corrections", corrections
        ));
    }
}