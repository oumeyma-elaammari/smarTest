package com.smartest.backend.controller;

import java.util.List;

import com.smartest.backend.dto.request.SoumissionQuizRequest;
import com.smartest.backend.dto.request.SoumissionQuizWebRequest;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import com.smartest.backend.dto.request.PublicationWebRequest;
import com.smartest.backend.dto.request.QrQuestionsSyncRequest;
import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.MessageResponse;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.dto.response.ResultatQuizResponse;
import com.smartest.backend.dto.response.QuizPassageWebResponse;
import com.smartest.backend.dto.response.ResultatQuizWebResponse;
import com.smartest.backend.dto.response.QuizQrLiveStatsResponse;
import com.smartest.backend.dto.response.VerificationQuestionWebResponse;
import com.smartest.backend.dto.request.VerificationQuestionWebRequest;
import com.smartest.backend.service.QuizService;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;

@RestController
@RequestMapping("/api/quizs")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class QuizController {

    private final QuizService quizService;

    // ================= GET =================

    @GetMapping
    public ResponseEntity<List<QuizResponse>> getAllQuizs() {
        return ResponseEntity.ok(quizService.getAllQuizs());
    }

    @GetMapping("/{id}")
    public ResponseEntity<QuizResponse> getQuizById(@PathVariable Long id) {
        return ResponseEntity.ok(quizService.getQuizById(id));
    }

    // ================= CREATE =================

    @PostMapping
    public ResponseEntity<QuizResponse> createQuiz(@Valid @RequestBody QuizRequest request) {
        return new ResponseEntity<>(quizService.createQuiz(request), HttpStatus.CREATED);
    }

    // ================= UPDATE =================

    @PutMapping("/{id}")
    public ResponseEntity<QuizResponse> updateQuiz(@PathVariable Long id,
                                                   @Valid @RequestBody QuizRequest request) {
        return ResponseEntity.ok(quizService.createQuiz(request));
    }

    // ================= DELETE =================

    @DeleteMapping("/{id}")
    public ResponseEntity<MessageResponse> deleteQuiz(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        quizService.deleteQuiz(id, userDetails.getUsername());
        return ResponseEntity.ok(new MessageResponse("Quiz supprimé avec succès", true, 200));
    }

    // ================= QUESTIONS =================

    @PostMapping("/{quizId}/questions/{questionId}")
    public ResponseEntity<QuizResponse> addQuestionToQuiz(
            @PathVariable Long quizId,
            @PathVariable Long questionId) {
        return ResponseEntity.ok(quizService.addQuestionToQuiz(quizId, questionId));
    }

    @DeleteMapping("/{quizId}/questions/{questionId}")
    public ResponseEntity<MessageResponse> removeQuestionFromQuiz(
            @PathVariable Long quizId,
            @PathVariable Long questionId) {
        quizService.removeQuestionFromQuiz(quizId, questionId);
        return ResponseEntity.ok(new MessageResponse("Question supprimée du quiz", true));
    }

    // ================= PUBLICATION =================

    @GetMapping("/publies")
    public ResponseEntity<List<QuizResponse>> getQuizPublies() {
        return ResponseEntity.ok(quizService.getQuizPublies());
    }

    /**
     * Quiz publiés sur le web pour lesquels l'étudiant connecté est dans la liste d'emails autorisés.
     */
    @GetMapping("/mes-publications-web")
    public ResponseEntity<List<QuizResponse>> getMesPublicationsWeb(
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(
                quizService.getMesQuizsPublicationWeb(userDetails.getUsername()));
    }

    @PatchMapping("/{id}/publier")
    public ResponseEntity<String> publierQuiz(@PathVariable Long id) {
        quizService.publierQuiz(id);
        return ResponseEntity.ok("Quiz publié");
    }

    /**
     * Publication web : enregistre les emails autorisés sur le serveur et publie le quiz.
     */
    @PostMapping("/{id}/publication-web")
    public ResponseEntity<MessageResponse> publierSurLeWeb(
            @PathVariable Long id,
            @Valid @RequestBody PublicationWebRequest request,
            @AuthenticationPrincipal UserDetails userDetails) {
        quizService.publierSurLeWeb(id, userDetails.getUsername(), request.getEmails());
        if (request.getQuestions() != null) {
            quizService.synchroniserQuestionsPublicationWeb(id, userDetails.getUsername(), request.getQuestions());
        }
        return ResponseEntity.ok(new MessageResponse("Quiz publié sur le web", true, 200));
    }

    @PostMapping("/{id}/qr-sync-questions")
    public ResponseEntity<MessageResponse> synchroniserQuestionsPourQr(
            @PathVariable Long id,
            @Valid @RequestBody QrQuestionsSyncRequest request,
            @AuthenticationPrincipal UserDetails userDetails) {
        quizService.synchroniserQuestionsPublicationWeb(id, userDetails.getUsername(), request.getQuestions());
        return ResponseEntity.ok(new MessageResponse("Questions QR synchronisées", true, 200));
    }

    // ================= QUIZ LOGIC =================

    @PostMapping("/{id}/soumettre")
    public ResponseEntity<ResultatQuizResponse> soumettreQuiz(
            @PathVariable Long id,
            @Valid @RequestBody SoumissionQuizRequest request
    ) {
        return ResponseEntity.ok(quizService.soumettreQuiz(id, request));
    }

    /**
     * Détails d'un quiz pour passage web (sans exposer les bonnes réponses avant soumission).
     */
    @GetMapping("/{id}/passage-web")
    public ResponseEntity<QuizPassageWebResponse> getQuizPourPassageWeb(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(quizService.getQuizPourPassageWeb(id, userDetails.getUsername()));
    }

    @GetMapping("/{id}/passage-qr")
    public ResponseEntity<QuizPassageWebResponse> getQuizPourPassageQr(@PathVariable Long id) {
        return ResponseEntity.ok(quizService.getQuizPourPassageQr(id));
    }

    /**
     * Vérifie une réponse au passage (feedback immédiat, sans enregistrer la tentative).
     */
    @PostMapping("/{id}/verifier-question-web")
    public ResponseEntity<VerificationQuestionWebResponse> verifierQuestionWeb(
            @PathVariable Long id,
            @Valid @RequestBody VerificationQuestionWebRequest request,
            @AuthenticationPrincipal UserDetails userDetails,
            @RequestHeader(value = "X-Quiz-Access-Mode", required = false) String accessMode) {
        return ResponseEntity.ok(quizService.verifierQuestionPassageWeb(id, userDetails.getUsername(), request, accessMode));
    }

    @PostMapping("/{id}/verifier-question-qr")
    public ResponseEntity<VerificationQuestionWebResponse> verifierQuestionQr(
            @PathVariable Long id,
            @Valid @RequestBody VerificationQuestionWebRequest request,
            @RequestHeader(value = "X-QR-Participant", required = false) String participantKey) {
        return ResponseEntity.ok(quizService.verifierQuestionPassageQr(id, request, participantKey));
    }

    /**
     * Soumission web d'un quiz par l'étudiant connecté (tentatives multiples autorisées).
     */
    @PostMapping("/{id}/soumettre-web")
    public ResponseEntity<ResultatQuizWebResponse> soumettreQuizWeb(
            @PathVariable Long id,
            @RequestBody SoumissionQuizWebRequest request,
            @AuthenticationPrincipal UserDetails userDetails,
            @RequestHeader(value = "X-Quiz-Access-Mode", required = false) String accessMode) {
        return ResponseEntity.ok(quizService.soumettreQuizWeb(id, userDetails.getUsername(), request, accessMode));
    }

    @PostMapping("/{id}/soumettre-qr")
    public ResponseEntity<ResultatQuizWebResponse> soumettreQuizQr(
            @PathVariable Long id,
            @RequestBody SoumissionQuizWebRequest request) {
        return ResponseEntity.ok(quizService.soumettreQuizQr(id, request));
    }

    @GetMapping("/{id}/stats-qr-live")
    public ResponseEntity<QuizQrLiveStatsResponse> getQrLiveStats(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok(quizService.getQrLiveStats(id, userDetails.getUsername()));
    }

    @DeleteMapping("/{id}/stats-qr-live")
    public ResponseEntity<MessageResponse> clearQrLiveStats(
            @PathVariable Long id,
            @AuthenticationPrincipal UserDetails userDetails) {
        quizService.clearQrLiveStats(id, userDetails.getUsername());
        return ResponseEntity.ok(new MessageResponse("Statistiques QR live clôturées", true, 200));
    }

    @GetMapping("/{id}/premiere-tentative/{etudiantId}")
    public ResponseEntity<Boolean> isPremiereTentative(
            @PathVariable Long id,
            @PathVariable Long etudiantId
    ) {
        return ResponseEntity.ok(
                quizService.isPremiereTentative(id, etudiantId)
        );
    }
}