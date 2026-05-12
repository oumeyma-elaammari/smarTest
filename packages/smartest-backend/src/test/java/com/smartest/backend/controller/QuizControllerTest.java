package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.dto.request.QuizRequest;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.entity.enumeration.StatutQuiz;
import com.smartest.backend.exception.GlobalExceptionHandler;
import com.smartest.backend.exception.QuizNotFoundException;
import com.smartest.backend.service.QuizService;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import java.util.List;

import static com.smartest.backend.testsupport.MockMvcAuthenticationSupport.authenticationPrincipalUserDetailsResolver;
import static com.smartest.backend.testsupport.MockMvcAuthenticationSupport.principal;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

/**
 * Tests {@link QuizController} (CRUD de base). Le PUT du contrôleur délègue à
 * {@link QuizService#createQuiz(QuizRequest)} (même comportement actuel que l’API).
 */
@ExtendWith(MockitoExtension.class)
class QuizControllerTest {

    private static final String TITRE_QUIZ_GEO = "Quiz de géographie";
    private static final String API_QUIZS = "/api/quizs";
    private static final String API_QUIZ_1 = "/api/quizs/1";
    private static final String API_QUIZ_99 = "/api/quizs/99";
    private static final String JSON_TITRE = "$.titre";
    private static final String TITRE_MODIFIE = "Quiz modifié";
    private static final String PROF_TEST_EMAIL = "prof@test.com";
    private static final String JSON_ERROR = "$.error";
    private static final String ERROR_QUIZ_NOT_FOUND = "QUIZ_NOT_FOUND";

    private MockMvc mockMvc;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Mock
    private QuizService quizService;

    @InjectMocks
    private QuizController quizController;

    private QuizResponse quizResponse;
    private QuizRequest quizRequest;

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders.standaloneSetup(quizController)
                .setCustomArgumentResolvers(authenticationPrincipalUserDetailsResolver())
                .setControllerAdvice(new GlobalExceptionHandler())
                .build();

        quizResponse = new QuizResponse();
        quizResponse.setId(1L);
        quizResponse.setTitre(TITRE_QUIZ_GEO);
        quizResponse.setProfesseurId(1L);
        quizResponse.setProfesseurNom("Dupont");
        quizResponse.setStatut(StatutQuiz.BROUILLON);

        quizRequest = new QuizRequest();
        quizRequest.setTitre(TITRE_QUIZ_GEO);
        quizRequest.setProfesseurId(1L);
    }

    @AfterEach
    void tearDown() {
        SecurityContextHolder.clearContext();
    }

    @Test
    void getAllQuizsReturns200WithListOfQuizzes() throws Exception {
        when(quizService.getAllQuizs()).thenReturn(List.of(quizResponse));

        mockMvc.perform(get(API_QUIZS))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].id").value(1))
                .andExpect(jsonPath("$[0].titre").value(TITRE_QUIZ_GEO))
                .andExpect(jsonPath("$[0].professeurNom").value("Dupont"));
    }

    @Test
    void getAllQuizsReturns200WithEmptyList() throws Exception {
        when(quizService.getAllQuizs()).thenReturn(List.of());

        mockMvc.perform(get(API_QUIZS))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    @Test
    void getQuizByIdReturns200WhenFound() throws Exception {
        when(quizService.getQuizById(1L)).thenReturn(quizResponse);

        mockMvc.perform(get(API_QUIZ_1))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath(JSON_TITRE).value(TITRE_QUIZ_GEO));
    }

    @Test
    void getQuizByIdReturns404WhenNotFound() throws Exception {
        when(quizService.getQuizById(99L))
                .thenThrow(new QuizNotFoundException("Quiz non trouvé"));

        mockMvc.perform(get(API_QUIZ_99))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath(JSON_ERROR).value(ERROR_QUIZ_NOT_FOUND));
    }

    @Test
    void createQuizReturns201WithCreatedQuiz() throws Exception {
        when(quizService.createQuiz(any(QuizRequest.class))).thenReturn(quizResponse);

        mockMvc.perform(post(API_QUIZS)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(quizRequest)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath(JSON_TITRE).value(TITRE_QUIZ_GEO))
                .andExpect(jsonPath("$.professeurId").value(1));
    }

    @Test
    void createQuizReturns404WhenProfesseurNotFound() throws Exception {
        when(quizService.createQuiz(any(QuizRequest.class)))
                .thenThrow(new QuizNotFoundException("Professeur non trouvé"));

        quizRequest.setProfesseurId(99L);

        mockMvc.perform(post(API_QUIZS)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(quizRequest)))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath(JSON_ERROR).value(ERROR_QUIZ_NOT_FOUND));
    }

    @Test
    void updateQuizCallsCreateQuizAndReturns200() throws Exception {
        QuizResponse updated = QuizResponse.builder()
                .id(1L)
                .titre(TITRE_MODIFIE)
                .professeurId(1L)
                .build();

        when(quizService.createQuiz(any(QuizRequest.class))).thenReturn(updated);

        quizRequest.setTitre(TITRE_MODIFIE);

        mockMvc.perform(put(API_QUIZ_1)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(quizRequest)))
                .andExpect(status().isOk())
                .andExpect(jsonPath(JSON_TITRE).value(TITRE_MODIFIE));
    }

    @Test
    void updateQuizReturns404WhenProfesseurNotFound() throws Exception {
        when(quizService.createQuiz(any(QuizRequest.class)))
                .thenThrow(new QuizNotFoundException("Professeur non trouvé"));

        mockMvc.perform(put(API_QUIZ_99)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(quizRequest)))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath(JSON_ERROR).value(ERROR_QUIZ_NOT_FOUND));
    }

    @Test
    void deleteQuizReturns200WithSuccessMessage() throws Exception {
        UserDetails user = new User(PROF_TEST_EMAIL, "pw", List.of(new SimpleGrantedAuthority("ROLE_PROFESSEUR")));
        doNothing().when(quizService).deleteQuiz(eq(1L), eq(PROF_TEST_EMAIL));

        mockMvc.perform(delete(API_QUIZ_1).with(principal(user)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.message").value("Quiz supprimé avec succès"))
                .andExpect(jsonPath("$.success").value(true));
    }

    @Test
    void deleteQuizReturns404WhenQuizMissing() throws Exception {
        UserDetails user = new User(PROF_TEST_EMAIL, "pw", List.of(new SimpleGrantedAuthority("ROLE_PROFESSEUR")));
        doThrow(new QuizNotFoundException("Quiz introuvable"))
                .when(quizService).deleteQuiz(eq(99L), eq(PROF_TEST_EMAIL));

        mockMvc.perform(delete(API_QUIZ_99).with(principal(user)))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath(JSON_ERROR).value(ERROR_QUIZ_NOT_FOUND));
    }
}
