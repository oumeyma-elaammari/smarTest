package com.smartest.backend.security;

import com.smartest.backend.entity.Etudiant;
import com.smartest.backend.entity.Professeur;
import com.smartest.backend.repository.EtudiantRepository;
import com.smartest.backend.repository.ProfesseurRepository;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UsernameNotFoundException;

import java.util.Optional;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("UserDetailsServiceImpl — Tests complets")
class UserDetailsServiceImplTest {

    @Mock private ProfesseurRepository professeurRepository;
    @Mock private EtudiantRepository etudiantRepository;

    @InjectMocks private UserDetailsServiceImpl userDetailsService;

    private Professeur professeur;
    private Etudiant etudiant;

    @BeforeEach
    void setUp() {
        professeur = new Professeur();
        professeur.setEmail("ikram@ensa.ma");
        professeur.setPassword("hashedPassword");

        etudiant = new Etudiant();
        etudiant.setEmail("nissrine@ump.ac.ma");
        etudiant.setPassword("hashedPassword");
    }

    @Test
    @DisplayName("✅ Charge un professeur correctement")
    void loadUserByUsername_Professeur() {
        when(professeurRepository.findByEmail("ikram@ensa.ma"))
            .thenReturn(Optional.of(professeur));

        UserDetails userDetails = userDetailsService.loadUserByUsername("ikram@ensa.ma");

        assertThat(userDetails.getUsername()).isEqualTo("ikram@ensa.ma");
        assertThat(userDetails.getPassword()).isEqualTo("hashedPassword");
        assertThat(userDetails.getAuthorities())
            .anyMatch(a -> a.getAuthority().equals("ROLE_PROFESSEUR"));
    }

    @Test
    @DisplayName("✅ Charge un étudiant correctement")
    void loadUserByUsername_Etudiant() {
        when(professeurRepository.findByEmail("nissrine@ump.ac.ma"))
            .thenReturn(Optional.empty());
        when(etudiantRepository.findByEmail("nissrine@ump.ac.ma"))
            .thenReturn(Optional.of(etudiant));

        UserDetails userDetails = userDetailsService.loadUserByUsername("nissrine@ump.ac.ma");

        assertThat(userDetails.getUsername()).isEqualTo("nissrine@ump.ac.ma");
        assertThat(userDetails.getPassword()).isEqualTo("hashedPassword");
        assertThat(userDetails.getAuthorities())
            .anyMatch(a -> a.getAuthority().equals("ROLE_ETUDIANT"));
    }

    @Test
    @DisplayName("❌ Lève UsernameNotFoundException si email inconnu")
    void loadUserByUsername_UserNotFound() {
        when(professeurRepository.findByEmail("inconnu@mail.com"))
            .thenReturn(Optional.empty());
        when(etudiantRepository.findByEmail("inconnu@mail.com"))
            .thenReturn(Optional.empty());

        assertThatThrownBy(() ->
            userDetailsService.loadUserByUsername("inconnu@mail.com")
        ).isInstanceOf(UsernameNotFoundException.class);
    }

    @Test
    @DisplayName("✅ Professeur trouvé avant l'étudiant")
    void loadUserByUsername_ProfesseurBeforeEtudiant() {
        when(professeurRepository.findByEmail("ikram@ensa.ma"))
            .thenReturn(Optional.of(professeur));

        userDetailsService.loadUserByUsername("ikram@ensa.ma");

        verify(etudiantRepository, never()).findByEmail(any());
    }
}
