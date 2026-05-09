package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import com.smartest.backend.entity.enumeration.StatutExamen;
import jakarta.persistence.*;
import lombok.*;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

@Entity
@Getter @Setter
@NoArgsConstructor @AllArgsConstructor
public class ExamenPublie {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String titre;
    private Integer duree;

    @Column(columnDefinition = "TEXT")
    private String description;

    @ManyToOne
    @JoinColumn(name = "professeur_id")
    private Professeur professeur;

    @Enumerated(EnumType.STRING)
    @Column(length = 32)
    private StatutExamen statut;

    private LocalDateTime dateDebut;
    private LocalDateTime dateFin;
    private LocalDateTime dateCreation;

    /**
     * Renseigné lorsque le professeur a validé la publication web (liste d’emails) depuis le bureau.
     * Tant que ce champ est nul, l’examen ne doit pas apparaître côté étudiant web.
     */
    private LocalDateTime publieSurWebLe;

    /**
     * Emails autorisés pour rejoindre l'examen sur le web (comme pour les quiz).
     * Non sérialisé en JSON : évite LazyInitializationException sur les endpoints qui renvoient l’entité hors session.
     */
    @JsonIgnore
    @ElementCollection(fetch = FetchType.LAZY)
    @CollectionTable(name = "examen_email_web_autorise", joinColumns = @JoinColumn(name = "examen_id"))
    @Column(name = "email", nullable = false, length = 320)
    private Set<String> emailsAutorisesWeb = new LinkedHashSet<>();

    // ✅ Relation avec Question
    @ManyToMany
    @JoinTable(
            name = "examen_publie_question",
            joinColumns = @JoinColumn(name = "examen_publie_id"),
            inverseJoinColumns = @JoinColumn(name = "question_id")
    )
    private List<Question> questions = new ArrayList<>();
}
