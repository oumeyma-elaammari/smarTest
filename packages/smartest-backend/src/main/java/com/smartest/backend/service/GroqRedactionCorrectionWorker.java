package com.smartest.backend.service;

import com.smartest.backend.entity.ExamenCorrectionLigne;
import com.smartest.backend.entity.ExamenPassageResultat;
import com.smartest.backend.entity.Question;
import com.smartest.backend.repository.ExamenCorrectionLigneRepository;
import com.smartest.backend.repository.ExamenPassageResultatRepository;
import com.smartest.backend.repository.QuestionRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Lazy;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.List;
import java.util.OptionalDouble;

/**
 * Correction asynchrone des rédactions via Groq (ne bloque pas la soumission étudiant).
 */
@Service
@Slf4j
public class GroqRedactionCorrectionWorker {

    private final ExamenCorrectionLigneRepository ligneRepository;
    private final ExamenPassageResultatRepository passageRepository;
    private final QuestionRepository questionRepository;
    private final GroqRedactionGradingClient groqClient;
    private final GroqRedactionCorrectionWorker self;
    private final ExamenSupervisionService supervisionService;

    public GroqRedactionCorrectionWorker(
            ExamenCorrectionLigneRepository ligneRepository,
            ExamenPassageResultatRepository passageRepository,
            QuestionRepository questionRepository,
            GroqRedactionGradingClient groqClient,
            @Lazy GroqRedactionCorrectionWorker self,
            @Lazy ExamenSupervisionService supervisionService) {
        this.ligneRepository = ligneRepository;
        this.passageRepository = passageRepository;
        this.questionRepository = questionRepository;
        this.groqClient = groqClient;
        this.self = self;
        this.supervisionService = supervisionService;
    }

    /**
     * Correction Groq immédiate (évaluation sémantique par l'IA, pas de comparaison de caractères).
     */
    public void corrigerRedactionMaintenant(Long examenPublieId, Long etudiantId, Long questionId, double pointsQuestion) {
        try {
            appliquerCorrectionGroq(examenPublieId, etudiantId, questionId, pointsQuestion);
        } catch (Exception ex) {
            log.warn("Correction Groq immédiate échouée examen={} etudiant={} question={}: {}",
                    examenPublieId, etudiantId, questionId, ex.getMessage());
        }
    }

    @Async
    public void planifierCorrection(Long examenPublieId, Long etudiantId, Long questionId, double pointsQuestion) {
        try {
            self.appliquerCorrectionGroq(examenPublieId, etudiantId, questionId, pointsQuestion);
        } catch (Exception ex) {
            log.warn("Correction Groq interrompue examen={} etudiant={} question={}: {}",
                    examenPublieId, etudiantId, questionId, ex.getMessage());
        }
    }

    @Transactional
    public void appliquerCorrectionGroq(Long examenPublieId, Long etudiantId, Long questionId, double pointsQuestion) {
        ExamenCorrectionLigne ligne = ligneRepository
                .findByExamenPublie_IdAndEtudiant_IdAndQuestion_Id(examenPublieId, etudiantId, questionId)
                .orElse(null);
        if (ligne == null || !ligne.isCorrigeParIa()) {
            return;
        }
        Question q = questionRepository.findById(questionId).orElse(null);
        if (q == null) {
            ligne.setGroqStatut("ERREUR");
            ligneRepository.save(ligne);
            return;
        }
        String modele = ExamenCorrectionService.extraireReponseModele(q);
        String texteEtudiant = ExamenCorrectionJsonUtil.lireTexte(ligne.getReponseJson());
        if (texteEtudiant.isBlank()) {
            ligne.setScorePartiel(0.0);
            ligne.setGroqStatut(null);
            ligne.setDateCorrection(LocalDateTime.now());
            ligneRepository.save(ligne);
            recalculerNoteProposee(examenPublieId, etudiantId);
            return;
        }
        String enonce = q.getEnonce() != null ? q.getEnonce() : "";
        OptionalDouble score = groqClient.noterRedaction(
                examenPublieId, enonce, texteEtudiant, modele, pointsQuestion);
        if (score.isPresent()) {
            ligne.setScorePartiel(score.getAsDouble());
            ligne.setGroqStatut("OK");
        } else {
            ligne.setScorePartiel(null);
            ligne.setGroqStatut(groqClient.estCleDisponible(examenPublieId) ? "ERREUR" : "CLE_MANQUANTE");
        }
        ligne.setDateCorrection(LocalDateTime.now());
        ligneRepository.save(ligne);

        recalculerNoteProposee(examenPublieId, etudiantId);
        supervisionService.rafraichirBrouillonResultatRuntime(examenPublieId, etudiantId);
    }

    private void recalculerNoteProposee(Long examenPublieId, Long etudiantId) {
        List<ExamenCorrectionLigne> lignes =
                ligneRepository.findByExamenPublie_IdAndEtudiant_IdOrderByQuestion_IdAsc(examenPublieId, etudiantId);
        double sum = 0.0;
        for (ExamenCorrectionLigne l : lignes) {
            if (l.getScorePartiel() != null) {
                sum += l.getScorePartiel();
            }
        }
        final double totalPropose = sum;
        passageRepository.findByExamenPublie_IdAndEtudiant_Id(examenPublieId, etudiantId).ifPresent(p -> {
            if (!p.isValideeParProf()) {
                p.setNoteProposee(totalPropose);
                p.setDateMiseAJour(LocalDateTime.now());
                passageRepository.save(p);
            }
        });
    }
}
