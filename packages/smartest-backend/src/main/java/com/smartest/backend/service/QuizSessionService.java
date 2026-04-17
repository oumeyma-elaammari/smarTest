package com.smartest.backend.service;

import com.smartest.backend.dto.request.SoumissionReponse;
import com.smartest.backend.dto.response.CorrectionImmediateResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.util.HashMap;
import java.util.Map;

@Service
@RequiredArgsConstructor
public class QuizSessionService {

    private final QuizRepository quizRepository;
    private final QuestionRepository questionRepository;
    private final ReponseEtudiantRepository reponseEtudiantRepository;
    private final ResultatRepository resultatRepository;
    private final EtudiantRepository etudiantRepository;

    private Map<Long, Map<Long, Integer>> progressionEtudiants = new HashMap<>();

    /**
     * Soumettre une réponse pour un Quiz (avec correction immédiate + note finale)
     */
    @Transactional
    public CorrectionImmediateResponse soumettreReponseQuiz(Long etudiantId, SoumissionReponse dto) {

        // 1. Récupérer la question et le quiz
        Question question = questionRepository.findById(dto.getQuestionId())
                .orElseThrow(() -> new RuntimeException("Question non trouvée"));

        Quiz quiz = quizRepository.findById(dto.getQuizId())
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé"));

        // 2. Vérifier si la réponse est correcte
        boolean estCorrecte = verifierReponse(question, dto.getReponse());

        // 3. Récupérer la bonne réponse à afficher
        String bonneReponse = getBonneReponse(question);

        // 4. Enregistrer la réponse
        enregistrerReponse(etudiantId, question, dto.getReponse(), estCorrecte, bonneReponse);

        // 5. Mettre à jour la progression
        int nbReponses = incrementerProgression(etudiantId, dto.getQuizId());
        int totalQuestions = quiz.getQuestions().size();
        boolean estTermine = nbReponses >= totalQuestions;

        // 6. Calculer la note actuelle et le score
        Long nbCorrectes = reponseEtudiantRepository.countByEtudiantIdAndEstCorrecteTrue(etudiantId);
        Double noteActuelle = (nbCorrectes.doubleValue() / totalQuestions) * 20;
        int score = nbCorrectes.intValue();

        // 7. Construire le message de feedback
        String message = construireMessageFeedback(estCorrecte, String.valueOf(question.getType()));
        String progression = nbReponses + "/" + totalQuestions;

        Double noteFinale = null;
        String messageFinal = null;

        // 8. Si le quiz est terminé, calculer et enregistrer la note finale
        if (estTermine) {
            noteFinale = enregistrerResultatFinal(etudiantId, quiz, nbCorrectes, totalQuestions, noteActuelle);
            messageFinal = getMessageFinal(noteFinale, score, totalQuestions);
        }

        // 9. Retourner la réponse
        return CorrectionImmediateResponse.builder()
                .estCorrecte(estCorrecte)
                .message(message)
                .bonneReponse(bonneReponse)
                .progression(progression)
                .estTermine(estTermine)
                .noteActuelle(noteActuelle)
                .noteFinale(noteFinale)
                .messageFinal(messageFinal)
                .score(score)
                .totalQuestions(totalQuestions)
                .build();
    }

    /**
     * Enregistrer une réponse d'étudiant
     */
    private void enregistrerReponse(Long etudiantId, Question question, String reponse, boolean estCorrecte, String bonneReponse) {
        ReponseEtudiant reponseEtudiant = new ReponseEtudiant();
        reponseEtudiant.setReponseTexte(reponse);
        reponseEtudiant.setEstCorrecte(estCorrecte);
        reponseEtudiant.setARecuCorrectionImmediate(true);
        reponseEtudiant.setBonneReponseAffichee(bonneReponse);

        Etudiant etudiant = etudiantRepository.findById(etudiantId)
                .orElseThrow(() -> new RuntimeException("Étudiant non trouvé"));
        reponseEtudiant.setEtudiant(etudiant);
        reponseEtudiant.setQuestion(question);

        reponseEtudiantRepository.save(reponseEtudiant);
    }

    /**
     * Incrémenter la progression de l'étudiant
     */
    private int incrementerProgression(Long etudiantId, Long quizId) {
        int nbReponses = progressionEtudiants
                .computeIfAbsent(quizId, k -> new HashMap<>())
                .getOrDefault(etudiantId, 0);
        nbReponses++;
        progressionEtudiants.get(quizId).put(etudiantId, nbReponses);
        return nbReponses;
    }

    /**
     * Enregistrer le résultat final du quiz
     */
    private Double enregistrerResultatFinal(Long etudiantId, Quiz quiz, Long nbCorrectes, int totalQuestions, Double note) {
        // Récupérer l'étudiant
        Etudiant etudiant = etudiantRepository.findById(etudiantId)
                .orElseThrow(() -> new RuntimeException("Étudiant non trouvé"));

        // Vérifier si un résultat existe déjà
        Resultat resultat = resultatRepository.findByEtudiantIdAndQuizId(etudiantId, quiz.getId())
                .orElse(new Resultat());

        resultat.setNote(note);
        resultat.setScore((float) nbCorrectes.intValue());
        resultat.setStatut("CALCULEE");
        resultat.setEtudiant(etudiant);
        resultat.setQuiz(quiz);

        resultatRepository.save(resultat);

        return note;
    }

    /**
     * Générer un message de fin personnalisé
     */
    private String getMessageFinal(Double note, int score, int totalQuestions) {
        if (note >= 16) {
            return "🏆 Exceptionnel ! " + score + "/" + totalQuestions + " - " + String.format("%.1f", note) + "/20";
        } else if (note >= 14) {
            return "🎉 Très bien ! " + score + "/" + totalQuestions + " - " + String.format("%.1f", note) + "/20";
        } else if (note >= 12) {
            return "👍 Bien ! " + score + "/" + totalQuestions + " - " + String.format("%.1f", note) + "/20";
        } else if (note >= 10) {
            return "✅ Passable. " + score + "/" + totalQuestions + " - " + String.format("%.1f", note) + "/20";
        } else {
            return "📚 À réviser. " + score + "/" + totalQuestions + " - " + String.format("%.1f", note) + "/20";
        }
    }

    /**
     * Vérifier si la réponse est correcte
     */
    private boolean verifierReponse(Question question, String reponseEtudiant) {
        if ("QCM".equals(question.getType()) || "VF".equals(question.getType())) {
            return question.getReponses().stream()
                    .filter(Reponse::getCorrecte)
                    .anyMatch(r -> r.getContenu().equalsIgnoreCase(reponseEtudiant));
        }
        return false;
    }

    /**
     * Récupérer la bonne réponse
     */
    private String getBonneReponse(Question question) {
        if ("QCM".equals(question.getType()) || "VF".equals(question.getType())) {
            return question.getReponses().stream()
                    .filter(Reponse::getCorrecte)
                    .map(Reponse::getContenu)
                    .findFirst()
                    .orElse("Non définie");
        }
        return "À vérifier par le professeur";
    }

    /**
     * Construire le message de feedback
     */
    private String construireMessageFeedback(boolean estCorrecte, String typeQuestion) {
        if (estCorrecte) {
            String[] felicitations = {"Bravo !", "Excellent !", "Bien joué !", "Parfait !", "Continue comme ça !"};
            return "✅ " + felicitations[(int)(Math.random() * felicitations.length)];
        } else {
            return "❌ Incorrect. La bonne réponse est affichée ci-dessous.";
        }
    }
}