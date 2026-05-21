package com.smartest.backend.service;

import com.smartest.backend.dto.request.SessionExamenRequest;
import com.smartest.backend.dto.response.SessionExamenResponse;
import com.smartest.backend.entity.ExamenPublie;
import com.smartest.backend.entity.Resultat;
import com.smartest.backend.entity.SessionExamen;
import com.smartest.backend.exception.InvalidSessionStateException;
import com.smartest.backend.exception.SessionNotFoundException;
import com.smartest.backend.repository.ExamenPublieRepository;
import com.smartest.backend.repository.ResultatRepository;
import com.smartest.backend.repository.SessionExamenRepository;
import org.junit.jupiter.api.BeforeEach;
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
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class SessionExamenServiceTest {

    @Mock
    private SessionExamenRepository sessionExamenRepository;
    @Mock
    private ExamenPublieRepository examenPublieRepository;
    @Mock
    private ResultatRepository resultatRepository;

    @InjectMocks
    private SessionExamenService sessionExamenService;

    private ExamenPublie examenPublie;
    private SessionExamen session;
    private LocalDateTime dateDebut;
    private LocalDateTime dateFin;

    @BeforeEach
    void setUp() {
        dateDebut = LocalDateTime.now().minusHours(1);
        dateFin = LocalDateTime.now().plusHours(3);
        examenPublie = new ExamenPublie();
        examenPublie.setId(10L);
        examenPublie.setTitre("Examen");
        examenPublie.setDuree(60);
        session = new SessionExamen();
        session.setId(1L);
        session.setDateDebut(dateDebut);
        session.setDateFin(dateFin);
        session.setStatut("PLANIFIE");
        session.setExamenPublie(examenPublie);
    }

    @Test
    void getAllSessions_repositoryReturnsOne_returnsOneDto() {
        // GIVEN
        when(sessionExamenRepository.findAll()).thenReturn(List.of(session));

        // WHEN
        List<SessionExamenResponse> result = sessionExamenService.getAllSessions();

        // THEN
        assertThat(result).hasSize(1);
        assertThat(result.get(0).getId()).isEqualTo(1L);
        assertThat(result.get(0).getStatut()).isEqualTo("PLANIFIE");
    }

    @Test
    void getSessionById_exists_returnsDto() {
        // GIVEN
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));

        // WHEN
        SessionExamenResponse r = sessionExamenService.getSessionById(1L);

        // THEN
        assertThat(r.getId()).isEqualTo(1L);
        assertThat(r.getExamenTitre()).isEqualTo("Examen");
    }

    @Test
    void getSessionById_missing_throwsSessionNotFoundException() {
        // GIVEN
        when(sessionExamenRepository.findById(99L)).thenReturn(Optional.empty());

        // WHEN / THEN
        assertThatThrownBy(() -> sessionExamenService.getSessionById(99L))
                .isInstanceOf(SessionNotFoundException.class);
    }

    @Test
    void getSessionsByExamenPublie_examenMissing_throwsIllegalArgumentException() {
        // GIVEN
        when(examenPublieRepository.existsById(10L)).thenReturn(false);

        // WHEN / THEN
        assertThatThrownBy(() -> sessionExamenService.getSessionsByExamenPublie(10L))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("Examen publié introuvable");
    }

    @Test
    void createSession_validRequest_savesAndReturns() {
        // GIVEN
        SessionExamenRequest req = new SessionExamenRequest(dateDebut, dateFin, "PLANIFIE", 10L);
        when(examenPublieRepository.findById(10L)).thenReturn(Optional.of(examenPublie));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> {
            SessionExamen s = inv.getArgument(0);
            s.setId(5L);
            return s;
        });

        // WHEN
        SessionExamenResponse r = sessionExamenService.createSession(req);

        // THEN
        assertThat(r.getId()).isEqualTo(5L);
        verify(sessionExamenRepository).save(any(SessionExamen.class));
    }

    @Test
    void createSession_dateDebutAfterFin_throwsIllegalArgumentException() {
        // GIVEN
        SessionExamenRequest req = new SessionExamenRequest(dateFin, dateDebut, "PLANIFIE", 10L);
        when(examenPublieRepository.findById(10L)).thenReturn(Optional.of(examenPublie));

        // WHEN / THEN
        assertThatThrownBy(() -> sessionExamenService.createSession(req))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("Dates invalides");
    }

    @Test
    void demarrerSession_futureStart_throwsInvalidSessionStateException() {
        // GIVEN
        session.setDateDebut(LocalDateTime.now().plusHours(2));
        session.setDateFin(LocalDateTime.now().plusHours(4));
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));

        // WHEN / THEN
        assertThatThrownBy(() -> sessionExamenService.demarrerSession(1L))
                .isInstanceOf(InvalidSessionStateException.class)
                .hasMessageContaining("pas encore commencé");
    }

    @Test
    void terminerSession_setsStatutTermine() {
        // GIVEN
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> inv.getArgument(0));

        // WHEN
        SessionExamenResponse r = sessionExamenService.terminerSession(1L);

        // THEN
        assertThat(r.getStatut()).isEqualTo("TERMINE");
    }

    @Test
    void corrigerExamen_sessionNotTerminee_throwsInvalidSessionStateException() {
        // GIVEN
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));

        // WHEN / THEN
        assertThatThrownBy(() -> sessionExamenService.corrigerExamen(1L))
                .isInstanceOf(InvalidSessionStateException.class);
    }

    @Test
    void corrigerExamen_terminéeAvecResultats_retournePourcentage() {
        // GIVEN
        session.setStatut("TERMINE");
        Resultat ok = new Resultat();
        ok.setCorrecte(true);
        Resultat ko = new Resultat();
        ko.setCorrecte(false);
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));
        when(resultatRepository.findBySessionExamenId(1L)).thenReturn(List.of(ok, ko));

        // WHEN
        double score = sessionExamenService.corrigerExamen(1L);

        // THEN
        assertThat(score).isEqualTo(50.0);
    }
}
