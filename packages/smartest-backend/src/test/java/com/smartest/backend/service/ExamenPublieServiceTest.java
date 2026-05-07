package com.smartest.backend.service;

import com.smartest.backend.exception.InvalidSessionStateException;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.enumeration.StatutExamen;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
@DisplayName("ExamenPublieService — Tests unitaires")
class ExamenPublieServiceTest {

    @Mock
    private ExamenPublieRepository examenPublieRepository;
    @Mock
    private ProfesseurRepository professeurRepository;

    @InjectMocks
    private ExamenPublieService examenPublieService;

    private Professeur professeur;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setId(1L);
    }

    @Test
    @DisplayName("publier : données valides → statut PLANIFIE")
    void publierValidePlanifie() {
        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(examenPublieRepository.save(any(ExamenPublie.class))).thenAnswer(inv -> {
            ExamenPublie e = inv.getArgument(0);
            e.setId(100L);
            return e;
        });

        LocalDateTime debut = LocalDateTime.now().plusDays(1);
        LocalDateTime fin = debut.plusHours(2);

        ExamenPublie saved = examenPublieService.publier(1L, "Examen Java", 120, "Desc", debut, fin);

        assertThat(saved.getStatut()).isEqualTo(StatutExamen.PLANIFIE);
        assertThat(saved.getTitre()).isEqualTo("Examen Java");
    }

    @Test
    @DisplayName("publier : professeur inconnu → IllegalArgumentException")
    void publierProfesseurInconnu() {
        when(professeurRepository.findById(99L)).thenReturn(Optional.empty());

        LocalDateTime debut = LocalDateTime.now();
        assertThatThrownBy(() ->
                examenPublieService.publier(99L, "X", 60, "d", debut, debut.plusHours(1)))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("Professeur");
    }

    @Test
    @DisplayName("demarrer : examen PLANIFIE → EN_COURS")
    void demarrerPlanifieOk() {
        ExamenPublie exam = new ExamenPublie();
        exam.setId(10L);
        exam.setStatut(StatutExamen.PLANIFIE);

        when(examenPublieRepository.findById(10L)).thenReturn(Optional.of(exam));
        when(examenPublieRepository.save(any(ExamenPublie.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenPublie apres = examenPublieService.demarrer(10L);

        assertThat(apres.getStatut()).isEqualTo(StatutExamen.EN_COURS);
    }

    @Test
    @DisplayName("demarrer : mauvais statut → InvalidSessionStateException")
    void demarrerDejaEnCoursRefuse() {
        ExamenPublie exam = new ExamenPublie();
        exam.setId(10L);
        exam.setStatut(StatutExamen.EN_COURS);

        when(examenPublieRepository.findById(10L)).thenReturn(Optional.of(exam));

        assertThatThrownBy(() -> examenPublieService.demarrer(10L))
                .isInstanceOf(InvalidSessionStateException.class);
    }

    @Test
    @DisplayName("terminer : EN_COURS → TERMINE")
    void terminerEnCoursTermine() {
        ExamenPublie exam = new ExamenPublie();
        exam.setId(10L);
        exam.setStatut(StatutExamen.EN_COURS);

        when(examenPublieRepository.findById(10L)).thenReturn(Optional.of(exam));
        when(examenPublieRepository.save(any(ExamenPublie.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenPublie apres = examenPublieService.terminer(10L);

        assertThat(apres.getStatut()).isEqualTo(StatutExamen.TERMINE);
    }

    @Test
    @DisplayName("getDisponibles filtre statut EN_COURS et fenêtre temporelle")
    void getDisponiblesDelegueAuRepository() {
        ExamenPublie ex = new ExamenPublie();
        when(examenPublieRepository.findByStatutAndDateDebutBeforeAndDateFinAfter(
                any(StatutExamen.class), any(LocalDateTime.class), any(LocalDateTime.class)))
                .thenReturn(List.of(ex));

        List<ExamenPublie> list = examenPublieService.getDisponibles();

        assertThat(list).hasSize(1);
    }
}
