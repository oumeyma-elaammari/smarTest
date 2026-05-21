package com.smartest.backend.integration;

import com.smartest.backend.dto.request.LoginRequest;
import com.smartest.backend.dto.request.RefreshTokenRequest;
import com.smartest.backend.dto.request.RegisterRequest;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

class AuthControllerIntegrationTest extends BaseIntegrationTest {

    @Test
    void postRegister_professeur_returnsCreated() throws Exception {
        // GIVEN
        RegisterRequest req = new RegisterRequest();
        req.setNom("Jean Dupont");
        req.setEmail("prof-int@test.local");
        req.setPassword("Ensa2025@");
        req.setConfirmPassword("Ensa2025@");

        // WHEN / THEN
        mockMvc.perform(post("/auth/register")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isCreated());
    }

    @Test
    void postLogin_validCredentials_returns200AndToken() throws Exception {
        // GIVEN
        saveProfesseurVerified("login-int@test.local", "Ensa2025@", "Login Int");
        LoginRequest req = new LoginRequest();
        req.setEmail("login-int@test.local");
        req.setPassword("Ensa2025@");

        // WHEN / THEN
        mockMvc.perform(post("/auth/login")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.token").isNotEmpty())
                .andExpect(jsonPath("$.role").value("PROFESSEUR"));
    }

    @Test
    void postRefresh_validToken_returns200AndNewToken() throws Exception {
        // GIVEN
        var prof = saveProfesseurVerified("refresh-int@test.local", "Ensa2025@", "Refresh Int");
        String token = jwtUtil.generateToken(prof.getEmail(), "PROFESSEUR", prof.getId());
        RefreshTokenRequest body = new RefreshTokenRequest();
        body.setToken(token);

        // WHEN / THEN
        mockMvc.perform(post("/auth/refresh")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(body)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.token").isNotEmpty())
                .andExpect(jsonPath("$.role").value("PROFESSEUR"));
    }

    @Test
    void postRefresh_invalidToken_returns401() throws Exception {
        // GIVEN
        RefreshTokenRequest body = new RefreshTokenRequest();
        body.setToken("not-a-jwt");

        // WHEN / THEN
        mockMvc.perform(post("/auth/refresh")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(body)))
                .andExpect(status().isUnauthorized());
    }
}
