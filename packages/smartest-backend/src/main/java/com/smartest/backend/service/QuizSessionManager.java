package com.smartest.backend.service;

import com.smartest.backend.dto.qr.QrAnswerFeedbackPayload;
import com.smartest.backend.dto.qr.QrLiveSubmitAnswerResponse;
import com.smartest.backend.dto.qr.QrLiveStreamEnvelope;
import com.smartest.backend.dto.qr.QrLiveStreamMessageType;
import com.smartest.backend.dto.qr.QuizQrEphemeralProfInitPayload;
import com.smartest.backend.dto.response.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.stereotype.Component;
import org.springframework.web.server.ResponseStatusException;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Sessions QR hors MySQL : contenu mémoire, stats agrégées, suppression totale au clôturer.
 */
@Component
@RequiredArgsConstructor
@Slf4j
public class QuizSessionManager {

    private final SimpMessagingTemplate messagingTemplate;

    private record InternalQuestion(int index,
                                    String enonce,
                                    String a, String b, String c, String d,
                                    String lettreCorrecte) {}

    private static final class Aggregate {
        int reponses = 0;
        int bonnes = 0;
        int incorrectes = 0;
    }

    private static final class SessionState {
        final long professeurId;
        final String profEmail;
        String titre = "";
        List<InternalQuestion> questions = Collections.emptyList();
        boolean closed;
        final Set<String> participants = ConcurrentHashMap.newKeySet();
        final ConcurrentHashMap<Integer, Aggregate> aggregates = new ConcurrentHashMap<>();

        SessionState(long professeurId, String profEmail) {
            this.professeurId = professeurId;
            this.profEmail = profEmail;
        }
    }

    private final ConcurrentHashMap<String, SessionState> sessions = new ConcurrentHashMap<>();

    public String createSession(long professeurId, String profEmailRaw) {
        String profEmailNorm = profEmailRaw == null ? "" : profEmailRaw.trim().toLowerCase(Locale.ROOT);
        String token = UUID.randomUUID().toString();
        sessions.put(token, new SessionState(professeurId, profEmailNorm));
        log.debug("Session QR créée token={}", token);
        return token;
    }

    public void initFromProf(String token, String profEmailNorm, QuizQrEphemeralProfInitPayload payload) {
        SessionState session = sessions.get(token);
        if (session == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Session introuvable");
        }
        if (session.closed) {
            throw new ResponseStatusException(HttpStatus.GONE, "Session fermée");
        }
        if (!session.profEmail.equalsIgnoreCase(profEmailNorm)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Session réservée à un autre compte.");
        }

        List<QuizQrEphemeralProfInitPayload.QrEphemeralQuestionPayload> rq = payload.getQuestions();
        if (rq == null || rq.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Liste de questions vide");
        }

        List<InternalQuestion> internes = new ArrayList<>(rq.size());
        for (int i = 0; i < rq.size(); i++) {
            QuizQrEphemeralProfInitPayload.QrEphemeralQuestionPayload q = rq.get(i);
            String correcte = q.getReponseCorrecte() != null ? q.getReponseCorrecte().trim().toUpperCase(Locale.ROOT) : "";
            if (!List.of("A", "B", "C", "D").contains(correcte)) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Réponse correcte invalide à la ligne " + (i + 1));
            }
            internes.add(new InternalQuestion(
                    i + 1,
                    q.getEnonce() == null ? "" : q.getEnonce().trim(),
                    nz(q.getOptionA()), nz(q.getOptionB()),
                    nz(q.getOptionC()), nz(q.getOptionD()),
                    correcte));
        }

        synchronized (session) {
            session.titre = payload.getTitre() != null ? payload.getTitre().trim() : "";
            session.questions = internes;
            session.aggregates.clear();
            for (InternalQuestion iq : internes) {
                session.aggregates.put(iq.index, new Aggregate());
            }
        }

        QuizPassageWebResponse passage = buildPassageSnapshot(token, session);
        messagingTemplate.convertAndSend(streamDestination(token),
                QrLiveStreamEnvelope.builder()
                        .type(QrLiveStreamMessageType.QUIZ)
                        .quiz(passage)
                        .build());
        publishStats(token, session);
    }

    /**
     * Réponse publique HTTP : déduit la lettre (A–D) depuis l’id d’option comme dans {@code buildPassageSnapshot}.
     */
    /**
     * @return vide si session ou question / réponse invalides ; sinon résultat avec la bonne réponse (id option).
     */
    public Optional<QrLiveSubmitAnswerResponse> submitAnswerByReponseId(String token,
                                                                        String participantId,
                                                                        String correlationId,
                                                                        long questionId,
                                                                        long reponseId) {
        SessionState session = sessions.get(token);
        if (session == null || session.closed) {
            return Optional.empty();
        }
        InternalQuestion q = session.questions.stream()
                .filter(x -> x.index == (int) questionId)
                .findFirst()
                .orElse(null);
        if (q == null) {
            return Optional.empty();
        }
        int idxOneBased = q.index;
        List<Long> expectedIds = List.of(
                100L * idxOneBased + 1,
                100L * idxOneBased + 2,
                100L * idxOneBased + 3,
                100L * idxOneBased + 4);
        int letterIdx = -1;
        for (int i = 0; i < expectedIds.size(); i++) {
            if (expectedIds.get(i) == reponseId) {
                letterIdx = i;
                break;
            }
        }
        if (letterIdx < 0 || letterIdx > 3) {
            return Optional.empty();
        }
        String lettre = List.of("A", "B", "C", "D").get(letterIdx);
        boolean correct = Objects.equals(q.lettreCorrecte, lettre);
        int bonneIdx = List.of("A", "B", "C", "D").indexOf(q.lettreCorrecte);
        if (bonneIdx < 0 || bonneIdx > 3) {
            bonneIdx = 0;
        }
        long bonneReponseId = expectedIds.get(bonneIdx);

        submitAnswer(token, participantId, correlationId, idxOneBased, lettre);
        return Optional.of(new QrLiveSubmitAnswerResponse(correct, bonneReponseId));
    }

    public void submitAnswer(String token,
                             String participantId,
                             String correlationId,
                             int questionIndexOneBased,
                             String lettre) {
        SessionState session = sessions.get(token);
        if (session == null || session.closed) {
            return;
        }
        List<InternalQuestion> qs = session.questions;
        if (qs.isEmpty()) {
            return;
        }

        InternalQuestion q = qs.stream()
                .filter(x -> x.index == questionIndexOneBased)
                .findFirst()
                .orElse(null);
        if (q == null) {
            return;
        }

        String l = lettre != null ? lettre.trim().toUpperCase(Locale.ROOT) : "";
        if (!List.of("A", "B", "C", "D").contains(l)) {
            return;
        }

        session.participants.add(participantId);
        Aggregate agg = session.aggregates.computeIfAbsent(questionIndexOneBased, k -> new Aggregate());
        boolean ok = Objects.equals(q.lettreCorrecte, l);

        synchronized (agg) {
            agg.reponses++;
            if (ok) {
                agg.bonnes++;
            } else {
                agg.incorrectes++;
            }
        }

        messagingTemplate.convertAndSend(streamDestination(token),
                QrLiveStreamEnvelope.builder()
                        .type(QrLiveStreamMessageType.FEEDBACK)
                        .feedback(QrAnswerFeedbackPayload.builder()
                                .correlationId(correlationId)
                                .questionIndex(questionIndexOneBased - 1)
                                .correcte(ok)
                                .build())
                        .build());

        publishStats(token, session);
    }

    public Optional<QrLiveStreamEnvelope> snapshotEnvelope(String token) {
        SessionState s = sessions.get(token);
        if (s == null) {
            return Optional.empty();
        }
        if (s.closed) {
            return Optional.of(QrLiveStreamEnvelope.builder().type(QrLiveStreamMessageType.CLOSED).build());
        }
        if (s.questions.isEmpty()) {
            return Optional.of(QrLiveStreamEnvelope.builder()
                    .type(QrLiveStreamMessageType.STATS)
                    .stats(buildStats(token, s))
                    .build());
        }
        return Optional.of(QrLiveStreamEnvelope.builder()
                .type(QrLiveStreamMessageType.QUIZ)
                .quiz(buildPassageSnapshot(token, s))
                .stats(buildStats(token, s))
                .build());
    }

    public void closeSession(long professeurId, String profEmailNorm, String token) {
        SessionState session = sessions.get(token);
        if (session == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Session introuvable ou déjà expirée");
        }
        if (!session.profEmail.equalsIgnoreCase(profEmailNorm) || session.professeurId != professeurId) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Impossible de fermer cette session");
        }

        terminateAndRemove(token, session);
    }

    /**
     * Clôture anonyme par jeton (secret dans l’URL). Utilisé par le web sans JWT et aligné sur le DELETE public.
     *
     * @return {@code false} si aucune session pour ce jeton.
     */
    public boolean forceCloseSession(String token) {
        SessionState session = sessions.get(token);
        if (session == null) {
            return false;
        }
        terminateAndRemove(token, session);
        return true;
    }

    /** Fin de session : notification clients puis aucune donnée résiduelle. */
    private void terminateAndRemove(String token, SessionState session) {
        synchronized (session) {
            session.closed = true;
        }
        messagingTemplate.convertAndSend(streamDestination(token),
                QrLiveStreamEnvelope.builder().type(QrLiveStreamMessageType.CLOSED).build());

        SessionState removed = sessions.remove(token);
        if (removed != null) {
            removed.participants.clear();
            removed.aggregates.clear();
        }
        log.debug("Session QR terminée token={}", token);
    }

    private void publishStats(String token, SessionState session) {
        messagingTemplate.convertAndSend(streamDestination(token),
                QrLiveStreamEnvelope.builder()
                        .type(QrLiveStreamMessageType.STATS)
                        .stats(buildStats(token, session))
                        .build());
    }

    private QuizQrLiveStatsResponse buildStats(String token, SessionState session) {
        List<QuestionQrLiveStatResponse> lignes = new ArrayList<>();
        List<InternalQuestion> qs = session.questions;
        Map<Integer, Aggregate> aggBy = session.aggregates;

        int totalSubs = 0;
        double sommePct = 0.0;

        for (InternalQuestion iq : qs) {
            Aggregate a = aggBy.getOrDefault(iq.index, new Aggregate());
            int n = Math.max(a.reponses, 0);
            totalSubs += n;
            double reussite = n == 0 ? 0.0 : ((double) a.bonnes / n) * 100.0;
            sommePct += reussite;
            lignes.add(QuestionQrLiveStatResponse.builder()
                    .questionId((long) iq.index)
                    .numeroQuestion(iq.index)
                    .questionEnonce(iq.enonce)
                    .nombreReponses(n)
                    .nombreCorrectes(a.bonnes)
                    .nombreIncorrectes(a.incorrectes)
                    .pourcentageReussite(reussite)
                    .pourcentageEchec(n == 0 ? 0.0 : 100.0 - reussite)
                    .build());
        }

        int nq = qs.size();
        double tauxGlobal = nq == 0 ? 0.0 : sommePct / nq;
        int participants = session.participants.size();

        return QuizQrLiveStatsResponse.builder()
                .quizId(absLongHash(token))
                .quizTitre(session.titre)
                .nombreParticipants(participants)
                .totalSoumissionsQuestions(totalSubs)
                .tauxReussiteGlobal(tauxGlobal)
                .statistiquesParQuestion(lignes)
                .build();
    }

    private static long absLongHash(String token) {
        long h = token.hashCode();
        return h == Long.MIN_VALUE ? 0L : Math.abs(h);
    }

    private QuizPassageWebResponse buildPassageSnapshot(String token, SessionState session) {
        QuizPassageWebResponse dto = new QuizPassageWebResponse();
        dto.setId(absLongHash(token));
        dto.setTitre(session.titre);
        dto.setNombreQuestions(session.questions.size());
        List<QuestionPassageWebResponse> out = new ArrayList<>();
        for (InternalQuestion iq : session.questions) {
            long qid = iq.index;
            QuestionPassageWebResponse qdto = new QuestionPassageWebResponse();
            qdto.setId(qid);
            qdto.setEnonce(iq.enonce);
            List<ReponsePassageWebResponse> reps = List.of(
                    rep(100 * qid + 1, iq.a),
                    rep(100 * qid + 2, iq.b),
                    rep(100 * qid + 3, iq.c),
                    rep(100 * qid + 4, iq.d));
            qdto.setReponses(reps);
            out.add(qdto);
        }
        dto.setQuestions(out);
        return dto;
    }

    private static ReponsePassageWebResponse rep(long id, String contenu) {
        ReponsePassageWebResponse r = new ReponsePassageWebResponse();
        r.setId(id);
        r.setContenu(contenu);
        return r;
    }

    private static String nz(String v) {
        return v == null ? "" : v.trim();
    }

    private static String streamDestination(String token) {
        return "/topic/quiz-qr/" + token + "/stream";
    }
}
