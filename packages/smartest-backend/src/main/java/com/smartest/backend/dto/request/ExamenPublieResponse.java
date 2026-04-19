package com.smartest.backend.dto.request;

import java.time.LocalDateTime;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor

public class ExamenPublieResponse {

    private Long id;
    private String titre;
    private Integer duree;
    private String description;

    private String statut;

    private LocalDateTime dateDebut;
    private LocalDateTime dateFin;
}
