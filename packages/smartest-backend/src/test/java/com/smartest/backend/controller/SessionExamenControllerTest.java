/*package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.smartest.backend.dto.request.SessionExamenRequest;
import com.smartest.backend.dto.response.SessionExamenResponse;
import com.smartest.backend.service.SessionExamenService;
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

import java.time.LocalDateTime;
import java.util.List;

import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@ExtendWith(MockitoExtension.class)
class SessionExamenControllerTest {

    private MockMvc mockMvc;
    private ObjectMapper objectMapper;

    @Mock
    private SessionExamenService sessionExamenService;

    @InjectMocks
    private SessionExamenController sessionExamenController;

    private SessionExamenResponse sessionResponse;
    private SessionExamenRequest sessionRequest;

    private final LocalDateTime dateDebut = LocalDateTime.now().plusHours(1);
    private final LocalDateTime dateFin   = LocalDateTime.now().plusHours(3);

    @RestControllerAdvice
    static class TestExceptionHandler {
        @ExceptionHandler(RuntimeException.class)
        public ResponseEntity<String> handleRuntime(RuntimeException ex) {
            return ResponseEntity.status(500).body(ex.getMessage());
        }
    }

    @BeforeEach
    void setUp() {
        objectMapper = new ObjectMapper();
        objectMapper.registerModule(new JavaTimeModule());

        mockMvc = MockMvcBuilders
                .standaloneSetup(sessionExamenController)
                .setControllerAdvice(new TestExceptionHandler())
                .build();

        sessionResponse = new SessionExamenResponse();
        sessionResponse.setId(1L);
        sessionResponse.setDateDebut(dateDebut);
        sessionResponse.setDateFin(dateFin);
        sessionResponse.setStatut("PLANIFIE");
        sessionResponse.setExamenId(1L);
        sessionResponse.setExamenTitre("Examen de Java");
        sessionResponse.setDureeExamen(120);

        sessionRequest = new SessionExamenRequest();
        sessionRequest.setExamenId(1L);
        sessionRequest.setDateDebut(dateDebut);
        sessionRequest.setDateFin(dateFin);
        sessionRequest.setStatut("PLANIFIE");
    }

    // ─── GET /api/sessions-examen ─────────────────────────────────────────────

    @Test
    void getAllSessions_returns200_withList() throws Exception {
        when(sessionExamenService.getAllSessions()).thenReturn(List.of(sessionResponse));

        mockMvc.perform(get("/api/sessions-examen"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].id").value(1))
                .andExpect(jsonPath("$[0].statut").value("PLANIFIE"))
                .andExpect(jsonPath("$[0].examenTitre").value("Examen de Java"));
    }

    @Test
    void getAllSessions_returns200_withEmptyList() throws Exception {
        when(sessionExamenService.getAllSessions()).thenReturn(List.of());

        mockMvc.perform(get("/api/sessions-examen"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── GET /api/sessions-examen/{id} ───────────────────────────────────────

    @Test
    void getSessionById_returns200_whenFound() throws Exception {
        when(sessionExamenService.getSessionById(1L)).thenReturn(sessionResponse);

        mockMvc.perform(get("/api/sessions-examen/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath("$.examenId").value(1))
                .andExpect(jsonPath("$.dureeExamen").value(120));
    }

    @Test
    void getSessionById_returns500_whenNotFound() throws Exception {
        when(sessionExamenService.getSessionById(99L))
                .thenThrow(new RuntimeException("Session non trouvée avec l'id: 99"));

        mockMvc.perform(get("/api/sessions-examen/99"))
                .andExpect(status().is5xxServerError());
    }

    // ─── GET /api/sessions-examen/examen/{examenId} ───────────────────────────

    @Test
    void getSessionsByExamen_returns200_whenExamenExists() throws Exception {
        when(sessionExamenService.getSessionsByExamen(1L)).thenReturn(List.of(sessionResponse));

        mockMvc.perform(get("/api/sessions-examen/examen/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].examenId").value(1));
    }

    @Test
    void getSessionsByExamen_returns500_whenExamenNotFound() throws Exception {
        when(sessionExamenService.getSessionsByExamen(99L))
                .thenThrow(new RuntimeException("Examen non trouvé avec l'id: 99"));

        mockMvc.perform(get("/api/sessions-examen/examen/99"))
                .andExpect(status().is5xxServerError());
    }

    // ─── GET /api/sessions-examen/statut/en-cours ────────────────────────────

    @Test
    void getSessionsEnCours_returns200_withActiveSessions() throws Exception {
        sessionResponse.setStatut("EN_COURS");
        when(sessionExamenService.getSessionsEnCours()).thenReturn(List.of(sessionResponse));

        mockMvc.perform(get("/api/sessions-examen/statut/en-cours"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].statut").value("EN_COURS"));
    }

    @Test
    void getSessionsEnCours_returns200_withEmptyList() throws Exception {
        when(sessionExamenService.getSessionsEnCours()).thenReturn(List.of());

        mockMvc.perform(get("/api/sessions-examen/statut/en-cours"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── GET /api/sessions-examen/statut/a-venir ─────────────────────────────

    @Test
    void getSessionsAFaire_returns200_withFutureSessions() throws Exception {
        when(sessionExamenService.getSessionsAFaire()).thenReturn(List.of(sessionResponse));

        mockMvc.perform(get("/api/sessions-examen/statut/a-venir"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1));
    }

    @Test
    void getSessionsAFaire_returns200_withEmptyList() throws Exception {
        when(sessionExamenService.getSessionsAFaire()).thenReturn(List.of());

        mockMvc.perform(get("/api/sessions-examen/statut/a-venir"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }

    // ─── POST /api/sessions-examen ────────────────────────────────────────────

    @Test
    void createSession_returns201_withCreatedSession() throws Exception {
        when(sessionExamenService.createSession(any(SessionExamenRequest.class)))
                .thenReturn(sessionResponse);

        mockMvc.perform(post("/api/sessions-examen")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(sessionRequest)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath("$.statut").value("PLANIFIE"))
                .andExpect(jsonPath("$.examenId").value(1));
    }

    @Test
    void createSession_returns500_whenExamenNotFound() throws Exception {
        when(sessionExamenService.createSession(any(SessionExamenRequest.class)))
                .thenThrow(new RuntimeException("Examen non trouvé avec l'id: 99"));

        sessionRequest.setExamenId(99L);

        mockMvc.perform(post("/api/sessions-examen")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(sessionRequest)))
                .andExpect(status().is5xxServerError());
    }

    @Test
    void createSession_returns500_whenDateDebutAfterDateFin() throws Exception {
        when(sessionExamenService.createSession(any(SessionExamenRequest.class)))
                .thenThrow(new RuntimeException("La date de début doit être avant la date de fin"));

        sessionRequest.setDateDebut(dateFin);
        sessionRequest.setDateFin(dateDebut);

        mockMvc.perform(post("/api/sessions-examen")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(sessionRequest)))
                .andExpect(status().is5xxServerError());
    }

    // ─── PUT /api/sessions-examen/{id} ───────────────────────────────────────

    @Test
    void updateSession_returns200_withUpdatedSession() throws Exception {
        sessionResponse.setStatut("EN_COURS");
        when(sessionExamenService.updateSession(eq(1L), any(SessionExamenRequest.class)))
                .thenReturn(sessionResponse);

        sessionRequest.setStatut("EN_COURS");

        mockMvc.perform(put("/api/sessions-examen/1")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(sessionRequest)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.statut").value("EN_COURS"));
    }

    @Test
    void updateSession_returns500_whenSessionNotFound() throws Exception {
        when(sessionExamenService.updateSession(eq(99L), any(SessionExamenRequest.class)))
                .thenThrow(new RuntimeException("Session non trouvée avec l'id: 99"));

        mockMvc.perform(put("/api/sessions-examen/99")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(sessionRequest)))
                .andExpect(status().is5xxServerError());
    }

    // ─── PATCH /api/sessions-examen/{id}/demarrer ────────────────────────────

    @Test
    void demarrerSession_returns200_withEnCoursStatut() throws Exception {
        sessionResponse.setStatut("EN_COURS");
        when(sessionExamenService.demarrerSession(1L)).thenReturn(sessionResponse);

        mockMvc.perform(patch("/api/sessions-examen/1/demarrer"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.statut").value("EN_COURS"));
    }

    @Test
    void demarrerSession_returns500_whenSessionNotStartedYet() throws Exception {
        when(sessionExamenService.demarrerSession(1L))
                .thenThrow(new RuntimeException("La session n'a pas encore commencé"));

        mockMvc.perform(patch("/api/sessions-examen/1/demarrer"))
                .andExpect(status().is5xxServerError());
    }

    // ─── PATCH /api/sessions-examen/{id}/terminer ────────────────────────────

    @Test
    void terminerSession_returns200_withTermineStatut() throws Exception {
        sessionResponse.setStatut("TERMINE");
        when(sessionExamenService.terminerSession(1L)).thenReturn(sessionResponse);

        mockMvc.perform(patch("/api/sessions-examen/1/terminer"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.statut").value("TERMINE"));
    }

    @Test
    void terminerSession_returns500_whenSessionNotFound() throws Exception {
        when(sessionExamenService.terminerSession(99L))
                .thenThrow(new RuntimeException("Session non trouvée avec l'id: 99"));

        mockMvc.perform(patch("/api/sessions-examen/99/terminer"))
                .andExpect(status().is5xxServerError());
    }

    // ─── PATCH /api/sessions-examen/{id}/annuler ─────────────────────────────

    @Test
    void annulerSession_returns200_withAnnuleStatut() throws Exception {
        sessionResponse.setStatut("ANNULE");
        when(sessionExamenService.annulerSession(1L)).thenReturn(sessionResponse);

        mockMvc.perform(patch("/api/sessions-examen/1/annuler"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.statut").value("ANNULE"));
    }

    @Test
    void annulerSession_returns500_whenSessionNotFound() throws Exception {
        when(sessionExamenService.annulerSession(99L))
                .thenThrow(new RuntimeException("Session non trouvée avec l'id: 99"));

        mockMvc.perform(patch("/api/sessions-examen/99/annuler"))
                .andExpect(status().is5xxServerError());
    }

    // ─── DELETE /api/sessions-examen/{id} ────────────────────────────────────

    @Test
    void deleteSession_returns200_withSuccessMessage() throws Exception {
        doNothing().when(sessionExamenService).deleteSession(1L);

        mockMvc.perform(delete("/api/sessions-examen/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.message").value("Session d'examen supprimée avec succès"))
                .andExpect(jsonPath("$.success").value(true));
    }

    @Test
    void deleteSession_returns500_whenSessionNotFound() throws Exception {
        doThrow(new RuntimeException("Session non trouvée avec l'id: 99"))
                .when(sessionExamenService).deleteSession(99L);

        mockMvc.perform(delete("/api/sessions-examen/99"))
                .andExpect(status().is5xxServerError());
    }

    // ─── GET /api/sessions-examen/examen/{examenId}/en-cours ─────────────────

    @Test
    void isExamenEnCours_returnsTrue_whenActiveSession() throws Exception {
        when(sessionExamenService.isExamenEnCours(1L)).thenReturn(true);

        mockMvc.perform(get("/api/sessions-examen/examen/1/en-cours"))
                .andExpect(status().isOk())
                .andExpect(content().string("true"));
    }

    @Test
    void isExamenEnCours_returnsFalse_whenNoActiveSession() throws Exception {
        when(sessionExamenService.isExamenEnCours(1L)).thenReturn(false);

        mockMvc.perform(get("/api/sessions-examen/examen/1/en-cours"))
                .andExpect(status().isOk())
                .andExpect(content().string("false"));
    }
}

 */