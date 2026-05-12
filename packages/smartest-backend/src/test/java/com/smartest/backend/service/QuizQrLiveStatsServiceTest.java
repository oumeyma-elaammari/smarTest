package com.smartest.backend.service;

import com.smartest.backend.dto.response.QuizQrLiveStatsResponse;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class QuizQrLiveStatsServiceTest {

    private QuizQrLiveStatsService service;

    @BeforeEach
    void setUp() {
        service = new QuizQrLiveStatsService();
    }

    @Test
    void snapshotQuizAbsentRetourneValeursNulles() {
        QuizQrLiveStatsResponse s = service.snapshot(999L);

        assertThat(s.getQuizId()).isEqualTo(999L);
        assertThat(s.getNombreParticipants()).isZero();
        assertThat(s.getTotalSoumissionsQuestions()).isZero();
        assertThat(s.getTauxReussiteGlobal()).isZero();
        assertThat(s.getStatistiquesParQuestion()).isEmpty();
    }

    @Test
    void recordVerificationAgregeParticipantsEtTaux() {
        service.recordVerification(1L, "Titre", "ABC", 10L, "Q1", true);
        service.recordVerification(1L, null, "abc", 10L, "Q1", false);
        service.recordVerification(1L, "Autre titre", null, 11L, "Q2 ?", true);

        QuizQrLiveStatsResponse s = service.snapshot(1L);

        assertThat(s.getQuizTitre()).isEqualTo("Autre titre");
        assertThat(s.getNombreParticipants()).isEqualTo(1);
        assertThat(s.getTotalSoumissionsQuestions()).isEqualTo(3);

        assertThat(s.getTauxReussiteGlobal()).isBetween(66.666, 66.667);

        assertThat(s.getStatistiquesParQuestion()).hasSize(2);
        assertThat(s.getStatistiquesParQuestion().get(0).getNombreReponses()).isEqualTo(2);
        assertThat(s.getStatistiquesParQuestion().get(0).getNombreCorrectes()).isEqualTo(1);
        assertThat(s.getStatistiquesParQuestion().get(0).getPourcentageReussite()).isEqualTo(50.0);

        assertThat(s.getStatistiquesParQuestion().get(1).getNombreIncorrectes()).isZero();
        assertThat(s.getStatistiquesParQuestion().get(1).getPourcentageReussite()).isEqualTo(100.0);
    }

    @Test
    void clearSupprimeLesDonnees() {
        service.recordVerification(5L, "Q", "p1", 1L, "e", true);
        assertThat(service.snapshot(5L).getTotalSoumissionsQuestions()).isEqualTo(1);

        service.clear(5L);

        assertThat(service.snapshot(5L).getTotalSoumissionsQuestions()).isZero();
    }
}
