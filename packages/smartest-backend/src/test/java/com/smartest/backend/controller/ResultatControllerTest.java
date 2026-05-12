package com.smartest.backend.controller;

import com.smartest.backend.entity.Resultat;
import com.smartest.backend.service.ResultatService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import java.util.List;

import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@ExtendWith(MockitoExtension.class)
@DisplayName("ResultatController — Tests MockMvc")
class ResultatControllerTest {

    private MockMvc mockMvc;

    @Mock
    private ResultatService resultatService;

    @InjectMocks
    private ResultatController resultatController;

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders.standaloneSetup(resultatController).build();
    }

    @Test
    @DisplayName("GET /api/resultats/etudiant/{id} → 200 liste")
    void getByEtudiantOk() throws Exception {
        Resultat r = new Resultat();
        r.setId(1L);
        when(resultatService.getByEtudiant(3L)).thenReturn(List.of(r));

        mockMvc.perform(get("/api/resultats/etudiant/3"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].id").value(1));
    }

    @Test
    @DisplayName("GET /api/resultats/score/quiz/{id} → 200 score")
    void getScoreQuizOk() throws Exception {
        when(resultatService.calculerScoreQuiz(8L)).thenReturn(72.5);

        mockMvc.perform(get("/api/resultats/score/quiz/8"))
                .andExpect(status().isOk())
                .andExpect(content().string("72.5"));
    }

    @Test
    @DisplayName("DELETE /api/resultats/{id} → 204")
    void deleteNoContent() throws Exception {
        mockMvc.perform(delete("/api/resultats/99"))
                .andExpect(status().isNoContent());
        verify(resultatService).delete(99L);
    }

    @Test
    @DisplayName("GET /api/resultats/etudiant/{id}/quiz → filtré quiz")
    void getResultatsQuizOk() throws Exception {
        Resultat r = new Resultat();
        r.setId(2L);
        when(resultatService.getMesResultatsQuiz(4L)).thenReturn(List.of(r));

        mockMvc.perform(get("/api/resultats/etudiant/4/quiz"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1));
    }
}
