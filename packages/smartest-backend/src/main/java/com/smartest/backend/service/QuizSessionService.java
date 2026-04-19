package com.smartest.backend.service;

import com.smartest.backend.dto.request.SoumissionReponse;
import com.smartest.backend.dto.response.CorrectionImmediateResponse;
import com.smartest.backend.entity.*;
import com.smartest.backend.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.util.HashMap;
import java.util.Map;

@Service
@RequiredArgsConstructor

public class QuizSessionService {

    private final ResultatRepository resultatRepository;

    public boolean estPremiereTentative(Long etudiantId, Long quizId) {
        return !resultatRepository.existsByEtudiantIdAndQuizId(etudiantId, quizId);
    }

    public void sauvegarderResultat(Resultat resultat, double score) {
        resultat.setScore(score);
        resultat.setDatePassage(java.time.LocalDateTime.now());
        resultat.setEstPremiereTentative(true);

        resultatRepository.save(resultat);
    }
}