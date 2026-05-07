package com.smartest.backend.service;

import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.exception.InvalidSessionStateException;
import com.smartest.backend.exception.QuestionNotFoundException;
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

        questionRepository.findById(questionId)
                .orElseThrow(() -> new QuestionNotFoundException(questionId));

        if (resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenIsNull(
                etudiantId, questionId)) {
            throw new IllegalArgumentException("Vous avez déjà répondu à cette question");
        }

        Reponse reponse = reponseRepository.findById(reponseId)
                .orElseThrow(() -> new IllegalArgumentException("Réponse non trouvée"));

        verifierCorrespondanceQuestion(reponse, questionId);

        Etudiant etudiant = etudiantRepository.findById(etudiantId)
                .orElseThrow(() -> new IllegalArgumentException("Étudiant non trouvé"));

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

        questionRepository.findById(questionId)
                .orElseThrow(() -> new QuestionNotFoundException(questionId));

        if (resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenId(
                etudiantId, questionId, sessionId)) {
            throw new IllegalArgumentException("Vous avez déjà répondu à cette question dans cet examen");
        }

        Reponse reponse = reponseRepository.findById(reponseId)
                .orElseThrow(() -> new IllegalArgumentException("Réponse non trouvée"));

        verifierCorrespondanceQuestion(reponse, questionId);

        Etudiant etudiant = etudiantRepository.findById(etudiantId)
                .orElseThrow(() -> new IllegalArgumentException("Etudiant non trouvé"));

        SessionExamen session = sessionExamenRepository.findById(sessionId)
                .orElseThrow(() -> new IllegalArgumentException("Session non trouvée"));

        if (!"EN_COURS".equals(session.getStatut())) {
            throw new InvalidSessionStateException("La session d'examen n'est pas active.");
        }

        if (session.getDateFin().isBefore(java.time.LocalDateTime.now())) {
            throw new InvalidSessionStateException("Le temps imparti pour cet examen est écoulé.");
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
            throw new IllegalArgumentException("La réponse ne correspond pas à la question indiquée.");
        }
    }
}
