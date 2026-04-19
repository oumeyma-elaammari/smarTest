package com.smartest.backend.dto.request;

import jakarta.validation.constraints.*;
import lombok.Data;

@Data
public class ChangePasswordRequest {

    @NotBlank(message = "L'ancien mot de passe est obligatoire")
    private String oldPassword;

    @NotBlank(message = "Le nouveau mot de passe est obligatoire")
    @Pattern(
            regexp = "^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%^&*()_+\\-=]).{8,}$",
            message = "Min 8 caractères, 1 majuscule, 1 chiffre, 1 caractère spécial"
    )
    private String newPassword;

    @NotBlank(message = "La confirmation est obligatoire")
    private String confirmPassword;
}