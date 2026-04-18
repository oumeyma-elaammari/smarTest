package com.smartest.backend.dto.request;

import jakarta.validation.constraints.*;
import lombok.Data;

@Data
public class UpdateProfesseurRequest {

    @Pattern(
            regexp = "^[a-zA-ZÀ-ÿ\\s\\-]{2,50}$",
            message = "Le nom ne doit contenir que des lettres (2 à 50 caractères)"
    )
    private String nom;
}