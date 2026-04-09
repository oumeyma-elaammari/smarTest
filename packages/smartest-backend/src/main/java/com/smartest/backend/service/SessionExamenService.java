package com.smartest.backend.service;

import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import com.smartest.backend.dto.request.SessionExamenRequest;
import com.smartest.backend.dto.response.SessionExamenResponse;
import com.smartest.backend.entity.Examen;
import com.smartest.backend.entity.SessionExamen;
import com.smartest.backend.repository.ExamenRepository;
import com.smartest.backend.repository.SessionExamenRepository;

import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor
public class SessionExamenService {

    private final SessionExamenRepository sessionExamenRepository;
    private final ExamenRepository examenRepository;

    /**
     * Récupérer toutes les sessions
     */
    @Transactional(readOnly = true)
    public List<SessionExamenResponse> getAllSessions() {
        return sessionExamenRepository.findAll().stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer une session par son ID
     */
    @Transactional(readOnly = true)
    public SessionExamenResponse getSessionById(Long id) {
        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Session d'examen non trouvée avec l'id: " + id));
        return convertToResponseDTO(session);
    }

    /**
     * Récupérer les sessions d'un examen
     */
    @Transactional(readOnly = true)
    public List<SessionExamenResponse> getSessionsByExamen(Long examenId) {
        // Vérifier que l'examen existe
        if (!examenRepository.existsById(examenId)) {
            throw new RuntimeException("Examen non trouvé avec l'id: " + examenId);
        }

        List<SessionExamen> sessions = sessionExamenRepository.findByExamenId(examenId);
        return sessions.stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer les sessions en cours
     */
    @Transactional(readOnly = true)
    public List<SessionExamenResponse> getSessionsEnCours() {
        return sessionExamenRepository.findSessionsEnCours().stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Récupérer les sessions à venir
     */
    @Transactional(readOnly = true)
    public List<SessionExamenResponse> getSessionsAFaire() {
        return sessionExamenRepository.findSessionsAFaire(LocalDateTime.now()).stream()
                .map(this::convertToResponseDTO)
                .collect(Collectors.toList());
    }

    /**
     * Créer une nouvelle session d'examen
     */
    @Transactional
    public SessionExamenResponse createSession(SessionExamenRequest request) {
        // Vérifier l'existence de l'examen
        Examen examen = examenRepository.findById(request.getExamenId())
                .orElseThrow(() -> new RuntimeException("Examen non trouvé avec l'id: " + request.getExamenId()));

        // Vérifier que la date de début est avant la date de fin
        if (request.getDateDebut().isAfter(request.getDateFin())) {
            throw new RuntimeException("La date de début doit être avant la date de fin");
        }

        // Créer la session
        SessionExamen session = new SessionExamen();
        session.setDateDebut(request.getDateDebut());
        session.setDateFin(request.getDateFin());
        session.setExamen(examen);

        // Définir le statut par défaut
        if (request.getStatut() != null) {
            session.setStatut(request.getStatut());
        } else {
            session.setStatut("PLANIFIE");
        }

        SessionExamen savedSession = sessionExamenRepository.save(session);
        return convertToResponseDTO(savedSession);
    }

    /**
     * Mettre à jour une session
     */
    @Transactional
    public SessionExamenResponse updateSession(Long id, SessionExamenRequest request) {
        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Session d'examen non trouvée avec l'id: " + id));

        // Vérifier que la date de début est avant la date de fin
        if (request.getDateDebut().isAfter(request.getDateFin())) {
            throw new RuntimeException("La date de début doit être avant la date de fin");
        }

        session.setDateDebut(request.getDateDebut());
        session.setDateFin(request.getDateFin());

        if (request.getStatut() != null) {
            session.setStatut(request.getStatut());
        }

        // Mettre à jour l'examen si nécessaire
        if (request.getExamenId() != null && !session.getExamen().getId().equals(request.getExamenId())) {
            Examen examen = examenRepository.findById(request.getExamenId())
                    .orElseThrow(() -> new RuntimeException("Examen non trouvé avec l'id: " + request.getExamenId()));
            session.setExamen(examen);
        }

        SessionExamen updatedSession = sessionExamenRepository.save(session);
        return convertToResponseDTO(updatedSession);
    }

    /**
     * Démarrer une session (mettre le statut à EN_COURS)
     */
    @Transactional
    public SessionExamenResponse demarrerSession(Long id) {
        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Session d'examen non trouvée avec l'id: " + id));

        if (session.getDateDebut().isAfter(LocalDateTime.now())) {
            throw new RuntimeException("La session n'a pas encore commencé");
        }

        session.setStatut("EN_COURS");
        SessionExamen updatedSession = sessionExamenRepository.save(session);
        return convertToResponseDTO(updatedSession);
    }

    /**
     * Terminer une session (mettre le statut à TERMINE)
     */
    @Transactional
    public SessionExamenResponse terminerSession(Long id) {
        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Session d'examen non trouvée avec l'id: " + id));

        session.setStatut("TERMINE");
        SessionExamen updatedSession = sessionExamenRepository.save(session);
        return convertToResponseDTO(updatedSession);
    }

    /**
     * Annuler une session
     */
    @Transactional
    public SessionExamenResponse annulerSession(Long id) {
        SessionExamen session = sessionExamenRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("Session d'examen non trouvée avec l'id: " + id));

        session.setStatut("ANNULE");
        SessionExamen updatedSession = sessionExamenRepository.save(session);
        return convertToResponseDTO(updatedSession);
    }

    /**
     * Supprimer une session
     */
    @Transactional
    public void deleteSession(Long id) {
        if (!sessionExamenRepository.existsById(id)) {
            throw new RuntimeException("Session d'examen non trouvée avec l'id: " + id);
        }
        sessionExamenRepository.deleteById(id);
    }

    /**
     * Vérifier si un examen a une session en cours
     */
    @Transactional(readOnly = true)
    public boolean isExamenEnCours(Long examenId) {
        return sessionExamenRepository.isExamenEnCours(examenId);
    }

    /**
     * Convertir l'entité en DTO
     */
    private SessionExamenResponse convertToResponseDTO(SessionExamen session) {
        SessionExamenResponse dto = new SessionExamenResponse();
        dto.setId(session.getId());
        dto.setDateDebut(session.getDateDebut());
        dto.setDateFin(session.getDateFin());
        dto.setStatut(session.getStatut());

        if (session.getExamen() != null) {
            dto.setExamenId(session.getExamen().getId());
            dto.setExamenTitre(session.getExamen().getTitre());
            dto.setDureeExamen(session.getExamen().getDuree());
        }

        return dto;
    }
}
