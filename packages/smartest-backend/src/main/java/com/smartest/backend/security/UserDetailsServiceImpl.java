package com.smartest.backend.security;

import com.smartest.backend.entity.Professeur;
import com.smartest.backend.entity.Utilisateur;
import com.smartest.backend.repository.ProfesseurRepository;
import com.smartest.backend.repository.UtilisateurRepository;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.stereotype.Service;

@Service
public class UserDetailsServiceImpl implements UserDetailsService {

    private final ProfesseurRepository professeurRepository;
    private final UtilisateurRepository utilisateurRepository;

    public UserDetailsServiceImpl(ProfesseurRepository professeurRepository,
                                  UtilisateurRepository utilisateurRepository) {
        this.professeurRepository = professeurRepository;
        this.utilisateurRepository = utilisateurRepository;
    }

    @Override
    public UserDetails loadUserByUsername(String email)
            throws UsernameNotFoundException {

        // ── CAS 1 : chercher dans Professeur ──────────────────
        var professor = professeurRepository.findByEmail(email);
        if (professor.isPresent()) {
            Professeur p = professor.get();
            return User.withUsername(p.getEmail())
                    .password(p.getPassword())
                    // ↑ mot de passe déjà hashé en base
                    .roles(p.getRole().name())
                    // ↑ "PROFESSEUR" → Spring ajoute "ROLE_PROFESSEUR"
                    .build();
        }

        // ── CAS 2 : chercher dans Utilisateur (étudiant) ──────
        var student = utilisateurRepository.findByEmail(email);
        if (student.isPresent()) {
            Utilisateur u = student.get();
            return User.withUsername(u.getEmail())
                    .password(u.getPassword())
                    .roles(u.getRole().name())
                    // ↑ "ETUDIANT" → Spring ajoute "ROLE_ETUDIANT"
                    .build();
        }

        // ── CAS 3 : introuvable ───────────────────────────────
        throw new UsernameNotFoundException(
                "No account found with email: " + email);
    }
}