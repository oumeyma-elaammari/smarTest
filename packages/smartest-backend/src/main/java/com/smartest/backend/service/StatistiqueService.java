package com.smartest.backend.service;

import com.smartest.backend.dto.response.StatistiqueQuestionResponse;
import com.smartest.backend.dto.response.StatistiquesQuizResponse;
import com.smartest.backend.exception.QuestionNotFoundException;
import com.smartest.backend.exception.QuizNotFoundException;
import com.smartest.backend.exception.UnauthorizedAccessException;
import com.smartest.backend.entity.ExamenCorrectionLigne;
import com.smartest.backend.entity.ExamenPassageResultat;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Quiz;
import com.smartest.backend.entity.StatistiqueQuestion;
import com.smartest.backend.repository.ExamenCorrectionLigneRepository;
import com.smartest.backend.repository.ExamenPassageResultatRepository;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.QuizRepository;
import com.smartest.backend.repository.ResultatRepository;
import com.smartest.backend.repository.StatistiqueQuestionRepository;
import org.springframework.http.HttpStatus;
import org.springframework.web.server.ResponseStatusException;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.stream.Collectors;

import com.smartest.backend.entity.Resultat;

/**
 * Statistiques de compréhension pour un quiz : basées uniquement sur la
 * <strong>première tentative</strong> sur les {@link com.smartest.backend.entity.Resultat} liés au
 * {@code quizId} (parcours quiz web / bureau — pas les examens en session, qui n’ont pas de {@code quizId}).
 */
@Service
@RequiredArgsConstructor
public class StatistiqueService {

    /** Alerte si plus de 70 % des réponses (1ʳᵉ tentative) sont incorrectes sur une question. */
    public static final double SEUIL_ALERTE_ECHEC_POURCENT = 70.0;

    private final StatistiqueQuestionRepository statistiqueQuestionRepository;
    private final ResultatRepository resultatRepository;
    private final QuizRepository quizRepository;
    private final QuestionRepository questionRepository;
    private final ProfesseurRepository professeurRepository;
    private final ExamenPublieRepository examenPublieRepository;
    private final ExamenPassageResultatRepository examenPassageResultatRepository;
    private final ExamenCorrectionLigneRepository examenCorrectionLigneRepository;

    /**
     * Statistiques à jour pour un quiz ; vérifie que le quiz appartient au professeur connecté.
     */
    @Transactional
    public StatistiquesQuizResponse obtenirStatistiquesQuizPourProfesseur(Long quizId, String professeurEmail) {
        verifierProprieteQuiz(quizId, professeurEmail);
        return construireEtPersisterStatistiquesQuiz(quizId);
    }

    /**
     * Recalcul persistant (sans contrôle prof), utilisé après délai ou par les jobs internes.
     */
    @Transactional
    public void recalculerStatistiquesQuizSync(Long quizId) {
        construireEtPersisterStatistiquesQuiz(quizId);
    }

    /**
     * Statistiques pour une question ; vérifie la propriété du quiz et que la question appartient au quiz.
     */
    @Transactional
    public StatistiqueQuestionResponse obtenirStatistiqueQuestionPourProfesseur(
            Long quizId,
            Long questionId,
            String professeurEmail) {
        Quiz quiz = verifierProprieteQuiz(quizId, professeurEmail);
        boolean questionDuQuiz = quiz.getQuestions().stream()
                .anyMatch(q -> q.getId().equals(questionId));
        if (!questionDuQuiz) {
            throw new QuestionNotFoundException("Question absente de ce quiz : " + questionId);
        }
        return calculerEtPersisterUneQuestion(quizId, questionId);
    }

    @Transactional(readOnly = true)
    public List<StatistiqueQuestionResponse> obtenirQuestionsAlertePourProfesseur(Long quizId, String professeurEmail) {
        verifierProprieteQuiz(quizId, professeurEmail);
        return statistiqueQuestionRepository.findAlertesByQuizId(quizId)
                .stream()
                .map(this::convertToDTO)
                .collect(Collectors.toList());
    }

    /**
     * Statistiques agrégées d’un examen publié (passages + lignes de correction).
     * Réutilise {@link StatistiquesQuizResponse} ({@code quizId} = id {@link ExamenPublie}).
     */
    @Transactional(readOnly = true)
    public StatistiquesQuizResponse obtenirStatistiquesExamenPourProfesseur(Long examenId, String professeurEmail) {
        ExamenPublie examen = verifierProprieteExamen(examenId, professeurEmail);
        return construireStatistiquesExamen(examen);
    }

    private ExamenPublie verifierProprieteExamen(Long examenId, String professeurEmail) {
        if (professeurEmail == null || professeurEmail.isBlank()) {
            throw new UnauthorizedAccessException("Professeur introuvable");
        }
        String email = professeurEmail.trim().toLowerCase(Locale.ROOT);
        Professeur prof = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new UnauthorizedAccessException("Professeur introuvable"));
        ExamenPublie examen = examenPublieRepository.findByIdWithQuestions(examenId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Examen introuvable"));
        if (examen.getProfesseur() == null || !examen.getProfesseur().getId().equals(prof.getId())) {
            throw new UnauthorizedAccessException("Cet examen n'appartient pas à votre compte");
        }
        return examen;
    }

    private Quiz verifierProprieteQuiz(Long quizId, String professeurEmail) {
        if (professeurEmail == null || professeurEmail.isBlank()) {
            throw new UnauthorizedAccessException("Professeur introuvable");
        }
        String email = professeurEmail.trim().toLowerCase(Locale.ROOT);
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new QuizNotFoundException("Quiz non trouvé"));
        Professeur prof = professeurRepository.findByEmail(email)
                .orElseThrow(() -> new UnauthorizedAccessException("Professeur introuvable"));
        if (quiz.getProfesseur() == null || !quiz.getProfesseur().getId().equals(prof.getId())) {
            throw new UnauthorizedAccessException("Ce quiz n'appartient pas à votre compte");
        }
        return quiz;
    }

    private StatistiquesQuizResponse construireEtPersisterStatistiquesQuiz(Long quizId) {
        Quiz quiz = quizRepository.findById(quizId)
                .orElseThrow(() -> new QuizNotFoundException("Quiz non trouvé"));

        List<StatistiqueQuestionResponse> statsParQuestion = new ArrayList<>();
        long participantsPremiere = resultatRepository.countEtudiantsDistinctsPremiereTentativePourQuiz(quizId);

        for (Question question : quiz.getQuestions()) {
            statsParQuestion.add(calculerEtPersisterUneQuestion(quizId, question.getId()));
        }

        double moyenneQuestions = statsParQuestion.stream()
                .mapToDouble(StatistiqueQuestionResponse::getPourcentageReussite)
                .average()
                .orElse(0);

        long nbAlertes = statsParQuestion.stream()
                .filter(s -> Boolean.TRUE.equals(s.getAlerteEchec()))
                .count();

        NotesPremiereTentative aggNotes = calculerNotesPremiereTentative(quizId);

        return StatistiquesQuizResponse.builder()
                .quizId(quizId)
                .quizTitre(quiz.getTitre())
                .nombreParticipants((int) Math.min(participantsPremiere, Integer.MAX_VALUE))
                .moyenneGenerale(moyenneQuestions)
                .tauxReussiteGlobal(moyenneQuestions)
                .moyenneNoteSur20(aggNotes.moyenneNoteSur20())
                .repartitionParTranche(aggNotes.repartition())
                .questionsAlerteCount(nbAlertes)
                .statistiquesParQuestion(statsParQuestion)
                .build();
    }

    private record NotesPremiereTentative(double moyenneNoteSur20, List<Integer> repartition) {}

    /**
     * Pour chaque étudiant (1ʳᵉ tentative), note sur 20 = bonnes / réponses enregistrées × 20,
     * puis histogramme sur les tranches imposées par le tableau de bord prof.
     */
    private NotesPremiereTentative calculerNotesPremiereTentative(Long quizId) {
        List<Resultat> lignes = resultatRepository.findByQuizId(quizId).stream()
                .filter(r -> Boolean.TRUE.equals(r.getEstPremiereTentative()))
                .toList();
        Map<Long, List<Resultat>> parEtudiant = lignes.stream()
                .collect(Collectors.groupingBy(r -> r.getEtudiant().getId()));

        int[] buckets = new int[7];
        double sumNotes = 0;
        int nbEtudiants = 0;

        for (List<Resultat> rows : parEtudiant.values()) {
            if (rows.isEmpty()) {
                continue;
            }
            long n = rows.size();
            long bonnes = rows.stream().filter(r -> Boolean.TRUE.equals(r.getCorrecte())).count();
            double noteSur20 = n == 0 ? 0.0 : (bonnes * 20.0 / n);
            sumNotes += noteSur20;
            nbEtudiants++;
            buckets[trancheIndexNoteSur20(noteSur20)]++;
        }

        double moyenne = nbEtudiants == 0 ? 0.0 : sumNotes / nbEtudiants;
        List<Integer> repartition = Arrays.stream(buckets).boxed().map(Integer::intValue).toList();
        return new NotesPremiereTentative(moyenne, repartition);
    }

    /** Tranches : [0,5), [5,8), … [18,20]. */
    private static int trancheIndexNoteSur20(double note) {
        if (note < 5) {
            return 0;
        }
        if (note < 8) {
            return 1;
        }
        if (note < 10) {
            return 2;
        }
        if (note < 12) {
            return 3;
        }
        if (note < 15) {
            return 4;
        }
        if (note < 18) {
            return 5;
        }
        return 6;
    }

    public StatistiqueQuestionResponse calculerEtPersisterUneQuestion(Long quizId, Long questionId) {

        long total = resultatRepository.countPremiereTentativePourQuestionPourQuiz(quizId, questionId);
        long correctes = resultatRepository.countPremiereTentativeCorrectesPourQuestionPourQuiz(quizId, questionId);
        long incorrectes = total - correctes;

        double pourcentageReussite = total > 0 ? (correctes * 100.0 / total) : 0;
        double pourcentageEchec = total > 0 ? (incorrectes * 100.0 / total) : 0;
        boolean alerteEchec = pourcentageEchec >= SEUIL_ALERTE_ECHEC_POURCENT;

        Question question = questionRepository.findById(questionId)
                .orElseThrow(() -> new QuestionNotFoundException(questionId));

        StatistiqueQuestion statistique = statistiqueQuestionRepository
                .findByQuestionIdAndQuizId(questionId, quizId)
                .orElseGet(StatistiqueQuestion::new);

        statistique.setQuestion(question);
        statistique.setQuiz(quizRepository.findById(quizId).orElse(null));
        statistique.setExamenPublie(null);
        /* Statistiques « quiz » : champs examen / session laissés vides (réservés au flux examens). */
        statistique.setSessionExamen(null);
        statistique.setNombreReponses((int) total);
        statistique.setNombreCorrectes((int) correctes);
        statistique.setNombreIncorrectes((int) incorrectes);
        statistique.setPourcentageReussite(pourcentageReussite);
        statistique.setPourcentageEchec(pourcentageEchec);
        statistique.setAGenereAlerte(alerteEchec);
        statistique.setAlerteEchec(alerteEchec);

        statistiqueQuestionRepository.save(statistique);

        return StatistiqueQuestionResponse.builder()
                .questionId(questionId)
                .questionEnonce(question.getEnonce())
                .typeQuestion(String.valueOf(question.getType()))
                .nombreReponses((int) total)
                .nombreCorrectes((int) correctes)
                .nombreIncorrectes((int) incorrectes)
                .pourcentageReussite(pourcentageReussite)
                .pourcentageEchec(pourcentageEchec)
                .alerteEchec(alerteEchec)
                .build();
    }

    private StatistiquesQuizResponse construireStatistiquesExamen(ExamenPublie examen) {
        Long examenId = examen.getId();
        List<Question> questions = examen.getQuestions() != null ? examen.getQuestions() : List.of();
        int nombreQuestions = Math.max(1, questions.size());

        List<ExamenPassageResultat> passages =
                examenPassageResultatRepository.findByExamenPublie_IdWithEtudiantOrderByEmail(examenId);
        double baremeReference = passages.stream()
                .mapToDouble(ExamenPassageResultat::getBaremeReference)
                .filter(b -> b > 0)
                .findFirst()
                .orElse(20.0);

        List<ExamenCorrectionLigne> lignes =
                examenCorrectionLigneRepository.findAllByExamenPublie_IdWithQuestion(examenId);

        List<StatistiqueQuestionResponse> statsParQuestion = new ArrayList<>();
        for (Question question : questions) {
            if (question == null || question.getId() == null) {
                continue;
            }
            Long questionId = question.getId();
            double pointsParQuestion = ExamenCorrectionService.pointsPourQuestion(
                    question, baremeReference, nombreQuestions);
            List<ExamenCorrectionLigne> lignesQuestion = lignes.stream()
                    .filter(l -> l.getQuestion() != null && questionId.equals(l.getQuestion().getId()))
                    .toList();
            long total = lignesQuestion.size();
            long correctes = lignesQuestion.stream()
                    .filter(l -> estLigneReussie(l, pointsParQuestion))
                    .count();
            long incorrectes = total - correctes;
            double pourcentageReussite = total > 0 ? (correctes * 100.0 / total) : 0;
            double pourcentageEchec = total > 0 ? (incorrectes * 100.0 / total) : 0;
            boolean alerteEchec = pourcentageEchec >= SEUIL_ALERTE_ECHEC_POURCENT;

            statsParQuestion.add(StatistiqueQuestionResponse.builder()
                    .questionId(questionId)
                    .questionEnonce(question.getEnonce())
                    .typeQuestion(String.valueOf(question.getType()))
                    .nombreReponses((int) total)
                    .nombreCorrectes((int) correctes)
                    .nombreIncorrectes((int) incorrectes)
                    .pourcentageReussite(pourcentageReussite)
                    .pourcentageEchec(pourcentageEchec)
                    .alerteEchec(alerteEchec)
                    .build());
        }

        double moyenneQuestions = statsParQuestion.stream()
                .mapToDouble(StatistiqueQuestionResponse::getPourcentageReussite)
                .average()
                .orElse(0);

        long nbAlertes = statsParQuestion.stream()
                .filter(s -> Boolean.TRUE.equals(s.getAlerteEchec()))
                .count();

        NotesPremiereTentative aggNotes = calculerNotesExamen(passages);

        return StatistiquesQuizResponse.builder()
                .quizId(examenId)
                .quizTitre(examen.getTitre())
                .nombreParticipants(passages.size())
                .moyenneGenerale(moyenneQuestions)
                .tauxReussiteGlobal(moyenneQuestions)
                .moyenneNoteSur20(aggNotes.moyenneNoteSur20())
                .repartitionParTranche(aggNotes.repartition())
                .questionsAlerteCount(nbAlertes)
                .statistiquesParQuestion(statsParQuestion)
                .build();
    }

    private static double scoreEffectifLigne(ExamenCorrectionLigne ligne) {
        if (ligne.getNoteFinaleLigne() != null) {
            return ligne.getNoteFinaleLigne();
        }
        return ligne.getScorePartiel() != null ? ligne.getScorePartiel() : 0.0;
    }

    /** Réussite : au moins la moitié des points alloués à la question (prise en charge du partiel). */
    private static boolean estLigneReussie(ExamenCorrectionLigne ligne, double pointsParQuestion) {
        if (pointsParQuestion <= 0) {
            return false;
        }
        return scoreEffectifLigne(ligne) >= pointsParQuestion * 0.5 - 1e-6;
    }

    private NotesPremiereTentative calculerNotesExamen(List<ExamenPassageResultat> passages) {
        int[] buckets = new int[7];
        double sumNotes = 0;
        int nbEtudiants = 0;

        for (ExamenPassageResultat passage : passages) {
            double bareme = passage.getBaremeReference() > 0 ? passage.getBaremeReference() : 20.0;
            double note = passage.getNoteFinale() != null ? passage.getNoteFinale() : passage.getNoteProposee();
            double noteSur20 = bareme <= 0 ? 0.0 : (note * 20.0 / bareme);
            sumNotes += noteSur20;
            nbEtudiants++;
            buckets[trancheIndexNoteSur20(noteSur20)]++;
        }

        double moyenne = nbEtudiants == 0 ? 0.0 : sumNotes / nbEtudiants;
        List<Integer> repartition = Arrays.stream(buckets).boxed().map(Integer::intValue).toList();
        return new NotesPremiereTentative(moyenne, repartition);
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
                .alerteEchec(Boolean.TRUE.equals(stat.getAlerteEchec())
                        || Boolean.TRUE.equals(stat.getAGenereAlerte()))
                .build();
    }
}
