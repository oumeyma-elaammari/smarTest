package com.smartest.backend.dto.request;

import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.LinkedHashMap;
import java.util.Map;

@Getter
@Setter
@NoArgsConstructor
public class ValiderCorrectionsDetailRequest {

    /**
     * Notes finales par question (clé = id question sous forme de chaîne JSON).
     */
    private Map<String, Double> notesFinales = new LinkedHashMap<>();

    private Double noteTotale;

    private String remarque;
}
