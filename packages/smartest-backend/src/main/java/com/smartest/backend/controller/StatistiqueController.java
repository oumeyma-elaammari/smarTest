package com.smartest.backend.controller;

import com.smartest.backend.dto.response.StatistiqueQuestionResponse;
import com.smartest.backend.dto.response.StatistiquesQuizResponse;
import com.smartest.backend.entity.StatistiqueQuestion;
import com.smartest.backend.service.StatistiqueService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.List;

@RestController
@RequestMapping("/api/statistiques")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class StatistiqueController {

    private final StatistiqueService statistiqueService;

    /**
     * GET /api/statistiques/quiz/{quizId} - Statistiques complètes d'un quiz
     */
    @GetMapping("/quiz/{quizId}")
    public ResponseEntity<StatistiquesQuizResponse> getStatistiquesQuiz(@PathVariable Long quizId) {
        return ResponseEntity.ok(statistiqueService.calculerStatistiquesQuiz(quizId));
    }

    /**
     * GET /api/statistiques/quiz/{quizId}/question/{questionId} - Statistique d'une question
     */
    @GetMapping("/quiz/{quizId}/question/{questionId}")
    public ResponseEntity<StatistiqueQuestionResponse> getStatistiqueQuestion(
            @PathVariable Long quizId,
            @PathVariable Long questionId) {
        return ResponseEntity.ok(statistiqueService.calculerStatistiqueQuestion(quizId, questionId));
    }

    /**
     * GET /api/statistiques/quiz/{quizId}/alertes - Questions en alerte (>80% échec)
     */
    @GetMapping("/quiz/{quizId}/alertes")
    public ResponseEntity<List<StatistiqueQuestionResponse>> getQuestionsAlerte(@PathVariable Long quizId) {
        return ResponseEntity.ok(statistiqueService.getQuestionsAlerte(quizId));
    }
}