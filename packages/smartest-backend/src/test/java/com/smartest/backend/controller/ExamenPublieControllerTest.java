package com.smartest.backend.controller;

import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.service.ExamenPublieService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
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

import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@ExtendWith(MockitoExtension.class)
@DisplayName("ExamenPublieController — Tests MockMvc")
class ExamenPublieControllerTest {

    private MockMvc mockMvc;

    @Mock
    private ExamenPublieService service;

    @InjectMocks
    private ExamenPublieController examenPublieController;

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders.standaloneSetup(examenPublieController).build();
    }

    @Test
    @DisplayName("GET /api/examens-publies → 200 liste")
    void getTousOk() throws Exception {
        ExamenPublie ex = new ExamenPublie();
        ex.setId(1L);
        ex.setTitre("Java");
        when(service.findAll()).thenReturn(List.of(ex));

        mockMvc.perform(get("/api/examens-publies"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].titre").value("Java"));
    }

    @Test
    @DisplayName("POST /api/examens-publies données valides → 201")
    void postPublierCreated() throws Exception {
        ExamenPublie saved = new ExamenPublie();
        saved.setId(5L);
        saved.setStatut(StatutExamen.PLANIFIE);
        Professeur p = new Professeur();
        p.setId(1L);
        saved.setProfesseur(p);

        LocalDateTime debut = LocalDateTime.of(2026, 6, 1, 10, 0);
        LocalDateTime fin = debut.plusHours(2);

        when(service.publier(eq(1L), eq("Examen"), eq(90), eq("Desc"),
                eq(debut), eq(fin))).thenReturn(saved);

        mockMvc.perform(post("/api/examens-publies")
                        .contentType(MediaType.APPLICATION_FORM_URLENCODED)
                        .param("professeurId", "1")
                        .param("titre", "Examen")
                        .param("duree", "90")
                        .param("description", "Desc")
                        .param("dateDebut", debut.toString())
                        .param("dateFin", fin.toString()))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").value(5));
    }

    @Test
    @DisplayName("PATCH demarrer → 200")
    void patchDemarrerOk() throws Exception {
        ExamenPublie ex = new ExamenPublie();
        ex.setId(3L);
        ex.setStatut(StatutExamen.EN_COURS);
        when(service.demarrer(3L)).thenReturn(ex);

        mockMvc.perform(patch("/api/examens-publies/3/demarrer"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.statut").value("EN_COURS"));
    }

    @Test
    @DisplayName("PATCH terminer → 200")
    void patchTerminerOk() throws Exception {
        ExamenPublie ex = new ExamenPublie();
        ex.setId(3L);
        ex.setStatut(StatutExamen.TERMINE);
        when(service.terminer(3L)).thenReturn(ex);

        mockMvc.perform(patch("/api/examens-publies/3/terminer"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.statut").value("TERMINE"));
    }
}
