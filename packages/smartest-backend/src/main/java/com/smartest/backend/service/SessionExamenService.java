package com.smartest.backend.service;

import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.Collectors;

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
                .orElseThrow(() -> new RuntimeException("Session non trouvée"));
        return convertToResponseDTO(session);
    }

    /**
     * 🔹 Sessions par examen publié
     */
    @Transactional(readOnly = true)
    public List<SessionExamenResponse> getSessionsByExamenPublie(Long examenPublieId) {

        if (!examenPublieRepository.existsById(examenPublieId)) {
            throw new RuntimeException("Examen publié non trouvé");
        }

        return sessionExamenRepository.findByExamenPublieId(examenPublieId)
                .stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * 🔹 Sessions en cours
     */
    /*@Transactional(readOnly = true)
    public List<SessionExamenResponse> getSessionsEnCours() {
        return sessionExamenRepository.findSessionsEnCours()
                .stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }*/

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
                .orElseThrow(() -> new RuntimeException("Examen publié non trouvé"));

        if (request.getDateDebut().isAfter(request.getDateFin())) {
            throw new RuntimeException("Date début > date fin");
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
                .orElseThrow(() -> new RuntimeException("Session non trouvée"));

        if (request.getDateDebut().isAfter(request.getDateFin())) {
            throw new RuntimeException("Date invalide");
        }

        session.setDateDebut(request.getDateDebut());
        session.setDateFin(request.getDateFin());

        if (request.getStatut() != null) {
            session.setStatut(request.getStatut());
        }

        if (request.getExamenPublieId() != null &&
                !session.getExamenPublie().getId().equals(request.getExamenPublieId())) {

            ExamenPublie examenPublie = examenPublieRepository.findById(request.getExamenPublieId())
                    .orElseThrow(() -> new RuntimeException("Examen publié non trouvé"));

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
                .orElseThrow(() -> new RuntimeException("Session non trouvée"));

        if (session.getDateDebut().isAfter(LocalDateTime.now())) {
            throw new RuntimeException("Session pas encore commencée");
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
                .orElseThrow(() -> new RuntimeException("Session non trouvée"));

        session.setStatut("TERMINE");

        return convertToResponseDTO(sessionExamenRepository.save(session));
    }

    /**
     * 🔥 ANNULER
     */
    @Transactional
    public SessionExamenResponse annulerSession(Long id) {

        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Session non trouvée"));

        session.setStatut("ANNULE");

        return convertToResponseDTO(sessionExamenRepository.save(session));
    }

    /**
     * 🔥 DELETE
     */
    @Transactional
    public void deleteSession(Long id) {

        if (!sessionExamenRepository.existsById(id)) {
            throw new RuntimeException("Session non trouvée");
        }

        sessionExamenRepository.deleteById(id);
    }

    /**
     * 🔥 Vérifier examen en cours
     */
   /* @Transactional(readOnly = true)
    public boolean isExamenEnCours(Long examenPublieId) {
        return sessionExamenRepository.isExamenEnCours(examenPublieId);
    }
*/
    /**
     * 🔥 CORRIGER EXAMEN
     */
    public double corrigerExamen(Long sessionId) {

        SessionExamen session = sessionExamenRepository.findById(sessionId)
                .orElseThrow(() -> new RuntimeException("Session non trouvée"));

        if (!"TERMINE".equals(session.getStatut())) {
            throw new RuntimeException("Examen non terminé");
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