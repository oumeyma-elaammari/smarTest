package com.smartest.backend.controller;

import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.dto.response.MessageResponse;
import com.smartest.backend.service.QuizService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import jakarta.validation.Valid;
import java.util.List;

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
    public ResponseEntity<QuizResponse> updateQuiz(@PathVariable Long id, @Valid @RequestBody QuizRequestDTO request) {
        return ResponseEntity.ok(quizService.updateQuiz(id, request));
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<MessageResponse> deleteQuiz(@PathVariable Long id) {
        quizService.deleteQuiz(id);
        return ResponseEntity.ok(new MessageResponse("Quiz supprimé avec succès", true, 200));
    }
}
