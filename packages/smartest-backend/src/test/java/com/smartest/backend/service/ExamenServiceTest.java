package com.smartest.backend.service;

import com.smartest.backend.dto.request.ExamenRequest;
import com.smartest.backend.dto.response.ExamenResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.*;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class ExamenServiceTest {

    @Mock private ExamenRepository examenRepository;
    @Mock private ProfesseurRepository professeurRepository;
    @Mock private CoursRepository coursRepository;
    @Mock private QuestionRepository questionRepository;

    @InjectMocks
    private ExamenService examenService;

    private Examen examen;
    private Professeur professeur;
    private Cours cours;
    private Question question;
    private Reponse reponse;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setId(1L);
        professeur.setNom("Dupont");

        cours = new Cours();
        cours.setId(10L);
        cours.setTitre("Algorithmique");

        reponse = new Reponse();
        reponse.setId(100L);
        reponse.setContenu("Java");
        reponse.setCorrecte(true);

        question = new Question();
        question.setId(50L);
        question.setEnonce("Quel langage utilise-t-on ?");
        question.setType(TypeQuestion.QCM);
        question.setDifficulte(Difficulte.FACILE);
        question.setReponses(List.of(reponse));

        examen = new Examen();
        examen.setId(1L);
        examen.setTitre("Examen de Java");
        examen.setDuree(120);
        examen.setProfesseur(professeur);
        examen.setCours(cours);
        examen.setQuestions(new ArrayList<>(List.of(question)));
    }

    // ─── getAllExamens ────────────────────────────────────────────────────────

    @Test
    void getAllExamens_returnsAllExamens() {
        when(examenRepository.findAll()).thenReturn(List.of(examen));

        List<ExamenResponse> result = examenService.getAllExamens();

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getTitre()).isEqualTo("Examen de Java");
        assertThat(result.get(0).getProfesseurNom()).isEqualTo("Dupont");
        assertThat(result.get(0).getCoursTitre()).isEqualTo("Algorithmique");
    }

    @Test
    void getAllExamens_returnsEmptyList_whenNoExamens() {
        when(examenRepository.findAll()).thenReturn(List.of());

        List<ExamenResponse> result = examenService.getAllExamens();

        assertThat(result).isEmpty();
    }

    // ─── getExamenById ────────────────────────────────────────────────────────

    @Test
    void getExamenById_returnsExamen_whenExists() {
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));

        ExamenResponse result = examenService.getExamenById(1L);

        assertThat(result.getId()).isEqualTo(1L);
        assertThat(result.getTitre()).isEqualTo("Examen de Java");
        assertThat(result.getDuree()).isEqualTo(120);
    }

    @Test
    void getExamenById_throwsException_whenNotFound() {
        when(examenRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> examenService.getExamenById(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── getExamensByProfesseur ───────────────────────────────────────────────

    @Test
    void getExamensByProfesseur_returnsExamens_forGivenProfesseur() {
        when(examenRepository.findByProfesseurId(1L)).thenReturn(List.of(examen));

        List<ExamenResponse> result = examenService.getExamensByProfesseur(1L);

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getProfesseurId()).isEqualTo(1L);
    }

    @Test
    void getExamensByProfesseur_returnsEmptyList_whenNoneFound() {
        when(examenRepository.findByProfesseurId(99L)).thenReturn(List.of());

        List<ExamenResponse> result = examenService.getExamensByProfesseur(99L);

        assertThat(result).isEmpty();
    }

    // ─── getExamensByCours ────────────────────────────────────────────────────

    @Test
    void getExamensByCours_returnsExamens_forGivenCours() {
        when(examenRepository.findByCoursId(10L)).thenReturn(List.of(examen));

        List<ExamenResponse> result = examenService.getExamensByCours(10L);

        assertThat(result).hasSize(1);
        assertThat(result.get(0).getCoursId()).isEqualTo(10L);
    }

    @Test
    void getExamensByCours_returnsEmptyList_whenNoneFound() {
        when(examenRepository.findByCoursId(99L)).thenReturn(List.of());

        assertThat(examenService.getExamensByCours(99L)).isEmpty();
    }

    // ─── createExamen ─────────────────────────────────────────────────────────

    @Test
    void createExamen_createsAndReturnsExamen_withCours() {
        ExamenRequest request = new ExamenRequest();
        request.setTitre("Nouvel examen");
        request.setDuree(90);
        request.setProfesseurId(1L);
        request.setCoursId(10L);
        request.setQuestionsIds(List.of(50L));

        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(coursRepository.findById(10L)).thenReturn(Optional.of(cours));
        when(questionRepository.findAllById(List.of(50L))).thenReturn(List.of(question));
        when(examenRepository.save(any(Examen.class))).thenAnswer(inv -> {
            Examen e = inv.getArgument(0);
            e.setId(2L);
            return e;
        });

        ExamenResponse result = examenService.createExamen(request);

        assertThat(result.getId()).isEqualTo(2L);
        assertThat(result.getTitre()).isEqualTo("Nouvel examen");
        assertThat(result.getDuree()).isEqualTo(90);
        assertThat(result.getProfesseurId()).isEqualTo(1L);
        assertThat(result.getCoursId()).isEqualTo(10L);
        assertThat(result.getQuestions()).hasSize(1);
    }

    @Test
    void createExamen_createsExamen_withoutCours() {
        ExamenRequest request = new ExamenRequest();
        request.setTitre("Examen sans cours");
        request.setDuree(60);
        request.setProfesseurId(1L);
        request.setCoursId(null);

        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(examenRepository.save(any(Examen.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenResponse result = examenService.createExamen(request);

        assertThat(result.getCoursId()).isNull();
        assertThat(result.getCoursTitre()).isNull();
    }

    @Test
    void createExamen_throwsException_whenProfesseurNotFound() {
        ExamenRequest request = new ExamenRequest();
        request.setProfesseurId(99L);

        when(professeurRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> examenService.createExamen(request))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    @Test
    void createExamen_throwsException_whenCoursNotFound() {
        ExamenRequest request = new ExamenRequest();
        request.setProfesseurId(1L);
        request.setCoursId(99L);

        when(professeurRepository.findById(1L)).thenReturn(Optional.of(professeur));
        when(coursRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> examenService.createExamen(request))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── updateExamen ─────────────────────────────────────────────────────────

    @Test
    void updateExamen_updatesAndReturnsExamen() {
        ExamenRequest request = new ExamenRequest();
        request.setTitre("Examen modifié");
        request.setDuree(150);
        request.setProfesseurId(1L);
        request.setCoursId(10L);
        request.setQuestionsIds(List.of(50L));

        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(coursRepository.findById(10L)).thenReturn(Optional.of(cours));
        when(questionRepository.findAllById(List.of(50L))).thenReturn(List.of(question));
        when(examenRepository.save(any(Examen.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenResponse result = examenService.updateExamen(1L, request);

        assertThat(result.getTitre()).isEqualTo("Examen modifié");
        assertThat(result.getDuree()).isEqualTo(150);
    }

    @Test
    void updateExamen_setsCours_toNull_whenCoursIdIsNull() {
        ExamenRequest request = new ExamenRequest();
        request.setTitre("Examen");
        request.setDuree(60);
        request.setProfesseurId(1L);
        request.setCoursId(null);

        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(examenRepository.save(any(Examen.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenResponse result = examenService.updateExamen(1L, request);

        assertThat(result.getCoursId()).isNull();
    }

    @Test
    void updateExamen_throwsException_whenExamenNotFound() {
        when(examenRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> examenService.updateExamen(99L, new ExamenRequest()))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── deleteExamen ─────────────────────────────────────────────────────────

    @Test
    void deleteExamen_deletesExamen_whenExists() {
        when(examenRepository.existsById(1L)).thenReturn(true);

        examenService.deleteExamen(1L);

        verify(examenRepository, times(1)).deleteById(1L);
    }

    @Test
    void deleteExamen_throwsException_whenNotFound() {
        when(examenRepository.existsById(99L)).thenReturn(false);

        assertThatThrownBy(() -> examenService.deleteExamen(99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");

        verify(examenRepository, never()).deleteById(any());
    }

    // ─── addQuestionToExamen ──────────────────────────────────────────────────

    @Test
    void addQuestionToExamen_addsQuestion_whenNotAlreadyPresent() {
        examen.setQuestions(new ArrayList<>());
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(questionRepository.findById(50L)).thenReturn(Optional.of(question));
        when(examenRepository.save(any(Examen.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenResponse result = examenService.addQuestionToExamen(1L, 50L);

        assertThat(result.getQuestions()).hasSize(1);
    }

    @Test
    void addQuestionToExamen_doesNotDuplicate_whenAlreadyPresent() {
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(questionRepository.findById(50L)).thenReturn(Optional.of(question));
        when(examenRepository.save(any(Examen.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenResponse result = examenService.addQuestionToExamen(1L, 50L);

        assertThat(result.getQuestions()).hasSize(1);
    }

    @Test
    void addQuestionToExamen_throwsException_whenExamenNotFound() {
        when(examenRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> examenService.addQuestionToExamen(99L, 50L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    @Test
    void addQuestionToExamen_throwsException_whenQuestionNotFound() {
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(questionRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> examenService.addQuestionToExamen(1L, 99L))
                .isInstanceOf(RuntimeException.class)
                .hasMessageContaining("99");
    }

    // ─── removeQuestionFromExamen ─────────────────────────────────────────────

    @Test
    void removeQuestionFromExamen_removesQuestion() {
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(examenRepository.save(any(Examen.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenResponse result = examenService.removeQuestionFromExamen(1L, 50L);

        assertThat(result.getQuestions()).isNullOrEmpty();
    }

    @Test
    void removeQuestionFromExamen_doesNothing_whenQuestionNotInExamen() {
        when(examenRepository.findById(1L)).thenReturn(Optional.of(examen));
        when(examenRepository.save(any(Examen.class))).thenAnswer(inv -> inv.getArgument(0));

        ExamenResponse result = examenService.removeQuestionFromExamen(1L, 999L);

        assertThat(result.getQuestions()).hasSize(1);
    }

    // ─── existsById ───────────────────────────────────────────────────────────

    @Test
    void existsById_returnsTrue_whenExamenExists() {
        when(examenRepository.existsById(1L)).thenReturn(true);

        assertThat(examenService.existsById(1L)).isTrue();
    }

    @Test
    void existsById_returnsFalse_whenExamenDoesNotExist() {
        when(examenRepository.existsById(99L)).thenReturn(false);

        assertThat(examenService.existsById(99L)).isFalse();
    }
}