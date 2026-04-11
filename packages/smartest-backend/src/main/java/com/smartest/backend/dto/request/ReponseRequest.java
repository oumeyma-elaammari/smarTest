package com.smartest.backend.dto.request;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class ReponseRequest {

    @NotBlank(message = "Le contenu de la réponse est obligatoire")
    private String contenu;

    private Boolean correcte = false;

    @NotNull(message = "L'ID de la question est obligatoire")
    private Long questionId;
}
