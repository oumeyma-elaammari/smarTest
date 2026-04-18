package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.dto.request.ExamenRequest;
import com.smartest.backend.dto.response.ExamenResponse;
import com.smartest.backend.service.ExamenService;
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
class ExamenControllerTest {

    private MockMvc mockMvc;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Mock
    private ExamenService examenService;

    @InjectMocks
    private ExamenController examenController;

    private ExamenResponse examenResponse;
    private ExamenRequest examenRequest;

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
                .standaloneSetup(examenController)
                .setControllerAdvice(new TestExceptionHandler())
                .build();

        examenResponse = new ExamenResponse();
        examenResponse.setId(1L);
        examenResponse.setTitre("Examen de Java");
        examenResponse.setDuree(120);
        examenResponse.setProfesseurId(1L);
        examenResponse.setProfesseurNom("Dupont");
        examenResponse.setCoursId(10L);
        examenResponse.setCoursTitre("Algorithmique");

        examenRequest = new ExamenRequest();
        examenRequest.setTitre("Examen de Java");
        examenRequest.setDuree(120);
        examenRequest.setProfesseurId(1L);
        examenRequest.setCoursId(10L);
    }

    // ─── GET /api/examens ─────────────────────────────────────────────────────

    @Test
    void getAllExamens_returns200_withList() throws Exception {
        when(examenService.getAllExamens()).thenReturn(List.of(examenResponse));

        mockMvc.perform(get("/api/examens"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].id").value(1))
                .andExpect(jsonPath("$[0].titre").value("Examen de Java"))
                .andExpect(jsonPath("$[0].duree").value(120))
                .andExpect(jsonPath("$[0].professeurNom").value("Dupont"))
                .andExpect(jsonPath("$[0].coursTitre").value("Algorithmique"));
    }

    @Test
    void getAllExamens_returns200_withEmptyList() throws Exception {
        when(examenService.getAllExamens()).thenReturn(List.of());

        mockMvc.perform(get("/api/examens"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── GET /api/examens/{id} ────────────────────────────────────────────────

    @Test
    void getExamenById_returns200_whenFound() throws Exception {
        when(examenService.getExamenById(1L)).thenReturn(examenResponse);

        mockMvc.perform(get("/api/examens/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath("$.titre").value("Examen de Java"))
                .andExpect(jsonPath("$.duree").value(120));
    }

    @Test
    void getExamenById_returns500_whenNotFound() throws Exception {
        when(examenService.getExamenById(99L))
                .thenThrow(new RuntimeException("Examen non trouvé avec l'id: 99"));

        mockMvc.perform(get("/api/examens/99"))
                .andExpect(status().is5xxServerError());
    }

    // ─── GET /api/examens/professeur/{professeurId} ───────────────────────────

    @Test
    void getExamensByProfesseur_returns200_withList() throws Exception {
        when(examenService.getExamensByProfesseur(1L)).thenReturn(List.of(examenResponse));

        mockMvc.perform(get("/api/examens/professeur/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].professeurId").value(1));
    }

    @Test
    void getExamensByProfesseur_returns200_withEmptyList() throws Exception {
        when(examenService.getExamensByProfesseur(99L)).thenReturn(List.of());

        mockMvc.perform(get("/api/examens/professeur/99"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── GET /api/examens/cours/{coursId} ────────────────────────────────────

    @Test
    void getExamensByCours_returns200_withList() throws Exception {
        when(examenService.getExamensByCours(10L)).thenReturn(List.of(examenResponse));

        mockMvc.perform(get("/api/examens/cours/10"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].coursId").value(10));
    }

    @Test
    void getExamensByCours_returns200_withEmptyList() throws Exception {
        when(examenService.getExamensByCours(99L)).thenReturn(List.of());

        mockMvc.perform(get("/api/examens/cours/99"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── POST /api/examens ────────────────────────────────────────────────────

    @Test
    void createExamen_returns201_withCreatedExamen() throws Exception {
        when(examenService.createExamen(any(ExamenRequest.class))).thenReturn(examenResponse);

        mockMvc.perform(post("/api/examens")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(examenRequest)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath("$.titre").value("Examen de Java"))
                .andExpect(jsonPath("$.professeurId").value(1))
                .andExpect(jsonPath("$.coursId").value(10));
    }

    @Test
    void createExamen_returns500_whenProfesseurNotFound() throws Exception {
        when(examenService.createExamen(any(ExamenRequest.class)))
                .thenThrow(new RuntimeException("Professeur non trouvé avec l'id: 99"));

        examenRequest.setProfesseurId(99L);

        mockMvc.perform(post("/api/examens")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(examenRequest)))
                .andExpect(status().is5xxServerError());
    }

    @Test
    void createExamen_returns500_whenCoursNotFound() throws Exception {
        when(examenService.createExamen(any(ExamenRequest.class)))
                .thenThrow(new RuntimeException("Cours non trouvé avec l'id: 99"));

        examenRequest.setCoursId(99L);

        mockMvc.perform(post("/api/examens")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(examenRequest)))
                .andExpect(status().is5xxServerError());
    }

    // ─── PUT /api/examens/{id} ────────────────────────────────────────────────

    @Test
    void updateExamen_returns200_withUpdatedExamen() throws Exception {
        ExamenResponse updated = new ExamenResponse();
        updated.setId(1L);
        updated.setTitre("Examen modifié");
        updated.setDuree(150);
        updated.setProfesseurId(1L);

        when(examenService.updateExamen(eq(1L), any(ExamenRequest.class))).thenReturn(updated);

        examenRequest.setTitre("Examen modifié");
        examenRequest.setDuree(150);

        mockMvc.perform(put("/api/examens/1")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(examenRequest)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.titre").value("Examen modifié"))
                .andExpect(jsonPath("$.duree").value(150));
    }

    @Test
    void updateExamen_returns500_whenExamenNotFound() throws Exception {
        when(examenService.updateExamen(eq(99L), any(ExamenRequest.class)))
                .thenThrow(new RuntimeException("Examen non trouvé avec l'id: 99"));

        mockMvc.perform(put("/api/examens/99")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(examenRequest)))
                .andExpect(status().is5xxServerError());
    }

    // ─── DELETE /api/examens/{id} ─────────────────────────────────────────────

    @Test
    void deleteExamen_returns200_withSuccessMessage() throws Exception {
        doNothing().when(examenService).deleteExamen(1L);

        mockMvc.perform(delete("/api/examens/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.message").value("Examen supprimé avec succès"))
                .andExpect(jsonPath("$.success").value(true));
    }

    @Test
    void deleteExamen_returns500_whenExamenNotFound() throws Exception {
        doThrow(new RuntimeException("Examen non trouvé avec l'id: 99"))
                .when(examenService).deleteExamen(99L);

        mockMvc.perform(delete("/api/examens/99"))
                .andExpect(status().is5xxServerError());
    }

    // ─── POST /api/examens/{examenId}/questions/{questionId} ──────────────────

    @Test
    void addQuestion_returns200_withUpdatedExamen() throws Exception {
        examenResponse.setQuestions(List.of());
        when(examenService.addQuestionToExamen(1L, 50L)).thenReturn(examenResponse);

        mockMvc.perform(post("/api/examens/1/questions/50"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1));
    }

    @Test
    void addQuestion_returns500_whenExamenNotFound() throws Exception {
        when(examenService.addQuestionToExamen(99L, 50L))
                .thenThrow(new RuntimeException("Examen non trouvé avec l'id: 99"));

        mockMvc.perform(post("/api/examens/99/questions/50"))
                .andExpect(status().is5xxServerError());
    }

    @Test
    void addQuestion_returns500_whenQuestionNotFound() throws Exception {
        when(examenService.addQuestionToExamen(1L, 99L))
                .thenThrow(new RuntimeException("Question non trouvée avec l'id: 99"));

        mockMvc.perform(post("/api/examens/1/questions/99"))
                .andExpect(status().is5xxServerError());
    }

    // ─── DELETE /api/examens/{examenId}/questions/{questionId} ────────────────

    @Test
    void removeQuestion_returns200_withUpdatedExamen() throws Exception {
        when(examenService.removeQuestionFromExamen(1L, 50L)).thenReturn(examenResponse);

        mockMvc.perform(delete("/api/examens/1/questions/50"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1));
    }

    @Test
    void removeQuestion_returns500_whenExamenNotFound() throws Exception {
        when(examenService.removeQuestionFromExamen(99L, 50L))
                .thenThrow(new RuntimeException("Examen non trouvé avec l'id: 99"));

        mockMvc.perform(delete("/api/examens/99/questions/50"))
                .andExpect(status().is5xxServerError());
    }
}