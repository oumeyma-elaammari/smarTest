package com.smartest.backend.integration;

import com.smartest.backend.dto.request.QuizRequest;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

class QuizControllerIntegrationTest extends BaseIntegrationTest {

    @Test
    void postCreateQuiz_thenGetById_returns200() throws Exception {
        // GIVEN
        var prof = saveProfesseurVerified("quiz-int@test.local", "Ensa2025@", "Quiz Int");
        QuizRequest req = new QuizRequest();
        req.setTitre("Quiz intégration");
        req.setProfesseurId(prof.getId());
        String auth = bearerProf(prof);

        // WHEN
        String createResponse = mockMvc.perform(post("/api/quizs")
                        .header("Authorization", auth)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").isNumber())
                .andExpect(jsonPath("$.titre").value("Quiz intégration"))
                .andReturn()
                .getResponse()
                .getContentAsString();

        long quizId = objectMapper.readTree(createResponse).get("id").asLong();

        // THEN
        mockMvc.perform(get("/api/quizs/" + quizId)
                        .header("Authorization", auth))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(quizId))
                .andExpect(jsonPath("$.titre").value("Quiz intégration"));
    }
}
