package com.smartest.backend.service;

import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class ReponseService {

    private final ReponseRepository reponseRepository;
    private final QuestionRepository questionRepository;
    private final ResultatRepository resultatRepository;
    private final SessionExamenRepository sessionExamenRepository;
    private final EtudiantRepository etudiantRepository;

    // ================= QUIZ =================
    public ReponseResponse verifierReponse(Long questionId, Long reponseId, Long etudiantId) {

        if (resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenIsNull(
                etudiantId, questionId)) {
            throw new RuntimeException("Vous avez déjà répondu à cette question");
        }

        Reponse reponse = reponseRepository.findById(reponseId)
                .orElseThrow(() -> new RuntimeException("Réponse non trouvée"));

        // ✅ UTILISATION MÉTHODE
        verifierCorrespondanceQuestion(reponse, questionId);

        Etudiant etudiant = etudiantRepository.findById(etudiantId)
                .orElseThrow(() -> new RuntimeException("Étudiant non trouvé"));

        Resultat resultat = new Resultat();
        resultat.setEtudiant(etudiant);
        resultat.setQuestion(reponse.getQuestion());
        resultat.setReponse(reponse);
        resultat.setCorrecte(reponse.getCorrecte());

        resultatRepository.save(resultat);

        return new ReponseResponse(reponse);
    }

    // ================= EXAMEN =================
    public void enregistrerReponseExamen(Long questionId, Long reponseId, Long etudiantId, Long sessionId) {

        if (resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenId(
                etudiantId, questionId, sessionId)) {
            throw new RuntimeException("Vous avez déjà répondu à cette question dans cet examen");
        }

        Reponse reponse = reponseRepository.findById(reponseId)
                .orElseThrow(() -> new RuntimeException("Réponse non trouvée"));

        // ✅ UTILISATION MÉTHODE
        verifierCorrespondanceQuestion(reponse, questionId);

        Etudiant etudiant = etudiantRepository.findById(etudiantId)
                .orElseThrow(() -> new RuntimeException("Etudiant non trouvé"));

        SessionExamen session = sessionExamenRepository.findById(sessionId)
                .orElseThrow(() -> new RuntimeException("Session non trouvée"));

        if (!"EN_COURS".equals(session.getStatut())) {
            throw new RuntimeException("Session non active");
        }

        if (session.getDateFin().isBefore(java.time.LocalDateTime.now())) {
            throw new RuntimeException("Temps écoulé, examen terminé");
        }

        Resultat resultat = new Resultat();
        resultat.setEtudiant(etudiant);
        resultat.setQuestion(reponse.getQuestion());
        resultat.setReponse(reponse);
        resultat.setCorrecte(reponse.getCorrecte());
        resultat.setSessionExamen(session);

        resultatRepository.save(resultat);
    }

    // ================= MÉTHODE UTILITAIRE =================
    private void verifierCorrespondanceQuestion(Reponse reponse, Long questionId) {
        if (!reponse.getQuestion().getId().equals(questionId)) {
            throw new RuntimeException("Réponse ne correspond pas à la question");
        }
    }
}