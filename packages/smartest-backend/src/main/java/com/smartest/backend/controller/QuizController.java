package com.smartest.backend.controller;

import java.util.List;

import com.smartest.backend.dto.request.SoumissionQuizRequest;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.MessageResponse;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.dto.response.ResultatQuizResponse;
import com.smartest.backend.service.QuizService;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;

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
    public ResponseEntity<MessageResponse> deleteQuiz(@PathVariable Long id) {
        quizService.deleteQuiz(id);
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

    @PatchMapping("/{id}/publier")
    public ResponseEntity<String> publierQuiz(@PathVariable Long id) {
        quizService.publierQuiz(id);
        return ResponseEntity.ok("Quiz publié");
    }

    // ================= QUIZ LOGIC =================

    @PostMapping("/{id}/soumettre")
    public ResponseEntity<ResultatQuizResponse> soumettreQuiz(
            @PathVariable Long id,
            @RequestBody SoumissionQuizRequest request
    ) {
        return ResponseEntity.ok(quizService.soumettreQuiz(id, request));
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