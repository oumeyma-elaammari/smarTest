package com.smartest.backend.service;

import com.smartest.backend.entity.Resultat;
import com.smartest.backend.repository.ResultatRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
@DisplayName("ResultatService — Tests unitaires")
class ResultatServiceTest {

    @Mock
    private ResultatRepository resultatRepository;

    @InjectMocks
    private ResultatService resultatService;

    private Resultat r1;
    private Resultat r2;
    private Resultat r3;

    @BeforeEach
    void setUp() {
        r1 = res(true);
        r2 = res(true);
        r3 = res(false);
    }

    private static Resultat res(boolean ok) {
        Resultat r = new Resultat();
        r.setCorrecte(ok);
        return r;
    }

    @Test
    @DisplayName("getByEtudiant avec résultats → liste non vide")
    void getByEtudiantAvecDonneesRetourneListe() {
        when(resultatRepository.findByEtudiantId(7L)).thenReturn(List.of(r1, r2));

        List<Resultat> rows = resultatService.getByEtudiant(7L);

        assertThat(rows).hasSize(2);
    }

    @Test
    @DisplayName("getByEtudiant étudiant inexistant côté repo → liste vide")
    void getByEtudiantInconnuRetourneListeVide() {
        when(resultatRepository.findByEtudiantId(999L)).thenReturn(List.of());

        assertThat(resultatService.getByEtudiant(999L)).isEmpty();
    }

    @Test
    @DisplayName("calculerScoreQuiz : 3 bonnes / 5 → 60 %")
    void calculerScoreQuizTroisSurCinq() {
        when(resultatRepository.findByEtudiantIdAndSessionExamenIsNull(7L))
                .thenReturn(List.of(res(true), res(true), res(true), res(false), res(false)));

        double score = resultatService.calculerScoreQuiz(7L);

        assertThat(score).isEqualTo(60.0);
    }

    @Test
    @DisplayName("calculerScoreSession agrège les résultats de session")
    void calculerScoreSessionMoyenneCorrecte() {
        when(resultatRepository.findBySessionExamenId(50L))
                .thenReturn(List.of(res(true), res(false)));

        double score = resultatService.calculerScoreSession(50L);

        assertThat(score).isEqualTo(50.0);
    }

    @Test
    @DisplayName("delete supprime par identifiant")
    void deleteSupprimeLeResultat() {
        resultatService.delete(42L);
        verify(resultatRepository).deleteById(42L);
    }

    @Test
    @DisplayName("getMesResultatsQuiz filtre session null")
    void getMesResultatsQuizDelegueAuRepository() {
        when(resultatRepository.findByEtudiantIdAndSessionExamenIsNull(7L)).thenReturn(List.of(r1));

        List<Resultat> rows = resultatService.getMesResultatsQuiz(7L);

        assertThat(rows).containsExactly(r1);
        verify(resultatRepository).findByEtudiantIdAndSessionExamenIsNull(7L);
    }

    @Test
    @DisplayName("getMesResultatsExamens filtre session non null")
    void getMesResultatsExamensDelegueAuRepository() {
        when(resultatRepository.findByEtudiantIdAndSessionExamenIsNotNull(7L)).thenReturn(List.of(r3));

        List<Resultat> rows = resultatService.getMesResultatsExamens(7L);

        assertThat(rows).containsExactly(r3);
    }
}
