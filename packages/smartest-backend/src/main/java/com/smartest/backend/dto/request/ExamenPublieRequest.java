package com.smartest.backend.dto.request;

import lombok.Data;
import java.time.LocalDateTime;

@Data
public class ExamenPublieRequest {
    private String titre;
    private Integer duree;
    private String description;
    private Long professeurId;
    private LocalDateTime dateDebut;
    private LocalDateTime dateFin;
}