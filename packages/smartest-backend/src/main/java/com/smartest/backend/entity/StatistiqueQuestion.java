package com.smartest.backend.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Table(name = "statistique_question")
@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class StatistiqueQuestion {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne
    @JoinColumn(name = "question_id")
    private Question question;

    @ManyToOne
    @JoinColumn(name = "quiz_id")
    private Quiz quiz;

    @ManyToOne
    @JoinColumn(name = "examen_id")
    private Examen examen;

    @ManyToOne
    @JoinColumn(name = "session_examen_id")
    private SessionExamen sessionExamen;

    private Integer nombreReponses;

    private Integer nombreCorrectes;

    private Integer nombreIncorrectes;

    private Double pourcentageReussite;

    private Double pourcentageEchec;

    private Boolean aGenereAlerte;
    private Boolean alerteEchec;  // ← Champ


    @Column(name = "dernier_calcul")
    private LocalDateTime dernierCalcul;

    @PreUpdate
    @PrePersist
    protected void onUpdate() {
        dernierCalcul = LocalDateTime.now();
    }

    public boolean getAlerteEchec() {
        return alerteEchec;
    }
}