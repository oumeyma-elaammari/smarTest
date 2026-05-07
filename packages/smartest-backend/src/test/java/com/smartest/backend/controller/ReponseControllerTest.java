package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.dto.request.ReponseRequest;
import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.exception.GlobalExceptionHandler;
import com.smartest.backend.exception.QuestionNotFoundException;
import com.smartest.backend.service.ReponseService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@ExtendWith(MockitoExtension.class)
@DisplayName("ReponseController — Tests MockMvc")
class ReponseControllerTest {

    private MockMvc mockMvc;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Mock
    private ReponseService reponseService;

    @InjectMocks
    private ReponseController reponseController;

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders.standaloneSetup(reponseController)
                .setControllerAdvice(new GlobalExceptionHandler())
                .build();
    }

    @Test
    @DisplayName("POST /api/reponses/quiz réponse valide → 200")
    void postQuizOk() throws Exception {
        ReponseResponse dto = new ReponseResponse();
        dto.setId(1L);
        dto.setCorrecte(true);
        when(reponseService.verifierReponse(10L, 200L, 50L)).thenReturn(dto);

        mockMvc.perform(post("/api/reponses/quiz")
                        .param("questionId", "10")
                        .param("reponseId", "200")
                        .param("etudiantId", "50"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.correcte").value(true));
    }

    @Test
    @DisplayName("POST /api/reponses/quiz question introuvable → 404 JSON")
    void postQuizQuestionInconnue() throws Exception {
        when(reponseService.verifierReponse(99L, 200L, 50L))
                .thenThrow(new QuestionNotFoundException(99L));

        mockMvc.perform(post("/api/reponses/quiz")
                        .param("questionId", "99")
                        .param("reponseId", "200")
                        .param("etudiantId", "50"))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.error").value("QUESTION_NOT_FOUND"));
    }

    @Test
    @DisplayName("POST /api/reponses/examen session valide → 200")
    void postExamenOk() throws Exception {
        doNothing().when(reponseService).enregistrerReponseExamen(anyLong(), anyLong(), anyLong(), anyLong());

        ReponseRequest req = new ReponseRequest();
        req.setQuestionId(10L);
        req.setReponseId(200L);
        req.setEtudiantId(50L);
        req.setSessionId(5L);

        mockMvc.perform(post("/api/reponses/examen")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isOk())
                .andExpect(content().string("Réponse enregistrée"));
    }
}
