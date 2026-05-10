package com.smartest.backend.dto.request;

import jakarta.validation.constraints.NotNull;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

/**
 * Synchronise uniquement les questions d’un quiz prof (ex. copie « miroir » QR), sans publication web ni emails.
 */
@Data
@NoArgsConstructor
@AllArgsConstructor
public class SyncQuestionsProfRequest {
    @NotNull
    private List<PublicationWebQuestionRequest> questions;
}
