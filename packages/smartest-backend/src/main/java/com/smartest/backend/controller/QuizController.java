package com.smartest.backend.controller;

import java.util.List;

import com.smartest.backend.dto.response.ExamenResponse;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.MessageResponse;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.service.QuizService;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("/api/quizs")
@RequiredArgsConstructor
@CrossOrigin(origins = "http://localhost:5173")
public class QuizController {

    private final QuizService quizService;

    @GetMapping
    public ResponseEntity<List<QuizResponse>> getAllQuizs() {
        return ResponseEntity.ok(quizService.getAllQuizs());
    }

    @GetMapping("/{id}")
    public ResponseEntity<QuizResponse> getQuizById(@PathVariable Long id) {
        return ResponseEntity.ok(quizService.getQuizById(id));
    }

    @PostMapping
    public ResponseEntity<QuizResponse> createQuiz(@Valid @RequestBody QuizRequest request) {
        QuizResponse createdQuiz = quizService.createQuiz(request);
        return new ResponseEntity<>(createdQuiz, HttpStatus.CREATED);
    }

    @PutMapping("/{id}")
    public ResponseEntity<QuizResponse> updateQuiz(@PathVariable Long id, @Valid @RequestBody QuizRequest request) {
        return ResponseEntity.ok(quizService.updateQuiz(id, request));
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<MessageResponse> deleteQuiz(@PathVariable Long id) {
        quizService.deleteQuiz(id);
        return ResponseEntity.ok(new MessageResponse("Quiz supprimé avec succès", true, 200));
    }
    /**
     * POST /api/quizs/{quizId}/questions/{questionId} - Ajouter une question à un quiz
     */
    @PostMapping("/{quizId}/questions/{questionId}")
    public ResponseEntity<QuizResponse> addQuestionToQuiz(
            @PathVariable Long quizId,
            @PathVariable Long questionId) {
        return ResponseEntity.ok(quizService.addQuestionToQuiz(quizId, questionId));
    }

    /**
     * DELETE /api/quizs/{quizId}/questions/{questionId} - Supprimer une question d'un quiz
     */
    @DeleteMapping("/{quizId}/questions/{questionId}")
    public ResponseEntity<QuizResponse> removeQuestionFromQuiz(
            @PathVariable Long quizId,
            @PathVariable Long questionId) {
        return ResponseEntity.ok(quizService.removeQuestionFromQuiz(quizId, questionId));
    }
}