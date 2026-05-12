package com.smartest.backend.service;

import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Quiz;
import com.smartest.backend.entity.enumeration.StatutQuiz;
import com.smartest.backend.exception.QuizNotFoundException;
import com.smartest.backend.exception.UnauthorizedAccessException;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.QuizRepository;
import com.smartest.backend.repository.ReponseEtudiantRepository;
import com.smartest.backend.repository.ReponseRepository;
import com.smartest.backend.repository.ResultatRepository;
import com.smartest.backend.repository.StatistiqueQuestionRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.messaging.simp.SimpMessagingTemplate;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class QuizServiceTest {

    private static final String PROF_TEST_EMAIL = "prof@test.com";
    private static final String QUIZ_TEST_TITLE = "Quiz Test";

    @Mock
    private QuizRepository quizRepository;
    @Mock
    private ProfesseurRepository professeurRepository;
    @Mock
    private QuestionRepository questionRepository;
    @Mock
    private ResultatRepository resultatRepository;
    @Mock
    private ReponseRepository reponseRepository;
    @Mock
    private EtudiantRepository etudiantRepository;
    @Mock
    private StatistiqueQuestionRepository statistiqueQuestionRepository;
    @Mock
    private ReponseEtudiantRepository reponseEtudiantRepository;
    @Mock
    private EmailService emailService;
    @Mock
    private StatistiqueRecalculService statistiqueRecalculService;
    @Mock
    private StatistiqueService statistiqueService;
    @Mock
    private SimpMessagingTemplate messagingTemplate;

    @InjectMocks
    private QuizService quizService;

    private Quiz quiz;
    private QuizRequest quizRequest;
    private Professeur professeur;
    private Question question;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setId(1L);
        professeur.setNom("Prof Test");
        professeur.setEmail(PROF_TEST_EMAIL);

        question = new Question();
        question.setId(1L);
        question.setEnonce("Question Test");

        quiz = new Quiz();
        quiz.setId(1L);
        quiz.setTitre(QUIZ_TEST_TITLE);
        quiz.setProfesseur(professeur);
        quiz.setStatut(StatutQuiz.BROUILLON);
        quiz.setQuestions(new ArrayList<>());

        quizRequest = new QuizRequest();
        quizRequest.setTitre(QUIZ_TEST_TITLE);
        quizRequest.setProfesseurId(1L);
    }

    @Test
    void getAllQuizsReturnsAllQuizzes() {
        when(quizRepository.findAll()).thenReturn(List.of(quiz));

        List<QuizResponse> result = quizService.getAllQuizs();

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getTitre()).isEqualTo(QUIZ_TEST_TITLE);
        verify(quizRepository).findAll();
    }

    @Test
    void getQuizByIdReturnsQuizWhenExists() {
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));

        QuizResponse result = quizService.getQuizById(1L);

        assertThat(result.getId()).isEqualTo(1L);
        assertThat(result.getTitre()).isEqualTo(QUIZ_TEST_TITLE);
        verify(quizRepository).findById(1L);
    }

    @Test
    void getQuizByIdThrowsExceptionWhenNotFound() {
        when(quizRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.getQuizById(99L))
                .isInstanceOf(QuizNotFoundException.class)
                .hasMessageContaining("Quiz non trouvé");
    }

    @Test
    void createQuizCreatesAndReturnsQuiz() {
        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> {
            Quiz q = inv.getArgument(0);
            q.setId(1L);
            return q;
        });

        QuizResponse result = quizService.createQuiz(quizRequest);

        assertThat(result.getTitre()).isEqualTo(QUIZ_TEST_TITLE);
        assertThat(result.getProfesseurId()).isEqualTo(1L);
        verify(professeurRepository).findById(1L);
        verify(quizRepository).save(any(Quiz.class));
    }

    @Test
    void createQuizThrowsExceptionWhenProfesseurNotFound() {
        when(professeurRepository.findById(99L)).thenReturn(Optional.empty());
        quizRequest.setProfesseurId(99L);

        assertThatThrownBy(() -> quizService.createQuiz(quizRequest))
                .isInstanceOf(QuizNotFoundException.class)
                .hasMessageContaining("Professeur non trouvé");
        verify(quizRepository, never()).save(any(Quiz.class));
    }

    @Test
    void deleteQuizRemovesQuizWhenOwner() {
        when(professeurRepository.findByEmail(PROF_TEST_EMAIL)).thenReturn(Optional.of(professeur));
        when(quizRepository.findByIdWithQuestions(1L)).thenReturn(Optional.of(quiz));
        when(quizRepository.deleteNativeById(1L)).thenReturn(1);

        quizService.deleteQuiz(1L, PROF_TEST_EMAIL);

        verify(reponseEtudiantRepository).deleteByResultatQuizId(1L);
        verify(statistiqueQuestionRepository).deleteAllByQuizId(1L);
        verify(resultatRepository).deleteByQuizId(1L);
        verify(quizRepository).deleteNativeQuizQuestionLinks(1L);
        verify(quizRepository).deleteNativeQuizEmailWebRows(1L);
        verify(quizRepository).deleteNativeById(1L);
        verify(quizRepository, never()).delete(any());
    }

    @Test
    void deleteQuizForbidsWhenNotOwner() {
        Professeur autre = new Professeur();
        autre.setId(99L);
        quiz.setProfesseur(autre);

        when(professeurRepository.findByEmail(PROF_TEST_EMAIL)).thenReturn(Optional.of(professeur));
        when(quizRepository.findByIdWithQuestions(1L)).thenReturn(Optional.of(quiz));

        assertThatThrownBy(() -> quizService.deleteQuiz(1L, PROF_TEST_EMAIL))
                .isInstanceOf(UnauthorizedAccessException.class);
        verify(quizRepository, never()).delete(any());
        verify(quizRepository, never()).deleteNativeById(anyLong());
    }

    @Test
    void addQuestionToQuizAddsQuestion() {
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(questionRepository.findById(1L)).thenReturn(Optional.of(question));
        when(quizRepository.save(any(Quiz.class))).thenReturn(quiz);

        QuizResponse result = quizService.addQuestionToQuiz(1L, 1L);

        assertThat(result).isNotNull();
        verify(quizRepository).save(any(Quiz.class));
    }

    @Test
    void removeQuestionFromQuizRemovesQuestion() {
        quiz.getQuestions().add(question);
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(questionRepository.findById(1L)).thenReturn(Optional.of(question));
        when(quizRepository.save(any(Quiz.class))).thenReturn(quiz);

        quizService.removeQuestionFromQuiz(1L, 1L);

        verify(quizRepository).save(quiz);
    }
}
