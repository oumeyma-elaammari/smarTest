package com.smartest.backend.service;

import com.smartest.backend.dto.qr.QrLiveStreamMessageType;
import com.smartest.backend.dto.qr.QrLiveStreamEnvelope;
import com.smartest.backend.dto.qr.QuizQrEphemeralProfInitPayload;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.messaging.simp.SimpMessagingTemplate;

import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class QuizSessionManagerTest {

    private static final String PROF_EMAIL = "prof@test.com";

    @Mock
    private SimpMessagingTemplate messagingTemplate;

    @InjectMocks
    private QuizSessionManager quizSessionManager;

    @Test
    void createInitSnapshotClose() {
        String token = quizSessionManager.createSession(1L, PROF_EMAIL);

        QuizQrEphemeralProfInitPayload payload = new QuizQrEphemeralProfInitPayload();
        payload.setSessionToken(token);
        payload.setTitre("T1");
        QuizQrEphemeralProfInitPayload.QrEphemeralQuestionPayload q = new QuizQrEphemeralProfInitPayload.QrEphemeralQuestionPayload();
        q.setEnonce("?");
        q.setOptionA("a");
        q.setOptionB("b");
        q.setOptionC("c");
        q.setOptionD("d");
        q.setReponseCorrecte("B");
        payload.setQuestions(List.of(q));

        quizSessionManager.initFromProf(token, PROF_EMAIL, payload);

        Optional<QrLiveStreamEnvelope> snap = quizSessionManager.snapshotEnvelope(token);
        assertThat(snap).isPresent();
        assertThat(snap.get().getType()).isEqualTo(QrLiveStreamMessageType.QUIZ);
        assertThat(snap.get().getQuiz().getNombreQuestions()).isEqualTo(1);

        quizSessionManager.submitAnswer(token, "p1", "c1", 1, "B");
        verify(messagingTemplate, atLeastOnce()).convertAndSend(eq("/topic/quiz-qr/" + token + "/stream"),
                any(QrLiveStreamEnvelope.class));

        quizSessionManager.closeSession(1L, PROF_EMAIL, token);
        assertThat(quizSessionManager.snapshotEnvelope(token)).isEmpty();
    }

    @Test
    void initRefuseSiMauvaisProf() {
        String token = quizSessionManager.createSession(1L, PROF_EMAIL);
        QuizQrEphemeralProfInitPayload payload = new QuizQrEphemeralProfInitPayload();
        payload.setSessionToken(token);
        payload.setTitre("T");
        QuizQrEphemeralProfInitPayload.QrEphemeralQuestionPayload q = new QuizQrEphemeralProfInitPayload.QrEphemeralQuestionPayload();
        q.setEnonce("?");
        q.setOptionA("a");
        q.setOptionB("b");
        q.setOptionC("c");
        q.setOptionD("d");
        q.setReponseCorrecte("A");
        payload.setQuestions(List.of(q));

        assertThatThrownBy(() -> quizSessionManager.initFromProf(token, "autre@test.com", payload))
                .isInstanceOf(org.springframework.web.server.ResponseStatusException.class);
    }
}
