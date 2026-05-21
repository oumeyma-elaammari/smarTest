package com.smartest.backend.integration;

import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;

import java.time.LocalDateTime;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

class ExamenPublieControllerIntegrationTest extends BaseIntegrationTest {

    @Test
    void postPublier_thenGetTous_returnsCreatedAndList() throws Exception {
        // GIVEN
        var prof = saveProfesseurVerified("examen-int@test.local", "Ensa2025@", "Examen Int");
        String auth = bearerProf(prof);
        LocalDateTime d0 = LocalDateTime.now().minusDays(1);
        LocalDateTime d1 = LocalDateTime.now().plusDays(7);

        // WHEN
        mockMvc.perform(post("/api/examens-publies")
                        .header("Authorization", auth)
                        .param("professeurId", String.valueOf(prof.getId()))
                        .param("titre", "Examen intégration")
                        .param("duree", "90")
                        .param("description", "Description test")
                        .param("dateDebut", d0.toString())
                        .param("dateFin", d1.toString())
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").isNumber())
                .andExpect(jsonPath("$.titre").value("Examen intégration"));

        // THEN — liste (entités racine sans collections lazy chargées)
        mockMvc.perform(get("/api/examens-publies")
                        .header("Authorization", auth))
                .andExpect(status().isOk());
    }
}
