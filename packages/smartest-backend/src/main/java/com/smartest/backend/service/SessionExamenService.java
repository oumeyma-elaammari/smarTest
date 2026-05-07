package com.smartest.backend.service;

import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.Collectors;

import com.smartest.backend.exception.InvalidSessionStateException;
import com.smartest.backend.exception.SessionNotFoundException;
import com.smartest.backend.entity.*;
import com.smartest.backend.repository.*;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import com.smartest.backend.dto.request.SessionExamenRequest;
import com.smartest.backend.dto.response.SessionExamenResponse;

import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor
public class SessionExamenService {
    private static final String SESSION_INTROUVABLE_PREFIX = "Session introuvable : ";
    private static final String EXAMEN_PUBLIE_INTROUVABLE_PREFIX = "Examen publié introuvable : ";
    private static final String STATUT_TERMINE = "TERMINE";

    private final SessionExamenRepository sessionExamenRepository;
    private final ExamenPublieRepository examenPublieRepository;
    private final ResultatRepository resultatRepository;

    /**
     * 🔹 Récupérer toutes les sessions
     */
    @Transactional(readOnly = true)
    public List<SessionExamenResponse> getAllSessions() {
        return sessionExamenRepository.findAll()
                .stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * 🔹 Récupérer une session par ID
     */
    @Transactional(readOnly = true)
    public SessionExamenResponse getSessionById(Long id) {
        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new SessionNotFoundException(SESSION_INTROUVABLE_PREFIX + id));
        return convertToResponseDTO(session);
    }

    /**
     * 🔹 Sessions par examen publié
     */
    @Transactional(readOnly = true)
    public List<SessionExamenResponse> getSessionsByExamenPublie(Long examenPublieId) {

        if (!examenPublieRepository.existsById(examenPublieId)) {
            throw new IllegalArgumentException(EXAMEN_PUBLIE_INTROUVABLE_PREFIX + examenPublieId);
        }

        return sessionExamenRepository.findByExamenPublieId(examenPublieId)
                .stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * 🔹 Sessions à venir
     */
    @Transactional(readOnly = true)
    public List<SessionExamenResponse> getSessionsAFaire() {
        return sessionExamenRepository.findSessionsAFaire(LocalDateTime.now())
                .stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * 🔥 CRÉER SESSION
     */
    @Transactional
    public SessionExamenResponse createSession(SessionExamenRequest request) {

        ExamenPublie examenPublie = examenPublieRepository.findById(request.getExamenPublieId())
                .orElseThrow(() -> new IllegalArgumentException(
                        EXAMEN_PUBLIE_INTROUVABLE_PREFIX + request.getExamenPublieId()));

        if (request.getDateDebut().isAfter(request.getDateFin())) {
            throw new IllegalArgumentException(
                    "Dates invalides : la date de début doit être antérieure ou égale à la date de fin.");
        }

        SessionExamen session = new SessionExamen();
        session.setDateDebut(request.getDateDebut());
        session.setDateFin(request.getDateFin());
        session.setExamenPublie(examenPublie);

        session.setStatut(
                request.getStatut() != null ? request.getStatut() : "PLANIFIE"
        );

        return convertToResponseDTO(sessionExamenRepository.save(session));
    }

    /**
     * 🔥 UPDATE SESSION
     */
    @Transactional
    public SessionExamenResponse updateSession(Long id, SessionExamenRequest request) {

        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new SessionNotFoundException(SESSION_INTROUVABLE_PREFIX + id));

        if (request.getDateDebut().isAfter(request.getDateFin())) {
            throw new IllegalArgumentException(
                    "Dates invalides : la date de début doit être antérieure ou égale à la date de fin.");
        }

        session.setDateDebut(request.getDateDebut());
        session.setDateFin(request.getDateFin());

        if (request.getStatut() != null) {
            session.setStatut(request.getStatut());
        }

        if (request.getExamenPublieId() != null &&
                !session.getExamenPublie().getId().equals(request.getExamenPublieId())) {

            ExamenPublie examenPublie = examenPublieRepository.findById(request.getExamenPublieId())
                    .orElseThrow(() -> new IllegalArgumentException(
                            EXAMEN_PUBLIE_INTROUVABLE_PREFIX + request.getExamenPublieId()));

            session.setExamenPublie(examenPublie);
        }

        return convertToResponseDTO(sessionExamenRepository.save(session));
    }

    /**
     * 🔥 DEMARRER
     */
    @Transactional
    public SessionExamenResponse demarrerSession(Long id) {

        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new SessionNotFoundException(SESSION_INTROUVABLE_PREFIX + id));

        if (STATUT_TERMINE.equals(session.getStatut()) || "ANNULE".equals(session.getStatut())) {
            throw new InvalidSessionStateException(
                    "Impossible de démarrer une session terminée ou annulée.");
        }

        if (session.getDateDebut().isAfter(LocalDateTime.now())) {
            throw new InvalidSessionStateException("La session n'a pas encore commencé (date de début future).");
        }

        session.setStatut("EN_COURS");

        return convertToResponseDTO(sessionExamenRepository.save(session));
    }

    /**
     * 🔥 TERMINER
     */
    @Transactional
    public SessionExamenResponse terminerSession(Long id) {

        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new SessionNotFoundException(SESSION_INTROUVABLE_PREFIX + id));

        session.setStatut(STATUT_TERMINE);

        return convertToResponseDTO(sessionExamenRepository.save(session));
    }

    /**
     * 🔥 ANNULER
     */
    @Transactional
    public SessionExamenResponse annulerSession(Long id) {

        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new SessionNotFoundException(SESSION_INTROUVABLE_PREFIX + id));

        session.setStatut("ANNULE");

        return convertToResponseDTO(sessionExamenRepository.save(session));
    }

    /**
     * 🔥 DELETE
     */
    @Transactional
    public void deleteSession(Long id) {

        if (!sessionExamenRepository.existsById(id)) {
            throw new SessionNotFoundException(SESSION_INTROUVABLE_PREFIX + id);
        }

        sessionExamenRepository.deleteById(id);
    }

    /**
     * 🔥 CORRIGER EXAMEN
     */
    public double corrigerExamen(Long sessionId) {

        SessionExamen session = sessionExamenRepository.findById(sessionId)
                .orElseThrow(() -> new SessionNotFoundException(SESSION_INTROUVABLE_PREFIX + sessionId));

        if (!STATUT_TERMINE.equals(session.getStatut())) {
            throw new InvalidSessionStateException("L'examen doit être terminé pour être corrigé.");
        }

        List<Resultat> resultats = resultatRepository.findBySessionExamenId(sessionId);

        int total = resultats.size();
        int correct = 0;

        for (Resultat r : resultats) {
            if (Boolean.TRUE.equals(r.getCorrecte())) {
                correct++;
            }
        }

        return total == 0 ? 0 : (double) correct / total * 100;
    }

    /**
     * 🔹 DTO
     */
    private SessionExamenResponse convertToResponseDTO(SessionExamen session) {

        SessionExamenResponse dto = new SessionExamenResponse();

        dto.setId(session.getId());
        dto.setDateDebut(session.getDateDebut());
        dto.setDateFin(session.getDateFin());
        dto.setStatut(session.getStatut());

        if (session.getExamenPublie() != null) {
            dto.setExamenId(session.getExamenPublie().getId());
            dto.setExamenTitre(session.getExamenPublie().getTitre());
            dto.setDureeExamen(session.getExamenPublie().getDuree());
        }

        return dto;
    }
}
