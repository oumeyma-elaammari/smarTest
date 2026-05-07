package com.smartest.backend.service;

import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.exception.InvalidSessionStateException;
import com.smartest.backend.exception.QuestionNotFoundException;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Resultat;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.SessionExamen;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.ReponseRepository;
import com.smartest.backend.repository.ResultatRepository;
import com.smartest.backend.repository.SessionExamenRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.LocalDateTime;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
@DisplayName("ReponseService — Tests unitaires")
class ReponseServiceTest {

    @Mock
    private ReponseRepository reponseRepository;
    @Mock
    private QuestionRepository questionRepository;
    @Mock
    private ResultatRepository resultatRepository;
    @Mock
    private SessionExamenRepository sessionExamenRepository;
    @Mock
    private EtudiantRepository etudiantRepository;

    @InjectMocks
    private ReponseService reponseService;

    private Question question;
    private Reponse reponseCorrecte;
    private Reponse reponseFausse;
    private Etudiant etudiant;

    @BeforeEach
    void setUp() {
        question = new Question();
        question.setId(10L);

        reponseCorrecte = new Reponse();
        reponseCorrecte.setId(200L);
        reponseCorrecte.setQuestion(question);
        reponseCorrecte.setCorrecte(true);

        reponseFausse = new Reponse();
        reponseFausse.setId(201L);
        reponseFausse.setQuestion(question);
        reponseFausse.setCorrecte(false);

        etudiant = new Etudiant();
        etudiant.setId(50L);
    }

    @Test
    @DisplayName("verifierReponse bonne réponse → correcte vraie dans la réponse")
    void verifierReponseBonneReponse() {
        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenIsNull(50L, 10L)).thenReturn(false);
        when(reponseRepository.findById(200L)).thenReturn(Optional.of(reponseCorrecte));
        when(etudiantRepository.findById(50L)).thenReturn(Optional.of(etudiant));
        when(resultatRepository.save(any(Resultat.class))).thenAnswer(inv -> inv.getArgument(0));

        ReponseResponse dto = reponseService.verifierReponse(10L, 200L, 50L);

        assertThat(dto.getCorrecte()).isTrue();
        verify(resultatRepository).save(any(Resultat.class));
    }

    @Test
    @DisplayName("verifierReponse mauvaise réponse → correcte fausse")
    void verifierReponseMauvaiseReponse() {
        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenIsNull(50L, 10L)).thenReturn(false);
        when(reponseRepository.findById(201L)).thenReturn(Optional.of(reponseFausse));
        when(etudiantRepository.findById(50L)).thenReturn(Optional.of(etudiant));
        when(resultatRepository.save(any(Resultat.class))).thenAnswer(inv -> inv.getArgument(0));

        ReponseResponse dto = reponseService.verifierReponse(10L, 201L, 50L);

        assertThat(dto.getCorrecte()).isFalse();
    }

    @Test
    @DisplayName("verifierReponse question inexistante → QuestionNotFoundException")
    void verifierReponseQuestionInconnue() {
        when(questionRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> reponseService.verifierReponse(99L, 200L, 50L))
                .isInstanceOf(QuestionNotFoundException.class);
    }

    @Test
    @DisplayName("enregistrerReponseExamen session valide → enregistrement")
    void enregistrerReponseExamenSessionActiveEnregistre() {
        SessionExamen session = new SessionExamen();
        session.setId(5L);
        session.setStatut("EN_COURS");
        session.setDateFin(LocalDateTime.now().plusHours(1));

        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenId(50L, 10L, 5L)).thenReturn(false);
        when(reponseRepository.findById(200L)).thenReturn(Optional.of(reponseCorrecte));
        when(etudiantRepository.findById(50L)).thenReturn(Optional.of(etudiant));
        when(sessionExamenRepository.findById(5L)).thenReturn(Optional.of(session));
        when(resultatRepository.save(any(Resultat.class))).thenAnswer(inv -> inv.getArgument(0));

        reponseService.enregistrerReponseExamen(10L, 200L, 50L, 5L);

        verify(resultatRepository).save(any(Resultat.class));
    }

    @Test
    @DisplayName("enregistrerReponseExamen correspondance invalide → IllegalArgumentException")
    void enregistrerReponseExamenMauvaiseQuestionPourLaReponse() {
        Question autre = new Question();
        autre.setId(88L);
        Reponse reponse = new Reponse();
        reponse.setId(300L);
        reponse.setQuestion(autre);

        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenId(50L, 10L, 5L)).thenReturn(false);
        when(reponseRepository.findById(300L)).thenReturn(Optional.of(reponse));

        assertThatThrownBy(() -> reponseService.enregistrerReponseExamen(10L, 300L, 50L, 5L))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("correspond");
    }

    @Test
    @DisplayName("enregistrerReponseExamen session non active → InvalidSessionStateException")
    void enregistrerReponseExamenSessionTerminee() {
        SessionExamen session = new SessionExamen();
        session.setId(5L);
        session.setStatut("TERMINE");
        session.setDateFin(LocalDateTime.now().minusHours(1));

        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(resultatRepository.existsByEtudiantIdAndQuestionIdAndSessionExamenId(50L, 10L, 5L)).thenReturn(false);
        when(reponseRepository.findById(200L)).thenReturn(Optional.of(reponseCorrecte));
        when(etudiantRepository.findById(50L)).thenReturn(Optional.of(etudiant));
        when(sessionExamenRepository.findById(5L)).thenReturn(Optional.of(session));

        assertThatThrownBy(() -> reponseService.enregistrerReponseExamen(10L, 200L, 50L, 5L))
                .isInstanceOf(InvalidSessionStateException.class);
    }
}
