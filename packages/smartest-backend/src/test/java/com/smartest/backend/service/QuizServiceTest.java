/*package com.smartest.backend.service;

import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.repository.*;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Arrays;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class QuizServiceTest {

    @Mock
    private QuizRepository quizRepository;

    @Mock
    private ProfesseurRepository professeurRepository;

    @Mock
    private CoursRepository coursRepository;

    @Mock
    private QuestionRepository questionRepository;

    @InjectMocks
    private QuizService quizService;

    private Quiz quiz;
    private QuizRequest quizRequest;
    private Professeur professeur;
    private Cours cours;
    private Question question;

    @BeforeEach
    void setUp() {
        // Initialiser le professeur
        professeur = new Professeur();
        professeur.setId(1L);
        professeur.setNom("Prof Test");
        professeur.setEmail("prof@test.com");

        // Initialiser le cours
        cours = new Cours();
        cours.setId(1L);
        cours.setTitre("Cours Test");

        // Initialiser la question
        question = new Question();
        question.setId(1L);
        question.setEnonce("Question Test");

        // Initialiser le quiz
        quiz = new Quiz();
        quiz.setId(1L);
        quiz.setTitre("Quiz Test");
        quiz.setDuree(30);
        quiz.setProfesseur(professeur);
        quiz.setCours(cours);

        // Initialiser la requête
        quizRequest = new QuizRequest();
        quizRequest.setTitre("Quiz Test");
        quizRequest.setDuree(30);
        quizRequest.setProfesseurId(1L);
        quizRequest.setCoursId(1L);
    }

    // ==================== TESTS GET ALL ====================

    @Test
    void getAllQuizs_returnsAllQuizzes() {
        // Arrange
        List<Quiz> quizs = Arrays.asList(quiz);
        when(quizRepository.findAll()).thenReturn(quizs);

        // Act
        List<QuizResponse> result = quizService.getAllQuizs();

        // Assert
        assertThat(result).isNotEmpty();
        assertThat(result).hasSize(1);
        assertThat(result.get(0).getTitre()).isEqualTo("Quiz Test");
        verify(quizRepository, times(1)).findAll();
    }

    // ==================== TESTS GET BY ID ====================

    @Test
    void getQuizById_returnsQuiz_whenExists() {
        // Arrange
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));

        // Act
        QuizResponse result = quizService.getQuizById(1L);

        // Assert
        assertThat(result).isNotNull();
        assertThat(result.getId()).isEqualTo(1L);
        assertThat(result.getTitre()).isEqualTo("Quiz Test");
        verify(quizRepository, times(1)).findById(1L);
    }

    @Test
    void getQuizById_throwsException_whenNotFound() {
        // Arrange
        when(quizRepository.findById(99L)).thenReturn(Optional.empty());

        // Act & Assert
        assertThrows(RuntimeException.class, () -> quizService.getQuizById(99L));
        verify(quizRepository, times(1)).findById(99L);
    }

    // ==================== TESTS GET BY PROFESSEUR ====================

    @Test
    void getQuizsByProfesseur_returnsQuizzes() {
        // Arrange
        List<Quiz> quizs = Arrays.asList(quiz);
        when(quizRepository.findByProfesseurId(1L)).thenReturn(quizs);

        // Act
        List<QuizResponse> result = quizService.getQuizsByProfesseur(1L);

        // Assert
        assertThat(result).hasSize(1);
        assertThat(result.get(0).getTitre()).isEqualTo("Quiz Test");
        verify(quizRepository, times(1)).findByProfesseurId(1L);
    }

    // ==================== TESTS GET BY COURS ====================

    @Test
    void getQuizsByCours_returnsQuizzes_forGivenCours() {
        // Arrange
        List<Quiz> quizs = Arrays.asList(quiz);
        when(quizRepository.findByCoursId(1L)).thenReturn(quizs);

        // Act
        List<QuizResponse> result = quizService.getQuizsByCours(1L);

        // Assert
        assertThat(result).hasSize(1);
        assertThat(result.get(0).getCoursId()).isEqualTo(1L);
        verify(quizRepository, times(1)).findByCoursId(1L);
    }

    // ==================== TESTS CREATE QUIZ ====================

    @Test
    void createQuiz_createsAndReturnsQuiz_withCours() {
        // Arrange
        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(coursRepository.findById(1L)).thenReturn(Optional.of(cours));
        when(quizRepository.save(any(Quiz.class))).thenReturn(quiz);

        // Act
        QuizResponse result = quizService.createQuiz(quizRequest);

        // Assert
        assertThat(result).isNotNull();
        assertThat(result.getTitre()).isEqualTo("Quiz Test");
        verify(professeurRepository, times(1)).findById(1L);
        verify(coursRepository, times(1)).findById(1L);
        verify(quizRepository, times(1)).save(any(Quiz.class));
    }

    @Test
    void createQuiz_throwsException_whenProfesseurNotFound() {
        // Arrange
        when(professeurRepository.findById(99L)).thenReturn(Optional.empty());
        quizRequest.setProfesseurId(99L);

        // Act & Assert
        assertThrows(RuntimeException.class, () -> quizService.createQuiz(quizRequest));
        verify(professeurRepository, times(1)).findById(99L);
        verify(quizRepository, never()).save(any(Quiz.class));
    }

    @Test
    void createQuiz_throwsException_whenCoursNotFound() {
        // Arrange
        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(coursRepository.findById(99L)).thenReturn(Optional.empty());
        quizRequest.setCoursId(99L);

        // Act & Assert
        assertThrows(RuntimeException.class, () -> quizService.createQuiz(quizRequest));
        verify(coursRepository, times(1)).findById(99L);
        verify(quizRepository, never()).save(any(Quiz.class));
    }

    // ==================== TESTS UPDATE QUIZ ====================

    @Test
    void updateQuiz_updatesAndReturnsQuiz() {
        // Arrange
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(coursRepository.findById(1L)).thenReturn(Optional.of(cours));
        when(quizRepository.save(any(Quiz.class))).thenReturn(quiz);

        quizRequest.setTitre("Quiz Modifié");
        quizRequest.setDuree(45);

        // Act
        QuizResponse result = quizService.updateQuiz(1L, quizRequest);

        // Assert
        assertThat(result).isNotNull();
        assertThat(result.getTitre()).isEqualTo("Quiz Modifié");
        verify(quizRepository, times(1)).findById(1L);
        verify(quizRepository, times(1)).save(any(Quiz.class));
    }

    @Test
    void updateQuiz_throwsException_whenQuizNotFound() {
        // Arrange
        when(quizRepository.findById(99L)).thenReturn(Optional.empty());

        // Act & Assert
        assertThrows(RuntimeException.class, () -> quizService.updateQuiz(99L, quizRequest));
        verify(quizRepository, times(1)).findById(99L);
        verify(quizRepository, never()).save(any(Quiz.class));
    }

    // ==================== TESTS DELETE QUIZ ====================

    @Test
    void deleteQuiz_deletesQuiz_whenExists() {
        // Arrange
        when(quizRepository.existsById(1L)).thenReturn(true);
        doNothing().when(quizRepository).deleteById(1L);

        // Act
        quizService.deleteQuiz(1L);

        // Assert
        verify(quizRepository, times(1)).existsById(1L);
        verify(quizRepository, times(1)).deleteById(1L);
    }

    @Test
    void deleteQuiz_throwsException_whenNotFound() {
        // Arrange
        when(quizRepository.existsById(99L)).thenReturn(false);

        // Act & Assert
        assertThrows(RuntimeException.class, () -> quizService.deleteQuiz(99L));
        verify(quizRepository, times(1)).existsById(99L);
        verify(quizRepository, never()).deleteById(anyLong());
    }

    // ==================== TESTS ADD QUESTION TO QUIZ ====================

    @Test
    void addQuestionToQuiz_addsQuestion_whenNotAlreadyPresent() {
        // Arrange
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(questionRepository.findById(1L)).thenReturn(Optional.of(question));
        when(quizRepository.save(any(Quiz.class))).thenReturn(quiz);

        // Act
        QuizResponse result = quizService.addQuestionToQuiz(1L, 1L);

        // Assert
        assertThat(result).isNotNull();
        verify(quizRepository, times(1)).findById(1L);
        verify(questionRepository, times(1)).findById(1L);
        verify(quizRepository, times(1)).save(any(Quiz.class));
    }

    @Test
    void addQuestionToQuiz_doesNotDuplicate_whenAlreadyPresent() {
        // Arrange
        quiz.getQuestions().add(question);
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(questionRepository.findById(1L)).thenReturn(Optional.of(question));
        when(quizRepository.save(any(Quiz.class))).thenReturn(quiz);

        // Act
        QuizResponse result = quizService.addQuestionToQuiz(1L, 1L);

        // Assert
        assertThat(result).isNotNull();
        verify(quizRepository, times(1)).findById(1L);
        verify(questionRepository, times(1)).findById(1L);
        verify(quizRepository, times(1)).save(any(Quiz.class));
    }

    // ==================== TESTS REMOVE QUESTION FROM QUIZ ====================

    @Test
    void removeQuestionFromQuiz_removesQuestion_whenPresent() {
        // Arrange
        quiz.getQuestions().add(question);
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(quizRepository.save(any(Quiz.class))).thenReturn(quiz);

        // Act
        QuizResponse result = quizService.removeQuestionFromQuiz(1L, 1L);

        // Assert
        assertThat(result).isNotNull();
        verify(quizRepository, times(1)).findById(1L);
        verify(quizRepository, times(1)).save(any(Quiz.class));
    }

    @Test
    void removeQuestionFromQuiz_doesNothing_whenQuestionNotInQuiz() {
        // Arrange
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));
        when(quizRepository.save(any(Quiz.class))).thenReturn(quiz);

        // Act
        QuizResponse result = quizService.removeQuestionFromQuiz(1L, 99L);

        // Assert
        assertThat(result).isNotNull();
        verify(quizRepository, times(1)).findById(1L);
        verify(quizRepository, times(1)).save(any(Quiz.class));
    }

    // ==================== TESTS COUNT QUESTIONS ====================

    @Test
    void countQuestionsByQuizId_returnsCorrectCount() {
        // Arrange
        quiz.getQuestions().add(question);
        when(quizRepository.findById(1L)).thenReturn(Optional.of(quiz));

        // Act
        Long count = quizService.countQuestionsByQuizId(1L);

        // Assert
        assertThat(count).isEqualTo(1L);
        verify(quizRepository, times(1)).findById(1L);
    }

    // ==================== TESTS EXISTS BY ID ====================

    @Test
    void existsById_returnsTrue_whenQuizExists() {
        // Arrange
        when(quizRepository.existsById(1L)).thenReturn(true);

        // Act
        boolean exists = quizService.existsById(1L);

        // Assert
        assertThat(exists).isTrue();
        verify(quizRepository, times(1)).existsById(1L);
    }

    @Test
    void existsById_returnsFalse_whenQuizNotExists() {
        // Arrange
        when(quizRepository.existsById(99L)).thenReturn(false);

        // Act
        boolean exists = quizService.existsById(99L);

        // Assert
        assertThat(exists).isFalse();
        verify(quizRepository, times(1)).existsById(99L);
    }
}

 */