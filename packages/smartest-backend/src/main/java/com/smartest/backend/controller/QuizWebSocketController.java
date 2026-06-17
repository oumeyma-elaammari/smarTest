package com.smartest.backend.controller;

import com.smartest.backend.dto.qr.QuizQrEphemeralProfInitPayload;
import com.smartest.backend.dto.qr.QuizQrEphemeralStudentAnswerPayload;
import com.smartest.backend.service.QuizSessionManager;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.validation.annotation.Validated;
import org.springframework.messaging.handler.annotation.MessageMapping;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Controller;
import org.springframework.web.server.ResponseStatusException;

import java.security.Principal;

// Flux QR hors MySQL 
 
@Controller
@Validated
@RequiredArgsConstructor
@Slf4j
public class QuizWebSocketController {

    private final QuizSessionManager quizSessionManager;

    @MessageMapping("/quiz-qr/prof/init")
    public void profInit(@Payload @Valid QuizQrEphemeralProfInitPayload body,
                         Principal principal) {
        if (principal == null) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "Authentification requise");
        }
        quizSessionManager.initFromProf(body.getSessionToken(), principal.getName(), body);
    }

    @MessageMapping("/quiz-qr/student/answer")
    public void studentAnswer(@Payload @Valid QuizQrEphemeralStudentAnswerPayload body) {
        int qOneBased = Math.toIntExact(body.getQuestionId());
        quizSessionManager.submitAnswer(
                body.getSessionToken(),
                body.getParticipantId(),
                body.getCorrelationId(),
                qOneBased,
                body.getLettreChoix());
    }
}
