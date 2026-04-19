
/*package com.smartest.backend.service;

import com.smartest.backend.dto.request.QuestionRequest;
import com.smartest.backend.dto.response.QuestionResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.*;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.*;

import java.util.List;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.Mockito.*;

public class QuestionServiceTest {

    @Mock
    private QuestionRepository questionRepository;

    @Mock
    private ProfesseurRepository professeurRepository;

    @Mock
    private CoursRepository coursRepository;

    @InjectMocks
    private QuestionService questionService;
    private Question question;


    @BeforeEach
    void setup() {
        MockitoAnnotations.openMocks(this);
    }

    //  TEST CREATE
    @Test
    void testCreateQuestion() {

        Question q = new Question();
        q.setEnonce("Test");

        Professeur prof = new Professeur();
        prof.setId(1L);

        when(professeurRepository.findById(1L))
                .thenReturn(Optional.of(prof));

        when(questionRepository.save(any()))
                .thenReturn(q);

        Question result = questionService.createQuestion(q, 1L, null);

        assertNotNull(result);
        assertEquals("Test", result.getEnonce());
    }

    //  TEST GET ALL
    @Test
    void testGetAll() {

        when(questionRepository.findAll())
                .thenReturn(List.of(new Question()));

        List<Question> result = questionService.getAll();

        assertEquals(1, result.size());
    }

    //  TEST GET BY TYPE
    @Test
    void testGetByType() {

        when(questionRepository.findByType(TypeQuestion.QCM))
                .thenReturn(List.of(new Question()));

        List<QuestionResponse> result = questionService.getByType(TypeQuestion.QCM);

        assertFalse(result.isEmpty());
    }


    // TEST UPDATE
    @Test
    void testUpdateQuestion() {
        // Arrange
        Question existingQuestion = new Question();
        existingQuestion.setId(1L);
        existingQuestion.setEnonce("Ancienne question");
        existingQuestion.setType(TypeQuestion.valueOf(String.valueOf(TypeQuestion.QCM)));
        existingQuestion.setDifficulte(Difficulte.valueOf(String.valueOf(Difficulte.MOYEN)));

        QuestionRequest request = new QuestionRequest();
        request.setEnonce("Nouvelle question");
        request.setType("QCM");  // ← String
        request.setDifficulte("DIFFICILE");

        when(questionRepository.findById(1L)).thenReturn(Optional.of(existingQuestion));
        when(questionRepository.save(any(Question.class))).thenReturn(existingQuestion);

        // Act
        QuestionResponse result = questionService.updateQuestion(1L, request, request.getCoursId());

        // Assert
        assertEquals("Nouvelle question", result.getEnonce());

        // ✅ CORRIGÉ : Comparer avec name() de l'Enum ou directement en String
        assertEquals("QCM", result.getType());  // ← Compare String avec String
        assertEquals("DIFFICILE", result.getDifficulte());
    }

    //  TEST DELETE
    @Test
    void testDelete_Success() {
        // Arrange
        when(questionRepository.findById(1L)).thenReturn(Optional.of(question));
        doNothing().when(questionRepository).delete(question);

        // Act
        questionService.delete(1L);

        // Assert
        verify(questionRepository, times(1)).findById(1L);
        verify(questionRepository, times(1)).delete(question);
    }

    @Test
    void testDelete_QuestionNotFound() {
        // Arrange
        when(questionRepository.findById(99L)).thenReturn(Optional.empty());

        // Act & Assert
        assertThrows(RuntimeException.class, () -> questionService.delete(99L));
        verify(questionRepository, times(1)).findById(99L);
        verify(questionRepository, never()).delete(any());
    }
}

 */