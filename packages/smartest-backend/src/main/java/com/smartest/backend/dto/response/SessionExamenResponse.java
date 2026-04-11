package com.smartest.backend.dto.response;

import java.time.LocalDateTime;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class SessionExamenResponse {

    private Long id;

    private LocalDateTime dateDebut;

    private LocalDateTime dateFin;

    private String statut;

    private Long examenId;

    private String examenTitre;

    private Integer dureeExamen;
}
