
package com.smartest.backend.service;

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

        List<Question> result = questionService.getByType(TypeQuestion.QCM);

        assertFalse(result.isEmpty());
    }

    //  TEST DELETE
    @Test
    void testDelete() {

        doNothing().when(questionRepository).deleteById(1L);

        questionService.delete(1L);

        verify(questionRepository, times(1)).deleteById(1L);
    }
}