package com.smartest.backend.service;

import com.smartest.backend.dto.request.SessionExamenRequest;
import com.smartest.backend.dto.response.SessionExamenResponse;
import com.smartest.backend.entity.Examen;
import com.smartest.backend.entity.SessionExamen;
import com.smartest.backend.repository.ExamenRepository;
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

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class SessionExamenServiceTest {

    @Mock private SessionExamenRepository sessionExamenRepository;
    @Mock private ExamenRepository examenRepository;

    @InjectMocks
    private SessionExamenService sessionExamenService;

    private Examen examen;
    private SessionExamen session;
    private SessionExamenRequest request;

    private final LocalDateTime dateDebut = LocalDateTime.now().plusHours(1);
    private final LocalDateTime dateFin   = LocalDateTime.now().plusHours(3);

    @BeforeEach
    void setUp() {
        examen = new Examen();
        examen.setId(1L);
        examen.setTitre("Examen de Java");
        examen.setDuree(120);

        session = new SessionExamen();
        session.setId(1L);
        session.setDateDebut(dateDebut);
        session.setDateFin(dateFin);
        session.setStatut("PLANIFIE");
        session.setExamen(examen);

        request = new SessionExamenRequest();
        request.setExamenId(1L);
        request.setDateDebut(dateDebut);
        request.setDateFin(dateFin);
        request.setStatut("PLANIFIE");
    }

    // ─── getAllSessions ───────────────────────────────────────────────────────

    @Test
    void getAllSessions_returnsAllSessions() {
        when(sessionExamenRepository.findAll()).thenReturn(List.of(session));

        List<SessionExamenResponse> result = sessionExamenService.getAllSessions();

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getId()).isEqualTo(1L);
        assertThat(result.get(0).getStatut()).isEqualTo("PLANIFIE");
        assertThat(result.get(0).getExamenTitre()).isEqualTo("Examen de Java");
    }

    @Test
    void getAllSessions_returnsEmptyList_whenNoSessions() {
        when(sessionExamenRepository.findAll()).thenReturn(List.of());

        List<SessionExamenResponse> result = sessionExamenService.getAllSessions();

        assertThat(result).isEmpty();
    }

    // ─── getSessionById ───────────────────────────────────────────────────────

    @Test
    void getSessionById_returnsSession_whenExists() {
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));

        SessionExamenResponse result = sessionExamenService.getSessionById(1L);

        assertThat(result.getId()).isEqualTo(1L);
        assertThat(result.getExamenId()).isEqualTo(1L);
        assertThat(result.getDureeExamen()).isEqualTo(120);
    }

    @Test
    void getSessionById_throwsException_whenNotFound() {
        when(sessionExamenRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> sessionExamenService.getSessionById(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── getSessionsByExamen ──────────────────────────────────────────────────

    @Test
    void getSessionsByExamen_returnsSessions_whenExamenExists() {
        when(examenRepository.existsById(1L)).thenReturn(true);
        when(sessionExamenRepository.findByExamenId(1L)).thenReturn(List.of(session));

        List<SessionExamenResponse> result = sessionExamenService.getSessionsByExamen(1L);

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getExamenId()).isEqualTo(1L);
    }

    @Test
    void getSessionsByExamen_throwsException_whenExamenNotFound() {
        when(examenRepository.existsById(99L)).thenReturn(false);

        assertThatThrownBy(() -> sessionExamenService.getSessionsByExamen(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── getSessionsEnCours ───────────────────────────────────────────────────

    @Test
    void getSessionsEnCours_returnsSessions() {
        session.setStatut("EN_COURS");
        when(sessionExamenRepository.findSessionsEnCours()).thenReturn(List.of(session));

        List<SessionExamenResponse> result = sessionExamenService.getSessionsEnCours();

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getStatut()).isEqualTo("EN_COURS");
    }

    @Test
    void getSessionsEnCours_returnsEmptyList_whenNone() {
        when(sessionExamenRepository.findSessionsEnCours()).thenReturn(List.of());

        assertThat(sessionExamenService.getSessionsEnCours()).isEmpty();
    }

    // ─── getSessionsAFaire ────────────────────────────────────────────────────

    @Test
    void getSessionsAFaire_returnsFutureSessions() {
        when(sessionExamenRepository.findSessionsAFaire(any(LocalDateTime.class)))
                .thenReturn(List.of(session));

        List<SessionExamenResponse> result = sessionExamenService.getSessionsAFaire();

        assertThat(result).hasSize(1);
    }

    @Test
    void getSessionsAFaire_returnsEmptyList_whenNone() {
        when(sessionExamenRepository.findSessionsAFaire(any(LocalDateTime.class)))
                .thenReturn(List.of());

        assertThat(sessionExamenService.getSessionsAFaire()).isEmpty();
    }

    // ─── createSession ────────────────────────────────────────────────────────

    @Test
    void createSession_createsSession_withStatutFromRequest() {
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> {
            SessionExamen s = inv.getArgument(0);
            s.setId(2L);
            return s;
        });

        SessionExamenResponse result = sessionExamenService.createSession(request);

        assertThat(result.getId()).isEqualTo(2L);
        assertThat(result.getStatut()).isEqualTo("PLANIFIE");
        assertThat(result.getExamenId()).isEqualTo(1L);
    }

    @Test
    void createSession_setsStatutPlanifie_whenStatutIsNull() {
        request.setStatut(null);
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> inv.getArgument(0));

        SessionExamenResponse result = sessionExamenService.createSession(request);

        assertThat(result.getStatut()).isEqualTo("PLANIFIE");
    }

    @Test
    void createSession_throwsException_whenDateDebutAfterDateFin() {
        request.setDateDebut(dateFin);
        request.setDateFin(dateDebut);
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));

        assertThatThrownBy(() -> sessionExamenService.createSession(request))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("date de début");
    }

    @Test
    void createSession_throwsException_whenExamenNotFound() {
        when(examenRepository.findById(99L)).thenReturn(Optional.empty());
        request.setExamenId(99L);

        assertThatThrownBy(() -> sessionExamenService.createSession(request))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── updateSession ────────────────────────────────────────────────────────

    @Test
    void updateSession_updatesSession_successfully() {
        request.setStatut("EN_COURS");
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> inv.getArgument(0));

        SessionExamenResponse result = sessionExamenService.updateSession(1L, request);

        assertThat(result.getStatut()).isEqualTo("EN_COURS");
    }

    @Test
    void updateSession_throwsException_whenDateDebutAfterDateFin() {
        request.setDateDebut(dateFin);
        request.setDateFin(dateDebut);
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));

        assertThatThrownBy(() -> sessionExamenService.updateSession(1L, request))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("date de début");
    }

    @Test
    void updateSession_throwsException_whenSessionNotFound() {
        when(sessionExamenRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> sessionExamenService.updateSession(99L, request))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    @Test
    void updateSession_changesExamen_whenExamenIdDifferent() {
        Examen newExamen = new Examen();
        newExamen.setId(2L);
        newExamen.setTitre("Examen de Spring");
        newExamen.setDuree(90);

        request.setExamenId(2L);
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));
        when(examenRepository.findById(2L)).thenReturn(Optional.of(newExamen));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> inv.getArgument(0));

        SessionExamenResponse result = sessionExamenService.updateSession(1L, request);

        assertThat(result.getExamenId()).isEqualTo(2L);
        assertThat(result.getExamenTitre()).isEqualTo("Examen de Spring");
    }

    // ─── demarrerSession ──────────────────────────────────────────────────────

    @Test
    void demarrerSession_setsStatutEnCours_whenDateDebutPassed() {
        session.setDateDebut(LocalDateTime.now().minusHours(1));
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> inv.getArgument(0));

        SessionExamenResponse result = sessionExamenService.demarrerSession(1L);

        assertThat(result.getStatut()).isEqualTo("EN_COURS");
    }

    @Test
    void demarrerSession_throwsException_whenSessionNotStartedYet() {
        session.setDateDebut(LocalDateTime.now().plusHours(2));
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));

        assertThatThrownBy(() -> sessionExamenService.demarrerSession(1L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("pas encore commencé");
    }

    @Test
    void demarrerSession_throwsException_whenSessionNotFound() {
        when(sessionExamenRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> sessionExamenService.demarrerSession(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── terminerSession ──────────────────────────────────────────────────────

    @Test
    void terminerSession_setsStatutTermine() {
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> inv.getArgument(0));

        SessionExamenResponse result = sessionExamenService.terminerSession(1L);

        assertThat(result.getStatut()).isEqualTo("TERMINE");
    }

    @Test
    void terminerSession_throwsException_whenSessionNotFound() {
        when(sessionExamenRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> sessionExamenService.terminerSession(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── annulerSession ───────────────────────────────────────────────────────

    @Test
    void annulerSession_setsStatutAnnule() {
        when(sessionExamenRepository.findById(1L)).thenReturn(Optional.of(session));
        when(sessionExamenRepository.save(any(SessionExamen.class))).thenAnswer(inv -> inv.getArgument(0));

        SessionExamenResponse result = sessionExamenService.annulerSession(1L);

        assertThat(result.getStatut()).isEqualTo("ANNULE");
    }

    @Test
    void annulerSession_throwsException_whenSessionNotFound() {
        when(sessionExamenRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> sessionExamenService.annulerSession(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── deleteSession ────────────────────────────────────────────────────────

    @Test
    void deleteSession_deletesSession_whenExists() {
        when(sessionExamenRepository.existsById(1L)).thenReturn(true);

        sessionExamenService.deleteSession(1L);

        verify(sessionExamenRepository, times(1)).deleteById(1L);
    }

    @Test
    void deleteSession_throwsException_whenNotFound() {
        when(sessionExamenRepository.existsById(99L)).thenReturn(false);

        assertThatThrownBy(() -> sessionExamenService.deleteSession(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");

        verify(sessionExamenRepository, never()).deleteById(any());
    }

    // ─── isExamenEnCours ──────────────────────────────────────────────────────

    @Test
    void isExamenEnCours_returnsTrue_whenExamenHasActiveSession() {
        when(sessionExamenRepository.isExamenEnCours(1L)).thenReturn(true);

        assertThat(sessionExamenService.isExamenEnCours(1L)).isTrue();
    }

    @Test
    void isExamenEnCours_returnsFalse_whenNoActiveSession() {
        when(sessionExamenRepository.isExamenEnCours(1L)).thenReturn(false);

        assertThat(sessionExamenService.isExamenEnCours(1L)).isFalse();
    }
}