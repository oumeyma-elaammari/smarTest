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
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.web.server.ResponseStatusException;
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

    private static final String CONTENU_PARIS = "Paris";
    private static final String API_CORRECTION_QUESTION = "/api/quiz-correction/question";
    private static final String API_CORRECTION_QUIZ = "/api/quiz-correction/quiz";
    private static final String API_CORRECTION_SCORE = "/api/quiz-correction/score";

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
        @ExceptionHandler(ResponseStatusException.class)
        public ResponseEntity<String> handleResponseStatus(ResponseStatusException ex) {
            HttpStatus status = HttpStatus.resolve(ex.getStatusCode().value());
            return ResponseEntity.status(status != null ? status : HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ex.getReason());
        }

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
        reponseCorrecte.setContenu(CONTENU_PARIS);
        reponseCorrecte.setCorrecte(true);

        correctionCorrecte = new CorrectionResponse(
                10L,
                "Quelle est la capitale de la France ?",
                1L,
                CONTENU_PARIS,
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
                "Mauvaise réponse. La bonne réponse était : " + CONTENU_PARIS
        );

        request = new ReponseEtudiantRequest();
        request.setQuestionId(10L);
        request.setReponseId(1L);
        request.setEtudiantId(5L);
    }

    // ─── POST /api/quiz-correction/question ───────────────────────────────────

    @Test
    void corrigerReponseReturns200WhenReponseCorrecte() throws Exception {
        when(quizCorrectionService.corrigerReponse(any(ReponseEtudiantRequest.class)))
                .thenReturn(correctionCorrecte);

        mockMvc.perform(post(API_CORRECTION_QUESTION)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.correct").value(true))
                .andExpect(jsonPath("$.questionId").value(10))
                .andExpect(jsonPath("$.reponseChoisieId").value(1))
                .andExpect(jsonPath("$.reponseChoisieContenu").value(CONTENU_PARIS))
                .andExpect(jsonPath("$.explication").value("Bonne réponse ! Paris est correct."))
                .andExpect(jsonPath("$.reponsesCorrectes.length()").value(1));
    }

    @Test
    void corrigerReponseReturns200WhenReponseIncorrecte() throws Exception {
        request.setReponseId(2L);
        when(quizCorrectionService.corrigerReponse(any(ReponseEtudiantRequest.class)))
                .thenReturn(correctionIncorrecte);

        mockMvc.perform(post(API_CORRECTION_QUESTION)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.correct").value(false))
                .andExpect(jsonPath("$.reponseChoisieContenu").value("Lyon"))
                .andExpect(jsonPath("$.explication").value("Mauvaise réponse. La bonne réponse était : Paris"));
    }

    @Test
    void corrigerReponseReturns404WhenQuestionNotFound() throws Exception {
        when(quizCorrectionService.corrigerReponse(any(ReponseEtudiantRequest.class)))
                .thenThrow(new ResponseStatusException(HttpStatus.NOT_FOUND, "Question non trouvée avec l'id: 99"));

        request.setQuestionId(99L);

        mockMvc.perform(post(API_CORRECTION_QUESTION)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isNotFound());
    }

    @Test
    void corrigerReponseReturns400WhenReponseNAppartientPasALaQuestion() throws Exception {
        when(quizCorrectionService.corrigerReponse(any(ReponseEtudiantRequest.class)))
                .thenThrow(new ResponseStatusException(HttpStatus.BAD_REQUEST,
                        "La réponse choisie n'appartient pas à cette question"));

        mockMvc.perform(post(API_CORRECTION_QUESTION)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest());
    }

    // ─── POST /api/quiz-correction/quiz ───────────────────────────────────────

    @Test
    void corrigerQuizReturns200WithAllCorrections() throws Exception {
        List<CorrectionResponse> corrections = List.of(correctionCorrecte, correctionIncorrecte);
        when(quizCorrectionService.corrigerToutesLesReponses(anyList())).thenReturn(corrections);

        List<ReponseEtudiantRequest> requests = List.of(request);

        mockMvc.perform(post(API_CORRECTION_QUIZ)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(requests)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(2))
                .andExpect(jsonPath("$[0].correct").value(true))
                .andExpect(jsonPath("$[1].correct").value(false));
    }

    @Test
    void corrigerQuizReturns200WithEmptyList() throws Exception {
        when(quizCorrectionService.corrigerToutesLesReponses(anyList())).thenReturn(List.of());

        mockMvc.perform(post(API_CORRECTION_QUIZ)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("[]"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── POST /api/quiz-correction/score ─────────────────────────────────────

    @Test
    void calculerScoreReturns200WithScoreAndCorrections() throws Exception {
        when(quizCorrectionService.calculerScore(anyList())).thenReturn(100.0);
        when(quizCorrectionService.corrigerToutesLesReponses(anyList()))
                .thenReturn(List.of(correctionCorrecte));

        List<ReponseEtudiantRequest> requests = List.of(request);

        mockMvc.perform(post(API_CORRECTION_SCORE)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(requests)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.score").value(100.0))
                .andExpect(jsonPath("$.bonnesReponses").value(1))
                .andExpect(jsonPath("$.totalQuestions").value(1))
                .andExpect(jsonPath("$.corrections.length()").value(1));
    }

    @Test
    void calculerScoreReturns200WithZeroScoreWhenAucuneBonneReponse() throws Exception {
        when(quizCorrectionService.calculerScore(anyList())).thenReturn(0.0);
        when(quizCorrectionService.corrigerToutesLesReponses(anyList()))
                .thenReturn(List.of(correctionIncorrecte));

        List<ReponseEtudiantRequest> requests = List.of(request);

        mockMvc.perform(post(API_CORRECTION_SCORE)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(requests)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.score").value(0.0))
                .andExpect(jsonPath("$.bonnesReponses").value(0))
                .andExpect(jsonPath("$.totalQuestions").value(1));
    }
}