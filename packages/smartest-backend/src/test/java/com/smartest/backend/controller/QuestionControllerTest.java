package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.service.QuestionService;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import java.util.List;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@ExtendWith(MockitoExtension.class)  // ← pas de Spring, juste Mockito
class QuestionControllerTest {

    private MockMvc mockMvc;

    @Mock
    private QuestionService questionService;

    @InjectMocks
    private QuestionController questionController;  // ← le vrai contrôleur

    private ObjectMapper objectMapper = new ObjectMapper();

    @BeforeEach
    void setUp() {
        // Construction manuelle de MockMvc — pas besoin de Spring Boot
        mockMvc = MockMvcBuilders
                .standaloneSetup(questionController)
                .build();
    }

    @Test
    void testGetAllQuestions() throws Exception {
        Question q = new Question();
        q.setEnonce("Question test");
        when(questionService.getAll()).thenReturn(List.of(q));

        mockMvc.perform(get("/api/questions"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1));
    }

    @Test
    void testGetByType() throws Exception {
        when(questionService.getByType(TypeQuestion.QCM))
                .thenReturn(List.of(new Question()));

        mockMvc.perform(get("/api/questions/type")
                        .param("type", "QCM"))
                .andExpect(status().isOk());
    }

    @Test
    void testCreateQuestion() throws Exception {
        Question q = new Question();
        q.setEnonce("Nouvelle question");
        q.setType(TypeQuestion.QCM);
        q.setDifficulte(Difficulte.FACILE);

        when(questionService.createQuestion(
                org.mockito.ArgumentMatchers.any(),
                org.mockito.ArgumentMatchers.eq(1L),
                org.mockito.ArgumentMatchers.eq(1L)
        )).thenReturn(q);

        mockMvc.perform(post("/api/questions")
                        .param("professeurId", "1")
                        .param("coursId", "1")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(q)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.enonce").value("Nouvelle question"));
    }

    @Test
    void testDeleteQuestion() throws Exception {
        mockMvc.perform(delete("/api/questions/1"))
                .andExpect(status().isOk()); // ← 200 au lieu de 204
    }
}