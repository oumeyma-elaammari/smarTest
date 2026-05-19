package com.smartest.backend.config;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;

@Getter
@Setter
@ConfigurationProperties(prefix = "examen.resultat")
public class ExamenResultatPublicationProperties {

    /**
     * Si {@code false} (défaut prod stricte), la note n'est visible côté étudiant web qu'après
     * {@code POST .../synchroniser-note-workbench}. Si {@code true}, la validation prof rend la note visible tout de suite
     * (utile lorsque la synchro Workbench est gérée hors API ou en développement).
     */
    private boolean publierNoteImmediatementApresValidation = true;
}
