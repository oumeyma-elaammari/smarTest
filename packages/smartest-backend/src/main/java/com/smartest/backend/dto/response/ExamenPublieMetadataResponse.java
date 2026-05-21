package com.smartest.backend.dto.response;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

/**
 * Métadonnées publiées sur le web sans contenu des questions (énoncés / réponses).
 */
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class ExamenPublieMetadataResponse {

    private Long id;
    private String titre;
    private String description;
    private LocalDateTime dateDebut;
    private LocalDateTime dateFin;
    private Integer duree;
    private String statut;
    /** Nombre de questions (information générale seulement). */
    private Integer totalQuestions;
    /** Barème par défaut affiché (le prof peut l’ajuster en supervision). */
    private Double bareme;
    private Boolean demarrageAutomatique;
    private String professeurNom;
    /** Renseigné pour l'étudiant lorsque la note est publiée (validation + synchro selon config). */
    private Double noteFinaleAffichee;
    private Double baremeNoteFinale;
}
