package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class ExamenCorrectionsEtudiantResponse {
    private Long etudiantId;
    private Double noteProposee;
    private Double baremeReference;
    private boolean valideeParProf;
    private List<ExamenCorrectionLigneResponse> lignes;
}
