package com.smartest.backend.service;

import com.smartest.backend.config.GroqProperties;
import com.smartest.backend.entity.ExamenCorrectionLigne;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Question;
import com.smartest.backend.repository.ExamenCorrectionLigneRepository;
import com.smartest.backend.repository.ExamenPublieRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

/**
 * Relance la correction Groq des rédactions lorsque la clé API devient disponible (lancement depuis le bureau).
 */
@Service
@RequiredArgsConstructor
@Slf4j
public class GroqRedactionRepriseService {

    private final ExamenCorrectionLigneRepository ligneRepository;
    private final ExamenPublieRepository examenPublieRepository;
    private final GroqApiKeyRegistry groqApiKeyRegistry;
    private final GroqProperties groqProperties;
    private final GroqRedactionCorrectionWorker groqWorker;

    @Transactional(readOnly = true)
    public void relancerCorrectionsRedactionExamen(Long examenId) {
        if (examenId == null) {
            return;
        }
        boolean cleDisponible = groqApiKeyRegistry.hasKeyForExamen(examenId)
                || (groqProperties.getApiKey() != null && !groqProperties.getApiKey().isBlank());
        if (!cleDisponible) {
            log.info("Groq: pas de clé pour l'examen {} — rédactions non relancées.", examenId);
            return;
        }

        List<Question> questions = examenPublieRepository.findByIdWithQuestions(examenId)
                .map(ExamenPublie::getQuestions)
                .orElse(List.of());
        int n = Math.max(1, questions.size());
        double baremeRef = ExamenCorrectionService.sommeBaremeQuestionsPublic(questions, 20.0);

        List<ExamenCorrectionLigne> lignes = ligneRepository.findByExamenPublie_IdAndCorrigeParIaIsTrue(examenId);
        int planifies = 0;
        for (ExamenCorrectionLigne ligne : lignes) {
            if (ligne.getQuestion() == null || ligne.getEtudiant() == null) {
                continue;
            }
            String statut = ligne.getGroqStatut();
            if ("OK".equals(statut)) {
                continue;
            }
            String texte = ExamenCorrectionJsonUtil.lireTexte(ligne.getReponseJson());
            if (texte.isBlank()) {
                continue;
            }
            Long etuId = ligne.getEtudiant().getId();
            Long qid = ligne.getQuestion().getId();
            double pts = ExamenCorrectionService.pointsPourQuestion(ligne.getQuestion(), baremeRef, n);
            groqWorker.planifierCorrection(examenId, etuId, qid, pts);
            planifies++;
        }
        if (planifies > 0) {
            log.info("Groq: {} correction(s) rédaction replanifiée(s) pour l'examen {}", planifies, examenId);
        }
    }
}
