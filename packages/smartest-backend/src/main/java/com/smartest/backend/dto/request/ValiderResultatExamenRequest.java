package com.smartest.backend.dto.request;

import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
public class ValiderResultatExamenRequest {

    private Long etudiantId;
    private Double noteFinale;
    private String remarque;
}
