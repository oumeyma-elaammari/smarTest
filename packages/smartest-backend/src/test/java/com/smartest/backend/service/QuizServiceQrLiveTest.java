package com.smartest.backend.service;

import com.smartest.backend.dto.response.QuizQrLiveStatsResponse;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Quiz;
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
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.http.HttpStatus;
import org.springframework.web.server.ResponseStatusException;

import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class QuizServiceQrLiveTest {

    private static final String PROF_TEST_EMAIL = "prof@test.com";

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
    private Quiz quiz;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setId(42L);
        professeur.setEmail(PROF_TEST_EMAIL);

        quiz = new Quiz();
        quiz.setId(100L);
        quiz.setProfesseur(professeur);
    }

    @Test
    void getQrLiveStatsRetourneSnapshotApresVerificationProprietaire() {
        when(professeurRepository.findByEmail(PROF_TEST_EMAIL)).thenReturn(Optional.of(professeur));
        when(quizRepository.findById(100L)).thenReturn(Optional.of(quiz));
        QuizQrLiveStatsResponse snap = QuizQrLiveStatsResponse.builder()
                .quizId(100L)
                .quizTitre("Mon quiz")
                .nombreParticipants(3)
                .build();
        when(quizQrLiveStatsService.snapshot(100L)).thenReturn(snap);

        QuizQrLiveStatsResponse result = quizService.getQrLiveStats(100L, PROF_TEST_EMAIL);

        assertThat(result).isSameAs(snap);
        verify(quizQrLiveStatsService).snapshot(100L);
    }

    @Test
    void getQrLiveStatsInterditSiEmailProfesseurInconnu() {
        when(professeurRepository.findByEmail("introuvable@test.com")).thenReturn(Optional.empty());

        assertThatThrownBy(() -> quizService.getQrLiveStats(100L, "introuvable@test.com"))
                .isInstanceOf(ResponseStatusException.class)
                .extracting(ex -> ((ResponseStatusException) ex).getStatusCode())
                .isEqualTo(HttpStatus.FORBIDDEN);
    }

    @Test
    void clearQrLiveStatsEffacePuisPublieSurTopicStomp() {
        when(professeurRepository.findByEmail(PROF_TEST_EMAIL)).thenReturn(Optional.of(professeur));
        when(quizRepository.findById(100L)).thenReturn(Optional.of(quiz));
        QuizQrLiveStatsResponse apresClear = QuizQrLiveStatsResponse.builder().quizId(100L).build();
        when(quizQrLiveStatsService.snapshot(100L)).thenReturn(apresClear);

        quizService.clearQrLiveStats(100L, PROF_TEST_EMAIL);

        verify(quizQrLiveStatsService).clear(100L);
        verify(quizQrLiveStatsService).snapshot(100L);
        ArgumentCaptor<QuizQrLiveStatsResponse> captor = ArgumentCaptor.forClass(QuizQrLiveStatsResponse.class);
        verify(messagingTemplate).convertAndSend(eq("/topic/quiz/100/qr-live"), captor.capture());
        assertThat(captor.getValue()).isSameAs(apresClear);
    }
}
