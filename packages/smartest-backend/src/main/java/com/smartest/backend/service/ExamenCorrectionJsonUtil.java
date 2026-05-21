package com.smartest.backend.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

/**
 * Lecture minimale du JSON de réponse (évite une dépendance circulaire avec le worker Groq).
 */
final class ExamenCorrectionJsonUtil {

    private static final ObjectMapper OM = new ObjectMapper();

    private ExamenCorrectionJsonUtil() {
    }

    static String lireTexte(String json) {
        if (json == null || json.isBlank()) {
            return "";
        }
        try {
            JsonNode n = OM.readTree(json).path("texte");
            return n.isMissingNode() || !n.isTextual() ? "" : n.asText("");
        } catch (Exception e) {
            return "";
        }
    }
}
