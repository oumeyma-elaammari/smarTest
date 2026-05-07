package com.smartest.backend.controller;

import com.smartest.backend.dto.response.QuizPassageWebResponse;
import com.smartest.backend.dto.response.QuizQrLiveStatsResponse;
import com.smartest.backend.exception.GlobalExceptionHandler;
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
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@ExtendWith(MockitoExtension.class)
class QuizControllerQrEndpointsTest {

    private static final String PROF_EXAMPLE_COM = "prof@example.com";

    private MockMvc mockMvc;

    @Mock
    private QuizService quizService;

    @InjectMocks
    private QuizController quizController;

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders.standaloneSetup(quizController)
                .setCustomArgumentResolvers(authenticationPrincipalUserDetailsResolver())
                .setControllerAdvice(new GlobalExceptionHandler())
                .build();
    }

    @AfterEach
    void tearDown() {
        SecurityContextHolder.clearContext();
    }

    @Test
    void getQuizPourPassageQrRetourne200EtCorps() throws Exception {
        QuizPassageWebResponse body = new QuizPassageWebResponse();
        body.setId(1L);
        body.setTitre("Quiz QR");
        body.setDuree(15);
        body.setNombreQuestions(2);
        when(quizService.getQuizPourPassageQr(1L)).thenReturn(body);

        mockMvc.perform(get("/api/quizs/1/passage-qr").accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath("$.titre").value("Quiz QR"))
                .andExpect(jsonPath("$.nombreQuestions").value(2));

        verify(quizService).getQuizPourPassageQr(1L);
    }

    @Test
    void getQrLiveStatsUtiliseEmailDuProfesseurConnecte() throws Exception {
        QuizQrLiveStatsResponse stats = QuizQrLiveStatsResponse.builder()
                .quizId(5L)
                .quizTitre("Live")
                .nombreParticipants(4)
                .build();
        when(quizService.getQrLiveStats(5L, PROF_EXAMPLE_COM)).thenReturn(stats);

        UserDetails user = new User(PROF_EXAMPLE_COM, "pw", List.of(new SimpleGrantedAuthority("ROLE_PROFESSEUR")));

        mockMvc.perform(get("/api/quizs/5/stats-qr-live")
                        .with(principal(user))
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.quizId").value(5))
                .andExpect(jsonPath("$.nombreParticipants").value(4));

        verify(quizService).getQrLiveStats(5L, PROF_EXAMPLE_COM);
    }

    @Test
    void clearQrLiveStatsAppelleLeService() throws Exception {
        UserDetails user = new User("p@example.com", "pw", List.of(new SimpleGrantedAuthority("ROLE_PROFESSEUR")));

        mockMvc.perform(delete("/api/quizs/9/stats-qr-live").with(principal(user)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.message").value("Statistiques QR live clôturées"))
                .andExpect(jsonPath("$.success").value(true));

        verify(quizService).clearQrLiveStats(9L, "p@example.com");
    }
}
