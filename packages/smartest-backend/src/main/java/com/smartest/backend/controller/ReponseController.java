package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ReponseRequest;
import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.service.ReponseService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/reponses")
@RequiredArgsConstructor
public class ReponseController {

    private final ReponseService reponseService;

    // QUIZ
    @PostMapping("/quiz")
    public ResponseEntity<ReponseResponse> repondreQuiz(
            @RequestParam Long questionId,
            @RequestParam Long reponseId,
            @RequestParam Long etudiantId
    ) {
        return ResponseEntity.ok(
                reponseService.verifierReponse(questionId, reponseId, etudiantId)
        );
    }

    // EXAMEN
    @PostMapping("/examen")
    public ResponseEntity<String> repondreExamen(@RequestBody ReponseRequest request) {

        reponseService.enregistrerReponseExamen(
                request.getQuestionId(),
                request.getReponseId(),
                request.getEtudiantId(),
                request.getSessionId()
        );

        return ResponseEntity.ok("Réponse enregistrée");
    }
}