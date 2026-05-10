package com.smartest.backend.controller;

import com.smartest.backend.dto.qr.QrLiveStreamEnvelope;
import com.smartest.backend.dto.qr.QrLiveStreamMessageType;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.exception.GlobalExceptionHandler;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.service.QuizSessionManager;
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
import java.util.Optional;

import static com.smartest.backend.testsupport.MockMvcAuthenticationSupport.authenticationPrincipalUserDetailsResolver;
import static com.smartest.backend.testsupport.MockMvcAuthenticationSupport.principal;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@ExtendWith(MockitoExtension.class)
class QuizControllerQrEndpointsTest {

    private static final String PROF_MAIL = "prof@example.com";

    private MockMvc mockMvc;

    @Mock
    private QuizSessionManager quizSessionManager;

    @Mock
    private ProfesseurRepository professeurRepository;

    @InjectMocks
    private QuizQrEphemeralRestController qrEphemeralRestController;

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders.standaloneSetup(qrEphemeralRestController)
                .setCustomArgumentResolvers(authenticationPrincipalUserDetailsResolver())
                .setControllerAdvice(new GlobalExceptionHandler())
                .build();
    }

    @AfterEach
    void tearDown() {
        SecurityContextHolder.clearContext();
    }

    @Test
    void createSessionRetourneJeton() throws Exception {
        Professeur p = new Professeur();
        p.setId(7L);
        p.setEmail(PROF_MAIL);
        when(professeurRepository.findByEmail(PROF_MAIL)).thenReturn(Optional.of(p));
        when(quizSessionManager.createSession(7L, PROF_MAIL)).thenReturn("abc-token");

        UserDetails user = new User(PROF_MAIL, "pw", List.of(new SimpleGrantedAuthority("ROLE_PROFESSEUR")));

        mockMvc.perform(post("/api/qr-live/sessions")
                        .with(principal(user))
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.sessionToken").value("abc-token"));

        verify(quizSessionManager).createSession(7L, PROF_MAIL);
    }

    @Test
    void snapshotPublicRetourne404SiInconnu() throws Exception {
        when(quizSessionManager.snapshotEnvelope("x")).thenReturn(Optional.empty());

        mockMvc.perform(get("/api/qr-live/public/x/snapshot").accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isNotFound());
    }

    @Test
    void snapshotPublicRetourneEnvelope() throws Exception {
        QrLiveStreamEnvelope env = QrLiveStreamEnvelope.builder()
                .type(QrLiveStreamMessageType.STATS)
                .build();
        when(quizSessionManager.snapshotEnvelope("tok")).thenReturn(Optional.of(env));

        mockMvc.perform(get("/api/qr-live/public/tok/snapshot").accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.type").value("STATS"));
    }

    @Test
    void deleteSessionNoContentSansJwt() throws Exception {
        when(quizSessionManager.forceCloseSession("t1")).thenReturn(true);

        mockMvc.perform(delete("/api/qr-live/sessions/t1"))
                .andExpect(status().isNoContent());

        verify(quizSessionManager).forceCloseSession("t1");
    }

    @Test
    void deleteSession404SiJetonInconnu() throws Exception {
        when(quizSessionManager.forceCloseSession("missing")).thenReturn(false);

        mockMvc.perform(delete("/api/qr-live/sessions/missing"))
                .andExpect(status().isNotFound());
    }
}
