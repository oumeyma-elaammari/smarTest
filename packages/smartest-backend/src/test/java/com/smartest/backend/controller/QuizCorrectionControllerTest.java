package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.dto.request.ReponseEtudiantRequest;
import com.smartest.backend.dto.response.CorrectionResponse;
import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.service.QuizCorrectionService;
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
class QuizCorrectionControllerTest {

    private MockMvc mockMvc;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Mock
    private QuizCorrectionService quizCorrectionService;

    @InjectMocks
    private QuizCorrectionController quizCorrectionController;

    private CorrectionResponse correctionCorrecte;
    private CorrectionResponse correctionIncorrecte;
    private ReponseEtudiantRequest request;

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
                .standaloneSetup(quizCorrectionController)
                .setControllerAdvice(new TestExceptionHandler())
                .build();

        ReponseResponse reponseCorrecte = new ReponseResponse();
        reponseCorrecte.setId(1L);
        reponseCorrecte.setContenu("Paris");
        reponseCorrecte.setCorrecte(true);

        correctionCorrecte = new CorrectionResponse(
                10L,
                "Quelle est la capitale de la France ?",
                1L,
                "Paris",
                true,
                List.of(reponseCorrecte),
                "Bonne réponse ! Paris est correct."
        );

        correctionIncorrecte = new CorrectionResponse(
                10L,
                "Quelle est la capitale de la France ?",
                2L,
                "Lyon",
                false,
                List.of(reponseCorrecte),
                "Mauvaise réponse. La bonne réponse était : Paris"
        );

        request = new ReponseEtudiantRequest();
        request.setQuestionId(10L);
        request.setReponseId(1L);
        request.setEtudiantId(5L);
    }

    // ─── POST /api/quiz-correction/question ───────────────────────────────────

    @Test
    void corrigerReponse_returns200_whenReponseCorrecte() throws Exception {
        when(quizCorrectionService.corrigerReponse(any(ReponseEtudiantRequest.class)))
                .thenReturn(correctionCorrecte);

        mockMvc.perform(post("/api/quiz-correction/question")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.correct").value(true))
                .andExpect(jsonPath("$.questionId").value(10))
                .andExpect(jsonPath("$.reponseChoisieId").value(1))
                .andExpect(jsonPath("$.reponseChoisieContenu").value("Paris"))
                .andExpect(jsonPath("$.explication").value("Bonne réponse ! Paris est correct."))
                .andExpect(jsonPath("$.reponsesCorrectes.length()").value(1));
    }

    @Test
    void corrigerReponse_returns200_whenReponseIncorrecte() throws Exception {
        request.setReponseId(2L);
        when(quizCorrectionService.corrigerReponse(any(ReponseEtudiantRequest.class)))
                .thenReturn(correctionIncorrecte);

        mockMvc.perform(post("/api/quiz-correction/question")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.correct").value(false))
                .andExpect(jsonPath("$.reponseChoisieContenu").value("Lyon"))
                .andExpect(jsonPath("$.explication").value("Mauvaise réponse. La bonne réponse était : Paris"));
    }

    @Test
    void corrigerReponse_returns500_whenQuestionNotFound() throws Exception {
        when(quizCorrectionService.corrigerReponse(any(ReponseEtudiantRequest.class)))
                .thenThrow(new RuntimeException("Question non trouvée avec l'id: 99"));

        request.setQuestionId(99L);

        mockMvc.perform(post("/api/quiz-correction/question")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().is5xxServerError());
    }

    @Test
    void corrigerReponse_returns500_whenReponseNAppartientPasALaQuestion() throws Exception {
        when(quizCorrectionService.corrigerReponse(any(ReponseEtudiantRequest.class)))
                .thenThrow(new RuntimeException("La réponse choisie n'appartient pas à cette question"));

        mockMvc.perform(post("/api/quiz-correction/question")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().is5xxServerError());
    }

    // ─── POST /api/quiz-correction/quiz ───────────────────────────────────────

    @Test
    void corrigerQuiz_returns200_withAllCorrections() throws Exception {
        List<CorrectionResponse> corrections = List.of(correctionCorrecte, correctionIncorrecte);
        when(quizCorrectionService.corrigerToutesLesReponses(anyList())).thenReturn(corrections);

        List<ReponseEtudiantRequest> requests = List.of(request);

        mockMvc.perform(post("/api/quiz-correction/quiz")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(requests)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(2))
                .andExpect(jsonPath("$[0].correct").value(true))
                .andExpect(jsonPath("$[1].correct").value(false));
    }

    @Test
    void corrigerQuiz_returns200_withEmptyList() throws Exception {
        when(quizCorrectionService.corrigerToutesLesReponses(anyList())).thenReturn(List.of());

        mockMvc.perform(post("/api/quiz-correction/quiz")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("[]"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── POST /api/quiz-correction/score ─────────────────────────────────────

    @Test
    void calculerScore_returns200_withScoreAndCorrections() throws Exception {
        when(quizCorrectionService.calculerScore(anyList())).thenReturn(100.0);
        when(quizCorrectionService.corrigerToutesLesReponses(anyList()))
                .thenReturn(List.of(correctionCorrecte));

        List<ReponseEtudiantRequest> requests = List.of(request);

        mockMvc.perform(post("/api/quiz-correction/score")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(requests)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.score").value(100.0))
                .andExpect(jsonPath("$.bonnesReponses").value(1))
                .andExpect(jsonPath("$.totalQuestions").value(1))
                .andExpect(jsonPath("$.corrections.length()").value(1));
    }

    @Test
    void calculerScore_returns200_withZeroScore_whenAucuneBonneReponse() throws Exception {
        when(quizCorrectionService.calculerScore(anyList())).thenReturn(0.0);
        when(quizCorrectionService.corrigerToutesLesReponses(anyList()))
                .thenReturn(List.of(correctionIncorrecte));

        List<ReponseEtudiantRequest> requests = List.of(request);

        mockMvc.perform(post("/api/quiz-correction/score")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(requests)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.score").value(0.0))
                .andExpect(jsonPath("$.bonnesReponses").value(0))
                .andExpect(jsonPath("$.totalQuestions").value(1));
    }
}