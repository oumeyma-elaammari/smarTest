package com.smartest.backend.service;

import com.smartest.backend.dto.response.QuestionQrLiveStatResponse;
import com.smartest.backend.dto.response.QuizQrLiveStatsResponse;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class QuizQrLiveStatsService {

    private static final class QuestionAgg {
        long questionId;
        String enonce;
        int numeroQuestion;
        int reponses;
        int correctes;
    }

    private static final class QuizAgg {
        long quizId;
        String quizTitre;
        final Set<String> participants = new LinkedHashSet<>();
        final Map<Long, QuestionAgg> questions = new LinkedHashMap<>();
    }

    private final Map<Long, QuizAgg> byQuiz = new ConcurrentHashMap<>();

    public synchronized void recordVerification(
            long quizId,
            String quizTitre,
            String participantKey,
            long questionId,
            String enonce,
            boolean correcte
    ) {
        QuizAgg quiz = byQuiz.computeIfAbsent(quizId, id -> {
            QuizAgg q = new QuizAgg();
            q.quizId = id;
            q.quizTitre = quizTitre == null ? "" : quizTitre;
            return q;
        });
        if (quizTitre != null && !quizTitre.isBlank()) {
            quiz.quizTitre = quizTitre;
        }
        if (participantKey != null && !participantKey.isBlank()) {
            quiz.participants.add(participantKey.trim().toLowerCase());
        }

        QuestionAgg qa = quiz.questions.computeIfAbsent(questionId, id -> {
            QuestionAgg x = new QuestionAgg();
            x.questionId = id;
            x.enonce = enonce == null ? "" : enonce;
            x.numeroQuestion = quiz.questions.size() + 1;
            return x;
        });
        if (enonce != null && !enonce.isBlank()) {
            qa.enonce = enonce;
        }
        qa.reponses++;
        if (correcte) {
            qa.correctes++;
        }
    }

    public synchronized QuizQrLiveStatsResponse snapshot(long quizId) {
        QuizAgg quiz = byQuiz.get(quizId);
        if (quiz == null) {
            return QuizQrLiveStatsResponse.builder()
                    .quizId(quizId)
                    .quizTitre("")
                    .nombreParticipants(0)
                    .totalSoumissionsQuestions(0)
                    .tauxReussiteGlobal(0.0)
                    .statistiquesParQuestion(List.of())
                    .build();
        }

        int totalRep = 0;
        int totalCorrectes = 0;
        List<QuestionQrLiveStatResponse> lignes = new ArrayList<>();
        for (QuestionAgg qa : quiz.questions.values()) {
            int incorrectes = qa.reponses - qa.correctes;
            double txR = qa.reponses == 0 ? 0.0 : (qa.correctes * 100.0 / qa.reponses);
            double txE = qa.reponses == 0 ? 0.0 : (incorrectes * 100.0 / qa.reponses);
            totalRep += qa.reponses;
            totalCorrectes += qa.correctes;
            lignes.add(QuestionQrLiveStatResponse.builder()
                    .questionId(qa.questionId)
                    .numeroQuestion(qa.numeroQuestion)
                    .questionEnonce(qa.enonce)
                    .nombreReponses(qa.reponses)
                    .nombreCorrectes(qa.correctes)
                    .nombreIncorrectes(incorrectes)
                    .pourcentageReussite(txR)
                    .pourcentageEchec(txE)
                    .build());
        }

        double txGlobal = totalRep == 0 ? 0.0 : (totalCorrectes * 100.0 / totalRep);
        return QuizQrLiveStatsResponse.builder()
                .quizId(quiz.quizId)
                .quizTitre(quiz.quizTitre)
                .nombreParticipants(quiz.participants.size())
                .totalSoumissionsQuestions(totalRep)
                .tauxReussiteGlobal(txGlobal)
                .statistiquesParQuestion(lignes)
                .build();
    }

    public synchronized void clear(long quizId) {
        byQuiz.remove(quizId);
    }
}
