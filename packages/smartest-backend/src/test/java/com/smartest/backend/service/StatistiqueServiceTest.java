package com.smartest.backend.service;

import com.smartest.backend.dto.response.StatistiqueQuestionResponse;
import com.smartest.backend.dto.response.StatistiquesQuizResponse;
import com.smartest.backend.exception.QuestionNotFoundException;
import com.smartest.backend.exception.UnauthorizedAccessException;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Quiz;
import com.smartest.backend.entity.StatistiqueQuestion;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.QuizRepository;
import com.smartest.backend.repository.ResultatRepository;
import com.smartest.backend.repository.StatistiqueQuestionRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
@DisplayName("StatistiqueService — Tests unitaires")
class StatistiqueServiceTest {

    private static final String EMAIL_PROPRIETAIRE = "prof@univ.fr";

    @Mock
    private StatistiqueQuestionRepository statistiqueQuestionRepository;
    @Mock
    private ResultatRepository resultatRepository;
    @Mock
    private QuizRepository quizRepository;
    @Mock
    private QuestionRepository questionRepository;
    @Mock
    private ProfesseurRepository professeurRepository;

    @InjectMocks
    private StatistiqueService statistiqueService;

    private Professeur proprietaire;
    private Professeur autreProf;
    private Quiz quiz;
    private Question question;

    @BeforeEach
    void setUp() {
        proprietaire = new Professeur();
        proprietaire.setId(1L);
        proprietaire.setEmail(EMAIL_PROPRIETAIRE);

        autreProf = new Professeur();
        autreProf.setId(2L);
        autreProf.setEmail("autre@univ.fr");

        question = new Question();
        question.setId(100L);
        question.setEnonce("Q1");
        question.setType(TypeQuestion.QCM);
        question.setDifficulte(Difficulte.MOYEN);

        quiz = new Quiz();
        quiz.setId(10L);
        quiz.setTitre("Quiz stats");
        quiz.setProfesseur(proprietaire);
        quiz.setQuestions(new ArrayList<>(List.of(question)));
    }

    @Test
    @DisplayName("Quiz du professeur connecté → statistiques construites")
    void obtenirStatistiquesQuizProprietaireRetourneStats() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quiz));
        when(professeurRepository.findByEmail(EMAIL_PROPRIETAIRE)).thenReturn(Optional.of(proprietaire));
        when(resultatRepository.countEtudiantsDistinctsPremiereTentativePourQuiz(10L)).thenReturn(0L);
        when(resultatRepository.findByQuizId(10L)).thenReturn(List.of());
        when(resultatRepository.countPremiereTentativePourQuestionPourQuiz(10L, 100L)).thenReturn(5L);
        when(resultatRepository.countPremiereTentativeCorrectesPourQuestionPourQuiz(10L, 100L)).thenReturn(3L);
        when(questionRepository.findById(100L)).thenReturn(Optional.of(question));
        when(statistiqueQuestionRepository.findByQuestionIdAndQuizId(100L, 10L)).thenReturn(Optional.empty());
        when(statistiqueQuestionRepository.save(any(StatistiqueQuestion.class))).thenAnswer(inv -> inv.getArgument(0));

        StatistiquesQuizResponse dto =
                statistiqueService.obtenirStatistiquesQuizPourProfesseur(10L, EMAIL_PROPRIETAIRE);

        assertThat(dto.getQuizId()).isEqualTo(10L);
        assertThat(dto.getQuizTitre()).isEqualTo("Quiz stats");
        assertThat(dto.getStatistiquesParQuestion()).hasSize(1);
        verify(statistiqueQuestionRepository).save(any(StatistiqueQuestion.class));
    }

    @Test
    @DisplayName("Quiz d'un autre professeur → UnauthorizedAccessException")
    void obtenirStatistiquesQuizAutreProfesseurInterdit() {
        quiz.setProfesseur(autreProf);
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quiz));
        when(professeurRepository.findByEmail(EMAIL_PROPRIETAIRE)).thenReturn(Optional.of(proprietaire));

        assertThatThrownBy(() -> statistiqueService.obtenirStatistiquesQuizPourProfesseur(10L, EMAIL_PROPRIETAIRE))
                .isInstanceOf(UnauthorizedAccessException.class)
                .hasMessageContaining("appartient pas");
    }

    @Test
    @DisplayName("Quiz sans résultats → stats vides mais réponse cohérente")
    void obtenirStatistiquesQuizSansResultatsRetourneZerosSansErreur() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quiz));
        when(professeurRepository.findByEmail(EMAIL_PROPRIETAIRE)).thenReturn(Optional.of(proprietaire));
        when(resultatRepository.countEtudiantsDistinctsPremiereTentativePourQuiz(10L)).thenReturn(0L);
        when(resultatRepository.findByQuizId(10L)).thenReturn(List.of());
        when(resultatRepository.countPremiereTentativePourQuestionPourQuiz(10L, 100L)).thenReturn(0L);
        when(resultatRepository.countPremiereTentativeCorrectesPourQuestionPourQuiz(10L, 100L)).thenReturn(0L);
        when(questionRepository.findById(100L)).thenReturn(Optional.of(question));
        when(statistiqueQuestionRepository.findByQuestionIdAndQuizId(100L, 10L)).thenReturn(Optional.empty());
        when(statistiqueQuestionRepository.save(any(StatistiqueQuestion.class))).thenAnswer(inv -> inv.getArgument(0));

        StatistiquesQuizResponse dto =
                statistiqueService.obtenirStatistiquesQuizPourProfesseur(10L, EMAIL_PROPRIETAIRE);

        assertThat(dto.getNombreParticipants()).isZero();
        assertThat(dto.getStatistiquesParQuestion()).hasSize(1);
        assertThat(dto.getStatistiquesParQuestion().get(0).getNombreReponses()).isZero();
    }

    @Test
    @DisplayName("Questions alerte → liste filtrée par le dépôt")
    void obtenirQuestionsAlerteRetourneLesAlertesPersistance() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quiz));
        when(professeurRepository.findByEmail(EMAIL_PROPRIETAIRE)).thenReturn(Optional.of(proprietaire));

        StatistiqueQuestion sq = StatistiqueQuestion.builder()
                .question(question)
                .nombreReponses(10)
                .nombreCorrectes(2)
                .nombreIncorrectes(8)
                .pourcentageReussite(20.0)
                .pourcentageEchec(80.0)
                .alerteEchec(true)
                .build();

        when(statistiqueQuestionRepository.findAlertesByQuizId(10L)).thenReturn(List.of(sq));

        List<StatistiqueQuestionResponse> alertes =
                statistiqueService.obtenirQuestionsAlertePourProfesseur(10L, EMAIL_PROPRIETAIRE);

        assertThat(alertes).hasSize(1);
        assertThat(alertes.get(0).getPourcentageEchec()).isGreaterThanOrEqualTo(
                StatistiqueService.SEUIL_ALERTE_ECHEC_POURCENT);
    }

    @Test
    @DisplayName("Aucune alerte persistée → liste vide")
    void obtenirQuestionsAlerteAucuneRetourneListeVide() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quiz));
        when(professeurRepository.findByEmail(EMAIL_PROPRIETAIRE)).thenReturn(Optional.of(proprietaire));
        when(statistiqueQuestionRepository.findAlertesByQuizId(10L)).thenReturn(List.of());

        List<StatistiqueQuestionResponse> alertes =
                statistiqueService.obtenirQuestionsAlertePourProfesseur(10L, EMAIL_PROPRIETAIRE);

        assertThat(alertes).isEmpty();
    }

    @Test
    @DisplayName("Question hors quiz → QuestionNotFoundException")
    void obtenirStatistiqueQuestionQuestionAbsenteDuQuiz() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quiz));
        when(professeurRepository.findByEmail(EMAIL_PROPRIETAIRE)).thenReturn(Optional.of(proprietaire));

        assertThatThrownBy(() ->
                statistiqueService.obtenirStatistiqueQuestionPourProfesseur(10L, 999L, EMAIL_PROPRIETAIRE))
                .isInstanceOf(QuestionNotFoundException.class)
                .hasMessageContaining("999");
    }
}
