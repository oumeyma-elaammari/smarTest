package com.smartest.backend.controller;

import com.smartest.backend.service.ExamenSupervisionService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.messaging.handler.annotation.MessageMapping;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Controller;

import java.util.Map;

@Controller
@RequiredArgsConstructor
@Slf4j
public class ExamenWebSocketController {

    private final ExamenSupervisionService examenSupervisionService;

    @MessageMapping("/examen/reponse")
    public void recevoirReponse(@Payload Map<String, Object> payload) {
        try {
            Long examenId = extractLong(payload, "examenId");
            Long etudiantId = extractLong(payload, "etudiantId");
            Long questionId = extractLong(payload, "questionId");
            Long reponseId = extractLong(payload, "reponseId");

            if (examenId == null || etudiantId == null || questionId == null || reponseId == null) {
                log.warn("Payload WebSocket réponse invalide: {}", payload);
                return;
            }

            examenSupervisionService.enregistrerReponseEtudiant(examenId, etudiantId, questionId, reponseId);
            log.debug("Réponse WebSocket enregistrée pour examen {} étudiant {} question {} réponse {}", 
                    examenId, etudiantId, questionId, reponseId);
        } catch (Exception e) {
            log.error("Erreur lors du traitement de la réponse WebSocket", e);
        }
    }

    private Long extractLong(Map<String, Object> payload, String key) {
        Object value = payload.get(key);
        if (value == null) return null;
        if (value instanceof Number) {
            return ((Number) value).longValue();
        }
        try {
            return Long.parseLong(value.toString());
        } catch (NumberFormatException e) {
            return null;
        }
    }
}