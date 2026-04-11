package com.smartest.backend.security;

import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import org.springframework.security.core.userdetails.*;
import org.springframework.stereotype.Service;

@Service
public class UserDetailsServiceImpl implements UserDetailsService {

    private final ProfesseurRepository professeurRepository;
    private final EtudiantRepository etudiantRepository;

    public UserDetailsServiceImpl(
            ProfesseurRepository professeurRepository,
            EtudiantRepository etudiantRepository) {
        this.professeurRepository = professeurRepository;
        this.etudiantRepository = etudiantRepository;
    }

    @Override
    public UserDetails loadUserByUsername(String email)
            throws UsernameNotFoundException {

        // Cherche dans Professeur
        var prof = professeurRepository.findByEmail(email);
        if (prof.isPresent()) {
            return User.builder()
                    .username(prof.get().getEmail())
                    .password(prof.get().getPassword())
                    .roles("PROFESSEUR")
                    .build();
        }

        // Cherche dans Etudiant
        var etudiant = etudiantRepository.findByEmail(email);
        if (etudiant.isPresent()) {
            return User.builder()
                    .username(etudiant.get().getEmail())
                    .password(etudiant.get().getPassword())
                    .roles("ETUDIANT")
                    .build();
        }

        throw new UsernameNotFoundException(
                "Utilisateur introuvable : " + email);
    }
}