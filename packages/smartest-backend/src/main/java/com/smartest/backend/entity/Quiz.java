package com.smartest.backend.entity;

import com.smartest.backend.entity.enumeration.StatutQuiz;
import jakarta.persistence.*;
import lombok.*;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

@Entity
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Quiz {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String titre;

    // NOUVEAU
    @Enumerated(EnumType.STRING)
    private StatutQuiz statut;   // BROUILLON / PUBLIE

    // NOUVEAU
    private LocalDateTime datePublication;

    @ManyToOne
    private Professeur professeur;


    //@ManyToMany
    //private List<Question> questions;

    @ManyToMany
    @JoinTable(
            name = "quiz_question",
            joinColumns = @JoinColumn(name = "quiz_id"),
            inverseJoinColumns = @JoinColumn(name = "question_id")
    )
    private List<Question> questions = new ArrayList<>();

    /**
     * Emails autorisés à voir / passer ce quiz sur le web (publication web, hors QR).
     */
    @ElementCollection(fetch = FetchType.LAZY)
    @CollectionTable(name = "quiz_email_web_autorise", joinColumns = @JoinColumn(name = "quiz_id"))
    @Column(name = "email", nullable = false, length = 320)
    private Set<String> emailsAutorisesWeb = new LinkedHashSet<>();

}