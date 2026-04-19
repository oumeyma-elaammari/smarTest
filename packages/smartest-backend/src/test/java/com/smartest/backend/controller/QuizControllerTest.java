/*package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.service.QuizService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

import java.util.List;

import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@ExtendWith(MockitoExtension.class)
class QuizControllerTest {

    private MockMvc mockMvc;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Mock
    private QuizService quizService;

    @InjectMocks
    private QuizController quizController;

    private QuizResponse quizResponse;
    private QuizRequest quizRequest;

    @RestControllerAdvice
    static class TestExceptionHandler {
        @ExceptionHandler(RuntimeException.class)
        public ResponseEntity<String> handleRuntime(RuntimeException ex) {
            return ResponseEntity.status(500).body(ex.getMessage());
        }
    }

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders
                .standaloneSetup(quizController)
                .setControllerAdvice(new TestExceptionHandler())
                .build();

        quizResponse = new QuizResponse();
        quizResponse.setId(1L);
        quizResponse.setTitre("Quiz de géographie");
        quizResponse.setDuree(30);
        quizResponse.setProfesseurId(1L);
        quizResponse.setProfesseurNom("Dupont");
        quizResponse.setCoursId(10L);
        quizResponse.setCoursTitre("Mathématiques");

        quizRequest = new QuizRequest();
        quizRequest.setTitre("Quiz de géographie");
        quizRequest.setDuree(30);
        quizRequest.setProfesseurId(1L);
        quizRequest.setCoursId(10L);
    }

    // ─── GET /api/quizs ───────────────────────────────────────────────────────

    @Test
    void getAllQuizs_returns200_withListOfQuizzes() throws Exception {
        when(quizService.getAllQuizs()).thenReturn(List.of(quizResponse));

        mockMvc.perform(get("/api/quizs"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].id").value(1))
                .andExpect(jsonPath("$[0].titre").value("Quiz de géographie"))
                .andExpect(jsonPath("$[0].duree").value(30))
                .andExpect(jsonPath("$[0].professeurNom").value("Dupont"))
                .andExpect(jsonPath("$[0].coursTitre").value("Mathématiques"));
    }

    @Test
    void getAllQuizs_returns200_withEmptyList() throws Exception {
        when(quizService.getAllQuizs()).thenReturn(List.of());

        mockMvc.perform(get("/api/quizs"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── GET /api/quizs/{id} ──────────────────────────────────────────────────

    @Test
    void getQuizById_returns200_whenFound() throws Exception {
        when(quizService.getQuizById(1L)).thenReturn(quizResponse);

        mockMvc.perform(get("/api/quizs/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath("$.titre").value("Quiz de géographie"))
                .andExpect(jsonPath("$.duree").value(30));
    }

    @Test
    void getQuizById_returns500_whenNotFound() throws Exception {
        when(quizService.getQuizById(99L))
                .thenThrow(new RuntimeException("Quiz non trouvé avec l'id: 99"));

        mockMvc.perform(get("/api/quizs/99"))
                .andExpect(status().is5xxServerError());
    }

    // ─── POST /api/quizs ──────────────────────────────────────────────────────

    @Test
    void createQuiz_returns201_withCreatedQuiz() throws Exception {
        when(quizService.createQuiz(any(QuizRequest.class))).thenReturn(quizResponse);

        mockMvc.perform(post("/api/quizs")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(quizRequest)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath("$.titre").value("Quiz de géographie"))
                .andExpect(jsonPath("$.professeurId").value(1))
                .andExpect(jsonPath("$.coursId").value(10));
    }

    @Test
    void createQuiz_returns500_whenProfesseurNotFound() throws Exception {
        when(quizService.createQuiz(any(QuizRequest.class)))
                .thenThrow(new RuntimeException("Professeur non trouvé avec l'id: 99"));

        quizRequest.setProfesseurId(99L);

        mockMvc.perform(post("/api/quizs")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(quizRequest)))
                .andExpect(status().is5xxServerError());
    }

    // ─── PUT /api/quizs/{id} ──────────────────────────────────────────────────

    @Test
    void updateQuiz_returns200_withUpdatedQuiz() throws Exception {
        QuizResponse updated = new QuizResponse();
        updated.setId(1L);
        updated.setTitre("Quiz modifié");
        updated.setDuree(60);
        updated.setProfesseurId(1L);

        when(quizService.updateQuiz(eq(1L), any(QuizRequest.class))).thenReturn(updated);

        quizRequest.setTitre("Quiz modifié");
        quizRequest.setDuree(60);

        mockMvc.perform(put("/api/quizs/1")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(quizRequest)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.titre").value("Quiz modifié"))
                .andExpect(jsonPath("$.duree").value(60));
    }

    @Test
    void updateQuiz_returns500_whenQuizNotFound() throws Exception {
        when(quizService.updateQuiz(eq(99L), any(QuizRequest.class)))
                .thenThrow(new RuntimeException("Quiz non trouvé avec l'id: 99"));

        mockMvc.perform(put("/api/quizs/99")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(quizRequest)))
                .andExpect(status().is5xxServerError());
    }

    // ─── DELETE /api/quizs/{id} ───────────────────────────────────────────────

    @Test
    void deleteQuiz_returns200_withSuccessMessage() throws Exception {
        doNothing().when(quizService).deleteQuiz(1L);

        mockMvc.perform(delete("/api/quizs/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.message").value("Quiz supprimé avec succès"))
                .andExpect(jsonPath("$.success").value(true));
    }

    @Test
    void deleteQuiz_returns500_whenQuizNotFound() throws Exception {
        doThrow(new RuntimeException("Quiz non trouvé avec l'id: 99"))
                .when(quizService).deleteQuiz(99L);

        mockMvc.perform(delete("/api/quizs/99"))
                .andExpect(status().is5xxServerError());
    }
}

 */