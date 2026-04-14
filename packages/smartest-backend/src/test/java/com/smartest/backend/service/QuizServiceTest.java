package com.smartest.backend.service;

import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.*;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class QuizServiceTest {

    @Mock private QuizRepository quizRepository;
    @Mock private ProfesseurRepository professeurRepository;
    @Mock private CoursRepository coursRepository;
    @Mock private QuestionRepository questionRepository;

    @InjectMocks
    private QuizService quizService;

    private Quiz quiz;
    private Professeur professeur;
    private Cours cours;
    private Question question;
    private Reponse reponse;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setId(1L);
        professeur.setNom("Dupont");

        cours = new Cours();
        cours.setId(10L);
        cours.setTitre("Mathématiques");

        reponse = new Reponse();
        reponse.setId(100L);
        reponse.setContenu("Paris");
        reponse.setCorrecte(true);

        question = new Question();
        question.setId(50L);
        question.setEnonce("Quelle est la capitale de la France ?");
        question.setType(TypeQuestion.QCM);
        question.setDifficulte(Difficulte.FACILE);
        question.setReponses(List.of(reponse));

        quiz = new Quiz();
        quiz.setId(1L);
        quiz.setTitre("Quiz de géographie");
        quiz.setDuree(30);
        quiz.setProfesseur(professeur);
        quiz.setCours(cours);
        quiz.setQuestions(new ArrayList<>(List.of(question)));
    }

    // ─── getAllQuizs ──────────────────────────────────────────────────────────

    @Test
    void getAllQuizs_returnsAllQuizzes() {
        when(quizRepository.findAll()).thenReturn(List.of(quiz));

        List<QuizResponse> result = quizService.getAllQuizs();

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getTitre()).isEqualTo("Quiz de géographie");
        assertThat(result.get(0).getProfesseurNom()).isEqualTo("Dupont");
        assertThat(result.get(0).getCoursTitre()).isEqualTo("Mathématiques");
    }

    @Test
    void getAllQuizs_returnsEmptyList_whenNoQuizzes() {
        when(quizRepository.findAll()).thenReturn(List.of());

        List<QuizResponse> result = quizService.getAllQuizs();

        assertThat(result).isEmpty();
    }

    // ─── getQuizById ──────────────────────────────────────────────────────────

    @Test
    void getQuizById_returnsQuiz_whenExists() {
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));

        QuizResponse result = quizService.getQuizById(1L);

        assertThat(result.getId()).isEqualTo(1L);
        assertThat(result.getTitre()).isEqualTo("Quiz de géographie");
        assertThat(result.getDuree()).isEqualTo(30);
    }

    @Test
    void getQuizById_throwsException_whenNotFound() {
        when(quizRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.getQuizById(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── getQuizsByProfesseur ─────────────────────────────────────────────────

    @Test
    void getQuizsByProfesseur_returnsQuizzes_forGivenProfesseur() {
        when(quizRepository.findByProfesseurId(1L)).thenReturn(List.of(quiz));

        List<QuizResponse> result = quizService.getQuizsByProfesseur(1L);

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getProfesseurId()).isEqualTo(1L);
    }

    @Test
    void getQuizsByProfesseur_returnsEmptyList_whenNoneFound() {
        when(quizRepository.findByProfesseurId(99L)).thenReturn(List.of());

        List<QuizResponse> result = quizService.getQuizsByProfesseur(99L);

        assertThat(result).isEmpty();
    }

    // ─── getQuizsByCours ──────────────────────────────────────────────────────

    @Test
    void getQuizsByCours_returnsQuizzes_forGivenCours() {
        when(quizRepository.findByCoursId(10L)).thenReturn(List.of(quiz));

        List<QuizResponse> result = quizService.getQuizsByCours(10L);

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getCoursId()).isEqualTo(10L);
    }

    // ─── createQuiz ───────────────────────────────────────────────────────────

    @Test
    void createQuiz_createsAndReturnsQuiz_withCours() {
        QuizRequest request = new QuizRequest();
        request.setTitre("Nouveau quiz");
        request.setDuree(45);
        request.setProfesseurId(1L);
        request.setCoursId(10L);
        request.setQuestionsIds(List.of(50L));

        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(coursRepository.findById(10L)).thenReturn(Optional.of(cours));
        when(questionRepository.findAllById(List.of(50L))).thenReturn(List.of(question));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> {
            Quiz q = inv.getArgument(0);
            q.setId(2L);
            return q;
        });

        QuizResponse result = quizService.createQuiz(request);

        assertThat(result.getId()).isEqualTo(2L);
        assertThat(result.getTitre()).isEqualTo("Nouveau quiz");
        assertThat(result.getDuree()).isEqualTo(45);
        assertThat(result.getProfesseurId()).isEqualTo(1L);
        assertThat(result.getCoursId()).isEqualTo(10L);
        assertThat(result.getQuestions()).hasSize(1);
    }

    @Test
    void createQuiz_createsQuiz_withoutCours() {
        QuizRequest request = new QuizRequest();
        request.setTitre("Quiz sans cours");
        request.setDuree(20);
        request.setProfesseurId(1L);
        request.setCoursId(null);

        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> inv.getArgument(0));

        QuizResponse result = quizService.createQuiz(request);

        assertThat(result.getCoursId()).isNull();
        assertThat(result.getCoursTitre()).isNull();
    }

    @Test
    void createQuiz_throwsException_whenProfesseurNotFound() {
        QuizRequest request = new QuizRequest();
        request.setProfesseurId(99L);

        when(professeurRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.createQuiz(request))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    @Test
    void createQuiz_throwsException_whenCoursNotFound() {
        QuizRequest request = new QuizRequest();
        request.setProfesseurId(1L);
        request.setCoursId(99L);

        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(coursRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.createQuiz(request))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── updateQuiz ───────────────────────────────────────────────────────────

    @Test
    void updateQuiz_updatesAndReturnsQuiz() {
        QuizRequest request = new QuizRequest();
        request.setTitre("Quiz modifié");
        request.setDuree(60);
        request.setProfesseurId(1L);
        request.setCoursId(10L);
        request.setQuestionsIds(List.of(50L));

        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(coursRepository.findById(10L)).thenReturn(Optional.of(cours));
        when(questionRepository.findAllById(List.of(50L))).thenReturn(List.of(question));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> inv.getArgument(0));

        QuizResponse result = quizService.updateQuiz(1L, request);

        assertThat(result.getTitre()).isEqualTo("Quiz modifié");
        assertThat(result.getDuree()).isEqualTo(60);
    }

    @Test
    void updateQuiz_setsCours_toNull_whenCoursIdIsNull() {
        QuizRequest request = new QuizRequest();
        request.setTitre("Quiz");
        request.setDuree(20);
        request.setProfesseurId(1L);
        request.setCoursId(null);

        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> inv.getArgument(0));

        QuizResponse result = quizService.updateQuiz(1L, request);

        assertThat(result.getCoursId()).isNull();
    }

    @Test
    void updateQuiz_throwsException_whenQuizNotFound() {
        when(quizRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.updateQuiz(99L, new QuizRequest()))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── deleteQuiz ───────────────────────────────────────────────────────────

    @Test
    void deleteQuiz_deletesQuiz_whenExists() {
        when(quizRepository.existsById(1L)).thenReturn(true);

        quizService.deleteQuiz(1L);

        verify(quizRepository, times(1)).deleteById(1L);
    }

    @Test
    void deleteQuiz_throwsException_whenNotFound() {
        when(quizRepository.existsById(99L)).thenReturn(false);

        assertThatThrownBy(() -> quizService.deleteQuiz(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");

        verify(quizRepository, never()).deleteById(any());
    }

    // ─── addQuestionToQuiz ────────────────────────────────────────────────────

    @Test
    void addQuestionToQuiz_addsQuestion_whenNotAlreadyPresent() {
        quiz.setQuestions(new ArrayList<>());
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(questionRepository.findById(50L)).thenReturn(Optional.of(question));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> inv.getArgument(0));

        QuizResponse result = quizService.addQuestionToQuiz(1L, 50L);

        assertThat(result.getQuestions()).hasSize(1);
    }

    @Test
    void addQuestionToQuiz_doesNotDuplicate_whenAlreadyPresent() {
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(questionRepository.findById(50L)).thenReturn(Optional.of(question));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> inv.getArgument(0));

        QuizResponse result = quizService.addQuestionToQuiz(1L, 50L);

        assertThat(result.getQuestions()).hasSize(1);
    }

    @Test
    void addQuestionToQuiz_throwsException_whenQuizNotFound() {
        when(quizRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.addQuestionToQuiz(99L, 50L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    @Test
    void addQuestionToQuiz_throwsException_whenQuestionNotFound() {
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(questionRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.addQuestionToQuiz(1L, 99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── removeQuestionFromQuiz ───────────────────────────────────────────────

    @Test
    void removeQuestionFromQuiz_removesQuestion() {
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> inv.getArgument(0));

        QuizResponse result = quizService.removeQuestionFromQuiz(1L, 50L);

        assertThat(result.getQuestions()).isNullOrEmpty();
    }

    @Test
    void removeQuestionFromQuiz_doesNothing_whenQuestionNotInQuiz() {
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> inv.getArgument(0));

        QuizResponse result = quizService.removeQuestionFromQuiz(1L, 999L);

        assertThat(result.getQuestions()).hasSize(1);
    }

    // ─── countQuestionsByQuizId ───────────────────────────────────────────────

    @Test
    void countQuestionsByQuizId_returnsCorrectCount() {
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));

        Long count = quizService.countQuestionsByQuizId(1L);

        assertThat(count).isEqualTo(1L);
    }

    @Test
    void countQuestionsByQuizId_throwsException_whenQuizNotFound() {
        when(quizRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.countQuestionsByQuizId(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── existsById ───────────────────────────────────────────────────────────

    @Test
    void existsById_returnsTrue_whenQuizExists() {
        when(quizRepository.existsById(1L)).thenReturn(true);

        assertThat(quizService.existsById(1L)).isTrue();
    }

    @Test
    void existsById_returnsFalse_whenQuizDoesNotExist() {
        when(quizRepository.existsById(99L)).thenReturn(false);

        assertThat(quizService.existsById(99L)).isFalse();
    }
}