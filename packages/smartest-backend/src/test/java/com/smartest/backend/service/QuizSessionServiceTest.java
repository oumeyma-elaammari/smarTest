package com.smartest.backend.service;

import com.smartest.backend.entity.Resultat;
import com.smartest.backend.repository.ResultatRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class QuizSessionServiceTest {

    @Mock
    private ResultatRepository resultatRepository;

    @InjectMocks
    private QuizSessionService quizSessionService;

    private Resultat resultat;

    @BeforeEach
    void setUp() {
        resultat = new Resultat();
        resultat.setId(77L);
    }

    @Test
    void estPremiereTentativeTrueSiAucuneLigneExist() {
        when(resultatRepository.existsByEtudiantIdAndQuizId(10L, 20L)).thenReturn(false);

        assertThat(quizSessionService.estPremiereTentative(10L, 20L)).isTrue();
        verify(resultatRepository).existsByEtudiantIdAndQuizId(10L, 20L);
    }

    @Test
    void estPremiereTentativeFalseSiResultatDejaPresent() {
        when(resultatRepository.existsByEtudiantIdAndQuizId(10L, 20L)).thenReturn(true);

        assertThat(quizSessionService.estPremiereTentative(10L, 20L)).isFalse();
    }

    @Test
    void sauvegarderResultatRemplitScoreDatesEtPersiste() {
        quizSessionService.sauvegarderResultat(resultat, 88.5);

        assertThat(resultat.getScore()).isEqualTo(88.5);
        assertThat(resultat.getEstPremiereTentative()).isTrue();
        assertThat(resultat.getDatePassage()).isNotNull();
        verify(resultatRepository).save(resultat);
    }
}
