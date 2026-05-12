package com.smartest.backend.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

/**
 * Recalcul des stats après un court délai (soumission web ou bureau), hors requête HTTP.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class StatistiqueRecalculService {

    private static final long DELAI_MS = 3000L;

    private final StatistiqueService statistiqueService;

    @Async
    public void planifierApresDelai(Long quizId) {
        try {
            Thread.sleep(DELAI_MS);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            return;
        }
        try {
            statistiqueService.recalculerStatistiquesQuizSync(quizId);
        } catch (Exception ex) {
            log.warn("Recalcul statistiques quiz {} : {}", quizId, ex.getMessage());
        }
    }
}
