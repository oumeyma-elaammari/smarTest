package com.smartest.backend.controller;

import com.smartest.backend.dto.response.StatistiqueQuestionResponse;
import com.smartest.backend.dto.response.StatistiquesQuizResponse;
import com.smartest.backend.service.StatistiqueService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/api/statistiques")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class StatistiqueController {

    private final StatistiqueService statistiqueService;

    // Statistiques complètes d’un quiz
    @GetMapping("/quiz/{quizId}")
    public ResponseEntity<StatistiquesQuizResponse> getStatistiquesQuiz(
            @PathVariable Long quizId,
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(
                statistiqueService.obtenirStatistiquesQuizPourProfesseur(quizId, userDetails.getUsername()));
    }

    @GetMapping("/quiz/{quizId}/question/{questionId}")
    public ResponseEntity<StatistiqueQuestionResponse> getStatistiqueQuestion(
            @PathVariable Long quizId,
            @PathVariable Long questionId,
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(
                statistiqueService.obtenirStatistiqueQuestionPourProfesseur(quizId, questionId, userDetails.getUsername()));
    }

    // Questions avec taux d’échec &gt; 70 % (1ʳᵉ tentative, résultats quiz).
     
    @GetMapping("/quiz/{quizId}/alertes")
    public ResponseEntity<List<StatistiqueQuestionResponse>> getQuestionsAlerte(
            @PathVariable Long quizId,
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(
                statistiqueService.obtenirQuestionsAlertePourProfesseur(quizId, userDetails.getUsername()));
    }

    // Statistiques d’un examen publié (notes de passage, taux de réussite par question).
    @GetMapping("/examen/{examenId}")
    public ResponseEntity<StatistiquesQuizResponse> getStatistiquesExamen(
            @PathVariable Long examenId,
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(
                statistiqueService.obtenirStatistiquesExamenPourProfesseur(examenId, userDetails.getUsername()));
    }
}
