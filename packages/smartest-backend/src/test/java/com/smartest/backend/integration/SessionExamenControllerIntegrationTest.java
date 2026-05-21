package com.smartest.backend.integration;

import com.smartest.backend.dto.request.SessionExamenRequest;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;

import java.time.LocalDateTime;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

class SessionExamenControllerIntegrationTest extends BaseIntegrationTest {

    @Test
    void postCreateSession_thenGetByExamenPublie_returns200() throws Exception {
        // GIVEN
        var prof = saveProfesseurVerified("session-int@test.local", "Ensa2025@", "Session Int");
        var examen = saveExamenPublie(prof);
        LocalDateTime now = LocalDateTime.now();
        SessionExamenRequest req = new SessionExamenRequest(
                now.minusHours(2),
                now.plusHours(2),
                "PLANIFIE",
                examen.getId()
        );
        String auth = bearerProf(prof);

        // WHEN
        String body = mockMvc.perform(post("/api/sessions-examen")
                        .header("Authorization", auth)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").isNumber())
                .andReturn()
                .getResponse()
                .getContentAsString();

        long sessionId = objectMapper.readTree(body).get("id").asLong();

        // THEN
        mockMvc.perform(get("/api/sessions-examen/examen-publie/" + examen.getId())
                        .header("Authorization", auth))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$[0].id").value(sessionId));
    }
}
