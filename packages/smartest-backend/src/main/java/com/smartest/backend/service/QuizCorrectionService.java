package com.smartest.backend.service;

import com.smartest.backend.dto.request.ReponseEtudiantRequest;
import com.smartest.backend.dto.response.CorrectionResponse;
import com.smartest.backend.dto.response.ReponseResponse;
import com.smartest.backend.entity.Question;
import com.smartest.backend.entity.Reponse;
import com.smartest.backend.repository.QuestionRepository;
import com.smartest.backend.repository.ReponseRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class QuizCorrectionService {

    private final QuestionRepository questionRepository;
    private final ReponseRepository reponseRepository;

    /**
     * Corriger la réponse d'un étudiant à une question
     */
    @Transactional(readOnly = true)
    public CorrectionResponse corrigerReponse(ReponseEtudiantRequest request) {
        if (request == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Requête de correction invalide");
        }

        Question question = questionRepository.findById(request.getQuestionId())
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND,
                        "Question non trouvée avec l'id: " + request.getQuestionId()));

        Reponse reponseChoisie = reponseRepository.findById(request.getReponseId())
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND,
                        "Réponse non trouvée avec l'id: " + request.getReponseId()));

        boolean reponseAppartientALaQuestion = question.getReponses().stream()
                .anyMatch(r -> r.getId().equals(reponseChoisie.getId()));

        if (!reponseAppartientALaQuestion) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST,
                    "La réponse choisie n'appartient pas à cette question");
        }

        // Déterminer si la réponse est correcte
        boolean estCorrecte = Boolean.TRUE.equals(reponseChoisie.getCorrecte());

        // Récupérer toutes les réponses correctes
        List<ReponseResponse> reponsesCorrectes = question.getReponses().stream()
                .filter(r -> Boolean.TRUE.equals(r.getCorrecte()))
                .map(this::convertReponseToResponse)
                .collect(Collectors.toList());

        // Construire l'explication
        String explication = estCorrecte
                ? "Bonne réponse ! " + reponseChoisie.getContenu() + " est correct."
                : "Mauvaise réponse. La bonne réponse était : "
                + reponsesCorrectes.stream()
                .map(ReponseResponse::getContenu)
                .collect(Collectors.joining(", "));

        return new CorrectionResponse(
                question.getId(),
                question.getEnonce(),
                reponseChoisie.getId(),
                reponseChoisie.getContenu(),
                estCorrecte,
                reponsesCorrectes,
                explication
        );
    }

    /**
     * Corriger toutes les réponses d'un étudiant pour un quiz
     */
    @Transactional(readOnly = true)
    public List<CorrectionResponse> corrigerToutesLesReponses(List<ReponseEtudiantRequest> requests) {
        if (requests == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST,
                    "La liste des corrections est obligatoire");
        }
        return requests.stream()
                .map(this::corrigerReponse)
                .collect(Collectors.toList());
    }

    /**
     * Calculer le score d'un étudiant
     */
    @Transactional(readOnly = true)
    public double calculerScore(List<ReponseEtudiantRequest> requests) {
        if (requests == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST,
                    "La liste des corrections est obligatoire");
        }
        if (requests.isEmpty()) {
            return 0.0;
        }

        List<CorrectionResponse> corrections = corrigerToutesLesReponses(requests);
        long bonnesReponses = corrections.stream().filter(CorrectionResponse::isCorrect).count();
        return ((double) bonnesReponses / corrections.size()) * 100;
    }

    private ReponseResponse convertReponseToResponse(Reponse reponse) {
        ReponseResponse dto = new ReponseResponse();
        dto.setId(reponse.getId());
        dto.setContenu(reponse.getContenu());
        dto.setCorrecte(reponse.getCorrecte());
        return dto;
    }
}