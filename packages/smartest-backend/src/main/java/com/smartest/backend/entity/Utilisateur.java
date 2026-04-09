package com.smartest.backend.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.*;
import com.smartest.backend.entity.enumeration.Role;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "utilisateur")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Utilisateur {
    @Column(unique = true)
    private String resetPasswordToken;

    private LocalDateTime resetPasswordExpiry;

    @Column(nullable = false)
    private boolean emailVerifie = false;

    @Column(unique = true)
    private String tokenVerification;

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(length = 100)
    private String nom;

    @Column(unique = true, length = 150)
    private String email;

    @Column(nullable = false)
    private String password;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private Role role;

    @JsonIgnore
    @ToString.Exclude
    @OneToMany(mappedBy = "utilisateur", cascade = CascadeType.ALL)
    private List<Reponse> reponses = new ArrayList<>();

    @JsonIgnore
    @ToString.Exclude
    @OneToMany(mappedBy = "utilisateur", cascade = CascadeType.ALL)
    private List<Resultat> resultats = new ArrayList<>();
}