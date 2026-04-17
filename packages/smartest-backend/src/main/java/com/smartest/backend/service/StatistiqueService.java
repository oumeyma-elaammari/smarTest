package com.smartest.backend.service;

import com.smartest.backend.dto.response.StatistiqueQuestionResponse;
import com.smartest.backend.entity.StatistiqueQuestion;
import com.smartest.backend.dto.response.StatistiquesQuizResponse;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Quiz;
import com.smartest.backend.entity.StatistiqueQuestion;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.QuizRepository;
import com.smartest.backend.repository.ReponseEtudiantRepository;
import com.smartest.backend.repository.StatistiqueQuestionRepository;
import lombok.Builder;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
@Builder
public class StatistiqueService {

    private final StatistiqueQuestionRepository statistiqueQuestionRepository;
    private final ReponseEtudiantRepository reponseEtudiantRepository;
    private final QuizRepository quizRepository;
    private final QuestionRepository questionRepository;

    /**
     * Calculer les statistiques pour toutes les questions d'un quiz
     */
    @Transactional
    public StatistiquesQuizResponse calculerStatistiquesQuiz(Long quizId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new RuntimeException("Quiz non trouvé"));

        List<StatistiqueQuestionResponse> statsParQuestion = new ArrayList<>();
        int totalParticipants = 0;
        double sommeNotes = 0;

        for (Question question : quiz.getQuestions()) {
            StatistiqueQuestionResponse stat = calculerStatistiqueQuestion(quizId, question.getId());
            statsParQuestion.add(stat);
            totalParticipants = Math.max(totalParticipants, stat.getNombreReponses());
        }

        // Statistiques globales
        double moyenneGenerale = statsParQuestion.stream()
                .mapToDouble(StatistiqueQuestionResponse::getPourcentageReussite)
                .average()
                .orElse(0);

        long questionsAlerte = statsParQuestion.stream()
                .filter(StatistiqueQuestionResponse::getAlerteEchec)
                .count();

        return StatistiquesQuizResponse.builder()
                .quizId(quizId)
                .quizTitre(quiz.getTitre())
                .nombreParticipants(totalParticipants)
                .moyenneGenerale(moyenneGenerale)
                .tauxReussiteGlobal(moyenneGenerale)
                .questionsAlerteCount(questionsAlerte)
                .statistiquesParQuestion(statsParQuestion)
                .build();
    }

    /**
     * Calculer les statistiques pour une question spécifique
     */
    @Transactional
    public StatistiqueQuestionResponse calculerStatistiqueQuestion(Long quizId, Long questionId) {

        // Compter les réponses
        Long totalReponses = reponseEtudiantRepository.countByQuestionId(questionId);
        Long reponsesCorrectes = reponseEtudiantRepository.countByQuestionIdAndEstCorrecteTrue(questionId);
        Long reponsesIncorrectes = totalReponses - reponsesCorrectes;

        double pourcentageReussite = totalReponses > 0
                ? (reponsesCorrectes.doubleValue() / totalReponses) * 100
                : 0;

        double pourcentageEchec = 100 - pourcentageReussite;
        boolean alerteEchec = pourcentageEchec > 80;

        // Récupérer la question
        Question question = questionRepository.findById(questionId)
                .orElseThrow(() -> new RuntimeException("Question non trouvée"));

        // Sauvegarder ou mettre à jour la statistique
        StatistiqueQuestion statistique = statistiqueQuestionRepository
                .findByQuestionIdAndQuizId(questionId, quizId)
                .orElse(new StatistiqueQuestion());

        statistique.setQuestion(question);
        statistique.setQuiz(quizRepository.findById(quizId).orElse(null));
        statistique.setNombreReponses(totalReponses.intValue());
        statistique.setNombreCorrectes(reponsesCorrectes.intValue());
        statistique.setNombreIncorrectes(reponsesIncorrectes.intValue());
        statistique.setPourcentageReussite(pourcentageReussite);
        statistique.setPourcentageEchec(pourcentageEchec);
        statistique.setAGenereAlerte(alerteEchec);

        statistiqueQuestionRepository.save(statistique);

        // Construire le DTO de réponse
        return StatistiqueQuestionResponse.builder()
                .questionId(questionId)
                .questionEnonce(question.getEnonce())
                .typeQuestion(String.valueOf(question.getType()))
                .nombreReponses(totalReponses.intValue())
                .nombreCorrectes(reponsesCorrectes.intValue())
                .nombreIncorrectes(reponsesIncorrectes.intValue())
                .pourcentageReussite(pourcentageReussite)
                .pourcentageEchec(pourcentageEchec)
                .alerteEchec(alerteEchec)
                .build();
    }

    /**
     * Récupérer toutes les questions en alerte pour un quiz
     */
    @Transactional(readOnly = true)
    public List<StatistiqueQuestionResponse> getQuestionsAlerte(Long quizId) {
        return statistiqueQuestionRepository.findAlertesByQuizId(quizId)
                .stream()
                .map(this::convertToDTO)
                .collect(Collectors.toList());
    }

    private StatistiqueQuestionResponse convertToDTO(StatistiqueQuestion stat) {
        return StatistiqueQuestionResponse.builder()
                .questionId(stat.getQuestion().getId())
                .questionEnonce(stat.getQuestion().getEnonce())
                .typeQuestion(String.valueOf(stat.getQuestion().getType()))
                .nombreReponses(stat.getNombreReponses())
                .nombreCorrectes(stat.getNombreCorrectes())
                .nombreIncorrectes(stat.getNombreIncorrectes())
                .pourcentageReussite(stat.getPourcentageReussite())
                .pourcentageEchec(stat.getPourcentageEchec())
                .alerteEchec(stat.getAGenereAlerte())
                .build();
    }
}