package com.smartest.backend.service;

import com.smartest.backend.dto.request.ReponseQuizDTO;
import com.smartest.backend.dto.request.SoumissionQuizRequest;
import com.smartest.backend.dto.request.SoumissionQuizWebRequest;
import com.smartest.backend.dto.request.VerificationQuestionWebRequest;
import com.smartest.backend.dto.response.QuizPassageWebResponse;
import com.smartest.backend.dto.response.QuizQrLiveStatsResponse;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.dto.response.ResultatQuizResponse;
import com.smartest.backend.dto.response.ResultatQuizWebResponse;
import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Quiz;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.entity.enumeration.StatutQuiz;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.QuizRepository;
import com.smartest.backend.repository.ReponseEtudiantRepository;
import com.smartest.backend.repository.ReponseRepository;
import com.smartest.backend.repository.ResultatRepository;
import com.smartest.backend.repository.StatistiqueQuestionRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.web.server.ResponseStatusException;

import com.smartest.backend.exception.InvalidQuizStateException;
import com.smartest.backend.exception.QuizNotFoundException;
import com.smartest.backend.exception.UnauthorizedAccessException;

import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Optional;
import java.util.Set;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

/**
 * Flux quiz : publication web, passage étudiant, soumissions, vérification de question.
 */
@ExtendWith(MockitoExtension.class)
class QuizServiceWebFlowsTest {

    private static final String EMAIL_PROF_SCHOOL = "prof@school.com";
    private static final String EMAIL_STUDENT_SCHOOL = "student@school.com";

    @Mock
    private QuizRepository quizRepository;
    @Mock
    private ProfesseurRepository professeurRepository;
    @Mock
    private QuestionRepository questionRepository;
    @Mock
    private ResultatRepository resultatRepository;
    @Mock
    private ReponseRepository reponseRepository;
    @Mock
    private EtudiantRepository etudiantRepository;
    @Mock
    private StatistiqueQuestionRepository statistiqueQuestionRepository;
    @Mock
    private ReponseEtudiantRepository reponseEtudiantRepository;
    @Mock
    private EmailService emailService;
    @Mock
    private StatistiqueRecalculService statistiqueRecalculService;
    @Mock
    private StatistiqueService statistiqueService;
    @Mock
    private QuizQrLiveStatsService quizQrLiveStatsService;
    @Mock
    private SimpMessagingTemplate messagingTemplate;

    @InjectMocks
    private QuizService quizService;

    private Professeur professeur;
    private Etudiant etudiant;
    private Quiz quizPublieWeb;
    private Reponse reponseBonne;
    private Reponse reponseFausse;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setId(1L);
        professeur.setEmail(EMAIL_PROF_SCHOOL);

        etudiant = new Etudiant();
        etudiant.setId(50L);
        etudiant.setEmail(EMAIL_STUDENT_SCHOOL);

        Question question = new Question();
        question.setId(100L);
        question.setEnonce("2+2 ?");
        question.setReponses(new ArrayList<>());

        reponseBonne = new Reponse();
        reponseBonne.setId(200L);
        reponseBonne.setContenu("4");
        reponseBonne.setCorrecte(true);
        reponseBonne.setQuestion(question);

        reponseFausse = new Reponse();
        reponseFausse.setId(201L);
        reponseFausse.setContenu("5");
        reponseFausse.setCorrecte(false);
        reponseFausse.setQuestion(question);

        question.setReponses(new ArrayList<>(List.of(reponseBonne, reponseFausse)));

        quizPublieWeb = new Quiz();
        quizPublieWeb.setId(10L);
        quizPublieWeb.setTitre("Math");
        quizPublieWeb.setDuree(20);
        quizPublieWeb.setStatut(StatutQuiz.PUBLIE);
        quizPublieWeb.setProfesseur(professeur);
        quizPublieWeb.setQuestions(new ArrayList<>(List.of(question)));
        quizPublieWeb.setEmailsAutorisesWeb(new LinkedHashSet<>(Set.of(EMAIL_STUDENT_SCHOOL)));
    }

    @Test
    void isPremiereTentativeTrueWhenAucunResultat() {
        when(resultatRepository.existsByEtudiantIdAndQuizId(50L, 10L)).thenReturn(false);
        assertThat(quizService.isPremiereTentative(10L, 50L)).isTrue();
    }

    @Test
    void isPremiereTentativeFalseWhenResultatExiste() {
        when(resultatRepository.existsByEtudiantIdAndQuizId(50L, 10L)).thenReturn(true);
        assertThat(quizService.isPremiereTentative(10L, 50L)).isFalse();
    }

    @Test
    void publierQuizMetStatutPublieEtSauvegarde() {
        Quiz brouillon = new Quiz();
        brouillon.setId(3L);
        brouillon.setStatut(StatutQuiz.BROUILLON);
        when(quizRepository.findById(3L)).thenReturn(Optional.of(brouillon));
        when(quizRepository.save(any(Quiz.class))).thenAnswer(inv -> inv.getArgument(0));

        quizService.publierQuiz(3L);

        ArgumentCaptor<Quiz> captor = ArgumentCaptor.forClass(Quiz.class);
        verify(quizRepository).save(captor.capture());
        assertThat(captor.getValue().getStatut()).isEqualTo(StatutQuiz.PUBLIE);
        assertThat(captor.getValue().getDatePublication()).isNotNull();
    }

    @Test
    void getQuizPubliesMappeLesQuiz() {
        when(quizRepository.findPublies()).thenReturn(List.of(quizPublieWeb));

        List<QuizResponse> list = quizService.getQuizPublies();

        assertThat(list).hasSize(1);
        assertThat(list.get(0).getId()).isEqualTo(10L);
        assertThat(list.get(0).getNombreQuestions()).isEqualTo(1);
    }

    @Test
    void getMesQuizsPublicationWebListeVideSiEmailVide() {
        assertThat(quizService.getMesQuizsPublicationWeb(null)).isEmpty();
        assertThat(quizService.getMesQuizsPublicationWeb("   ")).isEmpty();
        verify(quizRepository, never()).findPubliesAutorisesPourEmail(any());
    }

    @Test
    void getMesQuizsPublicationWebListeVideSiEtudiantInconnu() {
        when(etudiantRepository.findByEmail("x@y.com")).thenReturn(Optional.empty());

        assertThat(quizService.getMesQuizsPublicationWeb("x@y.com")).isEmpty();
        verify(quizRepository, never()).findPubliesAutorisesPourEmail(any());
    }

    @Test
    void getMesQuizsPublicationWebRetourneQuizsAvecPremiereTentative() {
        when(etudiantRepository.findByEmail(EMAIL_STUDENT_SCHOOL)).thenReturn(Optional.of(etudiant));
        when(quizRepository.findPubliesAutorisesPourEmail(EMAIL_STUDENT_SCHOOL)).thenReturn(List.of(quizPublieWeb));
        when(resultatRepository.existsByEtudiantIdAndQuizId(50L, 10L)).thenReturn(false);

        List<QuizResponse> list = quizService.getMesQuizsPublicationWeb(EMAIL_STUDENT_SCHOOL);

        assertThat(list).hasSize(1);
        assertThat(list.get(0).getPremiereTentative()).isTrue();
    }

    @Test
    void getQuizPourPassageWebInterditSiNonAutorise() {
        quizPublieWeb.getEmailsAutorisesWeb().clear();
        quizPublieWeb.getEmailsAutorisesWeb().add("autre@school.com");
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quizPublieWeb));

        assertThatThrownBy(() -> quizService.getQuizPourPassageWeb(10L, EMAIL_STUDENT_SCHOOL))
                .isInstanceOf(UnauthorizedAccessException.class);
    }

    @Test
    void getQuizPourPassageWebRetournePassageSansBonnesReponses() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quizPublieWeb));

        QuizPassageWebResponse dto = quizService.getQuizPourPassageWeb(10L, EMAIL_STUDENT_SCHOOL);

        assertThat(dto.getNombreQuestions()).isEqualTo(1);
        assertThat(dto.getQuestions()).hasSize(1);
        assertThat(dto.getQuestions().get(0).getReponses()).hasSize(2);
    }

    @Test
    void getQuizPourPassageQrInterditBrouillon() {
        Quiz b = new Quiz();
        b.setId(11L);
        b.setStatut(StatutQuiz.BROUILLON);
        when(quizRepository.findById(11L)).thenReturn(Optional.of(b));

        assertThatThrownBy(() -> quizService.getQuizPourPassageQr(11L))
                .isInstanceOf(InvalidQuizStateException.class);
    }

    @Test
    void soumettreQuizThrowsNotFoundQuandQuizInexistant() {
        when(quizRepository.findById(77L)).thenReturn(Optional.empty());

        SoumissionQuizRequest req = new SoumissionQuizRequest();
        req.setEtudiantId(50L);
        req.setReponses(new ArrayList<>());

        assertThatThrownBy(() -> quizService.soumettreQuiz(77L, req))
                .isInstanceOf(QuizNotFoundException.class);
        verify(resultatRepository, never()).save(any());
    }

    @Test
    void soumettreQuizThrowsBadRequestQuandListeReponsesNulle() {
        SoumissionQuizRequest req = new SoumissionQuizRequest();
        req.setEtudiantId(50L);
        req.setReponses(null);

        assertThatThrownBy(() -> quizService.soumettreQuiz(10L, req))
                .isInstanceOf(ResponseStatusException.class)
                .satisfies(ex -> assertThat(((ResponseStatusException) ex).getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST));
    }

    @Test
    void soumettreQuizCalculeScoreEtSauvegardeResultats() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quizPublieWeb));
        when(etudiantRepository.findById(50L)).thenReturn(Optional.of(etudiant));
        when(resultatRepository.existsByEtudiantIdAndQuizId(50L, 10L)).thenReturn(false);

        ReponseQuizDTO d1 = new ReponseQuizDTO();
        d1.setReponseId(200L);
        ReponseQuizDTO d2 = new ReponseQuizDTO();
        d2.setReponseId(201L);

        when(reponseRepository.findById(200L)).thenReturn(Optional.of(reponseBonne));
        when(reponseRepository.findById(201L)).thenReturn(Optional.of(reponseFausse));
        when(resultatRepository.save(any())).thenAnswer(inv -> inv.getArgument(0));

        SoumissionQuizRequest req = new SoumissionQuizRequest();
        req.setEtudiantId(50L);
        req.setReponses(List.of(d1, d2));

        ResultatQuizResponse res = quizService.soumettreQuiz(10L, req);

        assertThat(res.getScore()).isEqualTo(50.0);
        assertThat(res.getBonnesReponses()).isEqualTo(1);
        assertThat(res.getTotalQuestions()).isEqualTo(2);
        assertThat(res.getEstPremiereTentative()).isTrue();
        verify(resultatRepository, org.mockito.Mockito.times(2)).save(any());
        verify(statistiqueRecalculService).planifierApresDelai(10L);
    }

    @Test
    void verifierQuestionPassageWebRequeteIncompleteBadRequest() {
        assertThatThrownBy(() -> quizService.verifierQuestionPassageWeb(10L, EMAIL_STUDENT_SCHOOL, null, null))
                .isInstanceOf(ResponseStatusException.class)
                .extracting(ex -> ((ResponseStatusException) ex).getStatusCode())
                .isEqualTo(HttpStatus.BAD_REQUEST);
    }

    @Test
    void verifierQuestionPassageWebEnregistreStatsSiModeQr() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quizPublieWeb));
        QuizQrLiveStatsResponse snap = QuizQrLiveStatsResponse.builder().quizId(10L).quizTitre("Math").build();
        when(quizQrLiveStatsService.snapshot(10L)).thenReturn(snap);

        VerificationQuestionWebRequest req = new VerificationQuestionWebRequest();
        req.setQuestionId(100L);
        req.setReponseId(200L);

        quizService.verifierQuestionPassageWeb(10L, EMAIL_STUDENT_SCHOOL, req, "qr");

        verify(quizQrLiveStatsService).recordVerification(
                10L, "Math", EMAIL_STUDENT_SCHOOL, 100L, "2+2 ?", true);
        verify(messagingTemplate).convertAndSend(eq("/topic/quiz/10/qr-live"), eq(snap));
    }

    @Test
    void soumettreQuizWebPersisteEtRecalculeHorsModeQr() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quizPublieWeb));
        when(etudiantRepository.findByEmail(EMAIL_STUDENT_SCHOOL)).thenReturn(Optional.of(etudiant));
        when(resultatRepository.existsByEtudiantIdAndQuizId(50L, 10L)).thenReturn(false);
        when(resultatRepository.save(any())).thenAnswer(inv -> inv.getArgument(0));

        ReponseQuizDTO dto = new ReponseQuizDTO();
        dto.setQuestionId(100L);
        dto.setReponseId(200L);

        SoumissionQuizWebRequest req = new SoumissionQuizWebRequest();
        req.setReponses(List.of(dto));

        ResultatQuizWebResponse res = quizService.soumettreQuizWeb(
                10L, EMAIL_STUDENT_SCHOOL, req, null);

        assertThat(res.getBonnesReponses()).isEqualTo(1);
        assertThat(res.isEstPremiereTentative()).isTrue();
        verify(resultatRepository).save(any());
        verify(statistiqueRecalculService).planifierApresDelai(10L);
    }

    @Test
    void soumettreQuizQrNePersistePasResultatEtScoreCoherent() {
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quizPublieWeb));

        ReponseQuizDTO dto = new ReponseQuizDTO();
        dto.setQuestionId(100L);
        dto.setReponseId(201L);

        SoumissionQuizWebRequest req = new SoumissionQuizWebRequest();
        req.setReponses(List.of(dto));

        ResultatQuizWebResponse res = quizService.soumettreQuizQr(10L, req);

        assertThat(res.getBonnesReponses()).isEqualTo(0);
        assertThat(res.isEstPremiereTentative()).isTrue();
        assertThat(res.getScore()).isZero();
        verify(resultatRepository, never()).save(any());
    }

    @Test
    void publierSurLeWebEchecSiAucunEmailValide() {
        when(professeurRepository.findByEmail(EMAIL_PROF_SCHOOL)).thenReturn(Optional.of(professeur));
        when(quizRepository.findById(10L)).thenReturn(Optional.of(quizPublieWeb));

        assertThatThrownBy(() -> quizService.publierSurLeWeb(10L, EMAIL_PROF_SCHOOL, List.of("   ", "pas-email")))
                .isInstanceOf(ResponseStatusException.class)
                .extracting(ex -> ((ResponseStatusException) ex).getStatusCode())
                .isEqualTo(HttpStatus.BAD_REQUEST);
    }
}
