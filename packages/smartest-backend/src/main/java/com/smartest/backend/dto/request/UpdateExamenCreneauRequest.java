package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotNull;
import lombok.Data;

import java.time.LocalDateTime;

@Data
public class UpdateExamenCreneauRequest {

    @NotNull
    private LocalDateTime dateDebut;

    // Si renseignée, met à jour la durée et recalcule la date de fin
    private Integer duree;
}
