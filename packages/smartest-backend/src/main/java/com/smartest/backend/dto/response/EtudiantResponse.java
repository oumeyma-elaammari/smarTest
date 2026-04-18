package com.smartest.backend.dto.response;

import lombok.*;

@Getter @Setter @NoArgsConstructor @AllArgsConstructor
public class EtudiantResponse {
    private Long   id;
    private String nom;
    private String email;
    private boolean emailVerifie;
}