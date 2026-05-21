package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class ExamenEtudiantPassageResponse {
    private Long etudiantId;
    private String email;
    private String nom;
    private Double noteProposee;
    private Double noteFinale;
    private boolean valideeParProf;
}
