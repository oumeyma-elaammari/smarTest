package com.smartest.backend.dto.request;

import jakarta.validation.constraints.*;
import lombok.Data;

@Data
public class RegisterEtudiantRequest {

    @NotBlank(message = "Le nom est obligatoire")
    @Pattern(
            regexp = "^[a-zA-ZÀ-ÿ\\s\\-]{2,50}$",
            message = "Le nom ne doit contenir que des lettres"
    )
    private String nom;

    @Email(message = "Format email invalide")
    @NotBlank(message = "L'email est obligatoire")
    @Pattern(
            regexp = "^[a-zA-Z0-9._%+\\-]+@ump\\.ac\\.ma$",
            //                              ↑ domaine fixe : @ump.ac.ma
            message = "Vous devez utiliser un email académique (@ump.ac.ma)"
    )

    private String email;

    @NotBlank(message = "Le mot de passe est obligatoire")
    @Pattern(
            regexp = "^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%^&*()_+\\-=]).{8,}$",
            message = "Min 8 caractères, 1 majuscule, 1 chiffre, 1 caractère spécial"
    )
    private String password;

    @NotBlank(message = "La confirmation est obligatoire")
    private String confirmPassword;
}