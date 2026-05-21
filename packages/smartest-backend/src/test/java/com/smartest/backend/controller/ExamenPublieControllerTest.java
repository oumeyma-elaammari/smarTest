package com.smartest.backend.controller;

import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.service.ExamenCorrectionService;
import com.smartest.backend.service.ExamenPublieService;
import com.smartest.backend.service.ExamenSupervisionService;
import com.smartest.backend.service.GroqApiKeyRegistry;
import com.smartest.backend.service.GroqRedactionRepriseService;
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
import java.util.Map;

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
    private ExamenPublieService examenPublieService;

    @Mock
    private ExamenSupervisionService supervisionService;

    @Mock
    private ExamenCorrectionService examenCorrectionService;

    @Mock
    private ExamenPublieRepository examenPublieRepository;

    @Mock
    private ProfesseurRepository professeurRepository;

    @Mock
    private EtudiantRepository etudiantRepository;

    @Mock
    private GroqApiKeyRegistry groqApiKeyRegistry;

    @Mock
    private GroqRedactionRepriseService groqRedactionRepriseService;

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
        when(examenPublieService.findAll()).thenReturn(List.of(ex));

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

        when(examenPublieService.publier(eq(1L), eq("Examen"), eq(90), eq("Desc"),
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
        when(examenPublieService.demarrer(3L)).thenReturn(ex);
        when(supervisionService.snapshot(3L)).thenReturn(
                new ExamenSupervisionService.SnapshotResponse(
                        3L, "Java", "EN_COURS", false, 0, 1,
                        Map.of(), List.of(),
                        60, 3600, 0, 20.0, 0, 0, 0, 0, "MANUAL", 120, 0));

        mockMvc.perform(patch("/api/examens-publies/3/demarrer"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.etat").value("EN_COURS"))
                .andExpect(jsonPath("$.examenId").value(3));
    }

    @Test
    @DisplayName("PATCH terminer → 200")
    void patchTerminerOk() throws Exception {
        mockMvc.perform(patch("/api/examens-publies/3/terminer"))
                .andExpect(status().isOk());
    }
}
