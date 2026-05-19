package com.smartest.backend.service;

import org.springframework.stereotype.Component;

import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Clé Groq fournie par le bureau (en-tête HTTP) pour la correction des rédactions pendant un examen.
 * Non persistée en base — mémoire serveur uniquement.
 */
@Component
public class GroqApiKeyRegistry {

    private final ConcurrentHashMap<Long, String> byExamenId = new ConcurrentHashMap<>();

    public void registerForExamen(Long examenId, String apiKey) {
        if (examenId == null || apiKey == null || apiKey.isBlank()) {
            return;
        }
        byExamenId.put(examenId, apiKey.trim());
    }

    public Optional<String> resolveForExamen(Long examenId) {
        if (examenId == null) {
            return Optional.empty();
        }
        String k = byExamenId.get(examenId);
        if (k != null && !k.isBlank()) {
            return Optional.of(k);
        }
        return Optional.empty();
    }

    public boolean hasKeyForExamen(Long examenId) {
        return resolveForExamen(examenId).isPresent();
    }
}
