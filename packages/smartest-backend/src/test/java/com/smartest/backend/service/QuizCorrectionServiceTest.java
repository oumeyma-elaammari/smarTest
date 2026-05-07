package com.smartest.backend.service;

import com.smartest.backend.dto.request.ReponseEtudiantRequest;
import com.smartest.backend.dto.response.CorrectionResponse;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.Difficulte;
import com.smartest.backend.entity.enumeration.TypeQuestion;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.ReponseRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.web.server.ResponseStatusException;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class QuizCorrectionServiceTest {

    private static final String CONTENU_PARIS = "Paris";

    @Mock private QuestionRepository questionRepository;
    @Mock private ReponseRepository reponseRepository;

    @InjectMocks
    private QuizCorrectionService quizCorrectionService;

    private Question question;
    private Reponse reponseCorrecte;
    private Reponse reponseIncorrecte;
    private ReponseEtudiantRequest request;

    @BeforeEach
    void setUp() {
        reponseCorrecte = new Reponse();
        reponseCorrecte.setId(1L);
        reponseCorrecte.setContenu(CONTENU_PARIS);
        reponseCorrecte.setCorrecte(true);

        reponseIncorrecte = new Reponse();
        reponseIncorrecte.setId(2L);
        reponseIncorrecte.setContenu("Lyon");
        reponseIncorrecte.setCorrecte(false);

        question = new Question();
        question.setId(10L);
        question.setEnonce("Quelle est la capitale de la France ?");
        question.setType(TypeQuestion.QCM);
        question.setDifficulte(Difficulte.FACILE);
        question.setReponses(new ArrayList<>(List.of(reponseCorrecte, reponseIncorrecte)));

        request = new ReponseEtudiantRequest();
        request.setQuestionId(10L);
        request.setEtudiantId(5L);
    }

    // ─── corrigerReponse ──────────────────────────────────────────────────────

    @Test
    void corrigerReponseBadRequestWhenRequestNull() {
        assertThatThrownBy(() -> quizCorrectionService.corrigerReponse(null))
                .isInstanceOf(ResponseStatusException.class)
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST));
    }

    @Test
    void corrigerToutesLesReponsesBadRequestWhenListeNull() {
        assertThatThrownBy(() -> quizCorrectionService.corrigerToutesLesReponses(null))
                .isInstanceOf(ResponseStatusException.class)
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST));
    }

    @Test
    void calculerScoreBadRequestWhenListeNull() {
        assertThatThrownBy(() -> quizCorrectionService.calculerScore(null))
                .isInstanceOf(ResponseStatusException.class)
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST));
    }

    @Test
    void corrigerReponseReturnsCorrectWhenReponseIsCorrecte() {
        request.setReponseId(1L);
        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(reponseRepository.findById(1L)).thenReturn(Optional.of(reponseCorrecte));

        CorrectionResponse result = quizCorrectionService.corrigerReponse(request);

        assertThat(result.isCorrect()).isTrue();
        assertThat(result.getQuestionId()).isEqualTo(10L);
        assertThat(result.getReponseChoisieId()).isEqualTo(1L);
        assertThat(result.getReponseChoisieContenu()).isEqualTo(CONTENU_PARIS);
        assertThat(result.getReponsesCorrectes()).hasSize(1);
        assertThat(result.getExplication()).contains("Bonne réponse");
    }

    @Test
    void corrigerReponseReturnsIncorrectWhenReponseIsIncorrecte() {
        request.setReponseId(2L);
        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(reponseRepository.findById(2L)).thenReturn(Optional.of(reponseIncorrecte));

        CorrectionResponse result = quizCorrectionService.corrigerReponse(request);

        assertThat(result.isCorrect()).isFalse();
        assertThat(result.getReponseChoisieContenu()).isEqualTo("Lyon");
        assertThat(result.getReponsesCorrectes()).hasSize(1);
        assertThat(result.getReponsesCorrectes().get(0).getContenu()).isEqualTo(CONTENU_PARIS);
        assertThat(result.getExplication()).contains("Mauvaise réponse");
        assertThat(result.getExplication()).contains(CONTENU_PARIS);
    }

    @Test
    void corrigerReponseThrowsExceptionWhenQuestionNotFound() {
        request.setReponseId(1L);
        when(questionRepository.findById(99L)).thenReturn(Optional.empty());
        request.setQuestionId(99L);

        assertThatThrownBy(() -> quizCorrectionService.corrigerReponse(request))
                .isInstanceOf(ResponseStatusException.class)
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND))
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getReason()).contains("99"));
    }

    @Test
    void corrigerReponseThrowsExceptionWhenReponseNotFound() {
        request.setReponseId(99L);
        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(reponseRepository.findById(99L)).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizCorrectionService.corrigerReponse(request))
                .isInstanceOf(ResponseStatusException.class)
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND))
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getReason()).contains("99"));
    }

    @Test
    void corrigerReponseThrowsExceptionWhenReponseNAppartientPasALaQuestion() {
        Reponse reponseAutreQuestion = new Reponse();
        reponseAutreQuestion.setId(99L);
        reponseAutreQuestion.setContenu("Berlin");
        reponseAutreQuestion.setCorrecte(true);

        request.setReponseId(99L);
        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(reponseRepository.findById(99L)).thenReturn(Optional.of(reponseAutreQuestion));

        assertThatThrownBy(() -> quizCorrectionService.corrigerReponse(request))
                .isInstanceOf(ResponseStatusException.class)
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST))
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getReason()).contains("n'appartient pas"));
    }

    // ─── corrigerToutesLesReponses ────────────────────────────────────────────

    @Test
    void corrigerToutesLesReponsesReturnsAllCorrections() {
        ReponseEtudiantRequest req1 = new ReponseEtudiantRequest();
        req1.setQuestionId(10L);
        req1.setReponseId(1L);
        req1.setEtudiantId(5L);

        ReponseEtudiantRequest req2 = new ReponseEtudiantRequest();
        req2.setQuestionId(10L);
        req2.setReponseId(2L);
        req2.setEtudiantId(5L);

        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(reponseRepository.findById(1L)).thenReturn(Optional.of(reponseCorrecte));
        when(reponseRepository.findById(2L)).thenReturn(Optional.of(reponseIncorrecte));

        List<CorrectionResponse> results = quizCorrectionService.corrigerToutesLesReponses(List.of(req1, req2));

        assertThat(results).hasSize(2);
        assertThat(results.get(0).isCorrect()).isTrue();
        assertThat(results.get(1).isCorrect()).isFalse();
    }

    @Test
    void corrigerToutesLesReponsesReturnsEmptyListWhenNoRequests() {
        List<CorrectionResponse> results = quizCorrectionService.corrigerToutesLesReponses(List.of());

        assertThat(results).isEmpty();
    }

    // ─── calculerScore ────────────────────────────────────────────────────────

    @Test
    void calculerScoreReturns100WhenToutesLesReponsesCorrectes() {
        ReponseEtudiantRequest req = new ReponseEtudiantRequest();
        req.setQuestionId(10L);
        req.setReponseId(1L);
        req.setEtudiantId(5L);

        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(reponseRepository.findById(1L)).thenReturn(Optional.of(reponseCorrecte));

        double score = quizCorrectionService.calculerScore(List.of(req));

        assertThat(score).isEqualTo(100.0);
    }

    @Test
    void calculerScoreReturns0WhenAucuneReponseCorrecte() {
        ReponseEtudiantRequest req = new ReponseEtudiantRequest();
        req.setQuestionId(10L);
        req.setReponseId(2L);
        req.setEtudiantId(5L);

        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(reponseRepository.findById(2L)).thenReturn(Optional.of(reponseIncorrecte));

        double score = quizCorrectionService.calculerScore(List.of(req));

        assertThat(score).isEqualTo(0.0);
    }

    @Test
    void calculerScoreReturns50WhenMoitieCorrectes() {
        ReponseEtudiantRequest req1 = new ReponseEtudiantRequest();
        req1.setQuestionId(10L);
        req1.setReponseId(1L);
        req1.setEtudiantId(5L);

        ReponseEtudiantRequest req2 = new ReponseEtudiantRequest();
        req2.setQuestionId(10L);
        req2.setReponseId(2L);
        req2.setEtudiantId(5L);

        when(questionRepository.findById(10L)).thenReturn(Optional.of(question));
        when(reponseRepository.findById(1L)).thenReturn(Optional.of(reponseCorrecte));
        when(reponseRepository.findById(2L)).thenReturn(Optional.of(reponseIncorrecte));

        double score = quizCorrectionService.calculerScore(List.of(req1, req2));

        assertThat(score).isEqualTo(50.0);
    }

    @Test
    void calculerScoreReturns0WhenListeVide() {
        double score = quizCorrectionService.calculerScore(List.of());

        assertThat(score).isEqualTo(0.0);
    }
}