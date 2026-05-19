package com.smartest.backend.controller;

import com.smartest.backend.dto.request.ExamenPassageReponseRequest;
import com.smartest.backend.service.ExamenSupervisionService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.messaging.handler.annotation.MessageMapping;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Controller;

import java.util.ArrayList;
import java.util.List;
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
            if (examenId == null || etudiantId == null || questionId == null) {
                log.warn("Payload WebSocket réponse invalide (ids): {}", payload);
                return;
            }
            ExamenPassageReponseRequest req = new ExamenPassageReponseRequest();
            req.setQuestionId(questionId);
            req.setReponseId(extractLong(payload, "reponseId"));
            Object texte = payload.get("reponseTexte");
            if (texte != null) {
                req.setReponseTexte(String.valueOf(texte));
            }
            Object rids = payload.get("reponseIds");
            if (rids instanceof List<?> list) {
                List<Long> out = new ArrayList<>();
                for (Object o : list) {
                    Long l = extractLongFromObject(o);
                    if (l != null) {
                        out.add(l);
                    }
                }
                req.setReponseIds(out);
            }
            Object emailObj = payload.get("email");
            String email = emailObj != null ? String.valueOf(emailObj).trim() : "";
            examenSupervisionService.enregistrerReponseEtudiant(examenId, etudiantId, email, req);
            log.debug("Réponse WebSocket enregistrée pour examen {} étudiant {} question {}", examenId, etudiantId, questionId);
        } catch (Exception e) {
            log.error("Erreur lors du traitement de la réponse WebSocket", e);
        }
    }

    private static Long extractLong(Map<String, Object> payload, String key) {
        return extractLongFromObject(payload.get(key));
    }

    private static Long extractLongFromObject(Object value) {
        if (value == null) {
            return null;
        }
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
