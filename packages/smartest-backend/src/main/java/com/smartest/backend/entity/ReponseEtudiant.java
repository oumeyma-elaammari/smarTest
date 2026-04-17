package com.smartest.backend.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import java.time.LocalDateTime;

@Entity
@Table(name = "reponse_etudiant")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class ReponseEtudiant {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(columnDefinition = "TEXT")
    private String reponseTexte;

    private Boolean estCorrecte;

    private Boolean aRecuCorrectionImmediate;

    @Column(columnDefinition = "TEXT")
    private String bonneReponseAffichee;  // ← NOUVEAU : stocke la bonne réponse affichée

    private LocalDateTime dateSoumission;

    @ManyToOne
    @JoinColumn(name = "etudiant_id")
    private Etudiant etudiant;

    @ManyToOne
    @JoinColumn(name = "question_id")
    private Question question;

    @ManyToOne
    @JoinColumn(name = "resultat_id")
    private Resultat resultat;

    @ManyToOne
    @JoinColumn(name = "session_examen_id")
    private SessionExamen sessionExamen;

    @PrePersist
    protected void onCreate() {
        dateSoumission = LocalDateTime.now();
    }
}