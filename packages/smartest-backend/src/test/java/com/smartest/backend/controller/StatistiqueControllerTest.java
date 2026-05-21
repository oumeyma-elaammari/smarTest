package com.smartest.backend.controller;

import com.smartest.backend.dto.response.StatistiqueQuestionResponse;
import com.smartest.backend.dto.response.StatistiquesQuizResponse;
import com.smartest.backend.exception.GlobalExceptionHandler;
import com.smartest.backend.exception.UnauthorizedAccessException;
import com.smartest.backend.service.StatistiqueService;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
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
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@ExtendWith(MockitoExtension.class)
@DisplayName("StatistiqueController — Tests MockMvc")
class StatistiqueControllerTest {

    private static final String EMAIL_PROF = "prof@test.fr";

    private MockMvc mockMvc;

    @Mock
    private StatistiqueService statistiqueService;

    @InjectMocks
    private StatistiqueController statistiqueController;

    private UserDetails profPrincipal;

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders.standaloneSetup(statistiqueController)
                .setCustomArgumentResolvers(authenticationPrincipalUserDetailsResolver())
                .setControllerAdvice(new GlobalExceptionHandler())
                .build();

        profPrincipal = new User(EMAIL_PROF, "pw", List.of(new SimpleGrantedAuthority("ROLE_PROFESSEUR")));
    }

    @AfterEach
    void tearDown() {
        SecurityContextHolder.clearContext();
    }

    @Test
    @DisplayName("GET /api/statistiques/quiz/{id} propriétaire → 200")
    void getStatistiquesQuizProprietaireOk() throws Exception {
        StatistiquesQuizResponse body = StatistiquesQuizResponse.builder()
                .quizId(1L)
                .quizTitre("Mon quiz")
                .nombreParticipants(3)
                .build();
        when(statistiqueService.obtenirStatistiquesQuizPourProfesseur(eq(1L), eq(EMAIL_PROF))).thenReturn(body);

        mockMvc.perform(get("/api/statistiques/quiz/1").with(principal(profPrincipal)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.quizId").value(1))
                .andExpect(jsonPath("$.quizTitre").value("Mon quiz"));
    }

    @Test
    @DisplayName("GET /api/statistiques/quiz/{id} autre professeur → 403 JSON")
    void getStatistiquesQuizNonProprietaire403() throws Exception {
        when(statistiqueService.obtenirStatistiquesQuizPourProfesseur(eq(1L), eq(EMAIL_PROF)))
                .thenThrow(new UnauthorizedAccessException("Ce quiz n'appartient pas à votre compte"));

        mockMvc.perform(get("/api/statistiques/quiz/1").with(principal(profPrincipal)))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.error").value("FORBIDDEN"))
                .andExpect(jsonPath("$.message").value("Ce quiz n'appartient pas à votre compte"));
    }

    @Test
    @DisplayName("GET question stats → 200")
    void getStatistiqueQuestionOk() throws Exception {
        StatistiqueQuestionResponse stat = StatistiqueQuestionResponse.builder()
                .questionId(5L)
                .pourcentageReussite(80.0)
                .build();
        when(statistiqueService.obtenirStatistiqueQuestionPourProfesseur(1L, 5L, EMAIL_PROF)).thenReturn(stat);

        mockMvc.perform(get("/api/statistiques/quiz/1/question/5").with(principal(profPrincipal)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.questionId").value(5));
    }

    @Test
    @DisplayName("GET /api/statistiques/examen/{id} propriétaire → 200")
    void getStatistiquesExamenProprietaireOk() throws Exception {
        StatistiquesQuizResponse body = StatistiquesQuizResponse.builder()
                .quizId(42L)
                .quizTitre("Examen final")
                .nombreParticipants(5)
                .build();
        when(statistiqueService.obtenirStatistiquesExamenPourProfesseur(eq(42L), eq(EMAIL_PROF))).thenReturn(body);

        mockMvc.perform(get("/api/statistiques/examen/42").with(principal(profPrincipal)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.quizId").value(42))
                .andExpect(jsonPath("$.quizTitre").value("Examen final"));
    }

    @Test
    @DisplayName("GET alertes → 200 avec liste")
    void getAlertesOk() throws Exception {
        when(statistiqueService.obtenirQuestionsAlertePourProfesseur(1L, EMAIL_PROF))
                .thenReturn(List.of(
                        StatistiqueQuestionResponse.builder().questionId(9L).alerteEchec(true).build()
                ));

        mockMvc.perform(get("/api/statistiques/quiz/1/alertes").with(principal(profPrincipal))
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].questionId").value(9));
    }
}
