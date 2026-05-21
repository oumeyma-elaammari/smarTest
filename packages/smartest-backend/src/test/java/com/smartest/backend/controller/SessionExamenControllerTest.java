package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.smartest.backend.dto.request.SessionExamenRequest;
import com.smartest.backend.dto.response.SessionExamenResponse;
import com.smartest.backend.exception.GlobalExceptionHandler;
import com.smartest.backend.service.SessionExamenService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import java.time.LocalDateTime;
import java.util.List;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@ExtendWith(MockitoExtension.class)
class SessionExamenControllerTest {

    private MockMvc mockMvc;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Mock
    private SessionExamenService sessionExamenService;

    @InjectMocks
    private SessionExamenController sessionExamenController;

    private SessionExamenResponse sample;

    @BeforeEach
    void setUp() {
        objectMapper.registerModule(new JavaTimeModule());
        mockMvc = MockMvcBuilders.standaloneSetup(sessionExamenController)
                .setControllerAdvice(new GlobalExceptionHandler())
                .build();
        sample = new SessionExamenResponse();
        sample.setId(1L);
        sample.setStatut("PLANIFIE");
        sample.setExamenId(10L);
        sample.setExamenTitre("Examen");
        sample.setDureeExamen(60);
    }

    @Test
    void getAllSessions_returns200() throws Exception {
        // GIVEN
        when(sessionExamenService.getAllSessions()).thenReturn(List.of(sample));

        // WHEN / THEN
        mockMvc.perform(get("/api/sessions-examen"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$[0].id").value(1));
    }

    @Test
    void getSessionsByExamenPublie_returns200() throws Exception {
        // GIVEN
        when(sessionExamenService.getSessionsByExamenPublie(10L)).thenReturn(List.of(sample));

        // WHEN / THEN
        mockMvc.perform(get("/api/sessions-examen/examen-publie/10"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$[0].examenId").value(10));
    }

    @Test
    void createSession_returns200() throws Exception {
        // GIVEN
        LocalDateTime d0 = LocalDateTime.now();
        LocalDateTime d1 = d0.plusHours(2);
        SessionExamenRequest req = new SessionExamenRequest(d0, d1, "PLANIFIE", 10L);
        when(sessionExamenService.createSession(any(SessionExamenRequest.class))).thenReturn(sample);

        // WHEN / THEN
        mockMvc.perform(post("/api/sessions-examen")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1));
        verify(sessionExamenService).createSession(any(SessionExamenRequest.class));
    }

    @Test
    void patchDemarrer_returns200() throws Exception {
        // GIVEN
        when(sessionExamenService.demarrerSession(1L)).thenReturn(sample);

        // WHEN / THEN
        mockMvc.perform(patch("/api/sessions-examen/1/demarrer"))
                .andExpect(status().isOk());
    }

    @Test
    void deleteSession_returns200() throws Exception {
        // GIVEN
        doNothing().when(sessionExamenService).deleteSession(1L);

        // WHEN / THEN
        mockMvc.perform(delete("/api/sessions-examen/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.success").value(true));
    }
}
