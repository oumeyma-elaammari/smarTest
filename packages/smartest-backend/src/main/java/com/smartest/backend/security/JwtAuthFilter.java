package com.smartest.backend.security;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.web.authentication.WebAuthenticationDetailsSource;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

@Component
public class JwtAuthFilter extends OncePerRequestFilter {
    // ↑ OncePerRequestFilter garantit que le filtre
    //   s'exécute UNE SEULE fois par requête HTTP

    private final JwtUtil jwtUtil;
    private final UserDetailsServiceImpl userDetailsService;

    public JwtAuthFilter(JwtUtil jwtUtil,
                         UserDetailsServiceImpl userDetailsService) {
        this.jwtUtil = jwtUtil;
        this.userDetailsService = userDetailsService;
    }

    @Override
    protected void doFilterInternal(HttpServletRequest request,
                                    HttpServletResponse response,
                                    FilterChain filterChain)
            throws ServletException, IOException {

        // ── ÉTAPE 1 : lire le header Authorization ────────────
        String header = request.getHeader("Authorization");
        // ↑ format attendu : "Bearer eyJhbGci..."

        String token = null;
        String email = null;

        if (header != null && header.startsWith("Bearer ")) {
            token = header.substring(7);
            // ↑ on retire "Bearer " pour garder juste le token

            try {
                email = jwtUtil.extractEmail(token);
                // ↑ on extrait l'email depuis le token
            } catch (Exception e) {
                // token malformé → on ignore et on continue
                // Spring Security bloquera la requête après
            }
        }

        // ── ÉTAPE 2 : valider et authentifier ─────────────────
        if (email != null &&
                SecurityContextHolder.getContext().getAuthentication() == null) {
            // ↑ on vérifie qu'il n'y a pas déjà une auth en cours

            UserDetails userDetails =
                    userDetailsService.loadUserByUsername(email);

            if (jwtUtil.validateToken(token)) {
                // ↑ token valide et non expiré ?

                UsernamePasswordAuthenticationToken authentication =
                        new UsernamePasswordAuthenticationToken(
                                userDetails,
                                null,
                                userDetails.getAuthorities()
                                // ↑ les rôles : ROLE_PROFESSEUR ou ROLE_ETUDIANT
                        );

                authentication.setDetails(
                        new WebAuthenticationDetailsSource()
                                .buildDetails(request)
                );

                SecurityContextHolder.getContext()
                        .setAuthentication(authentication);
                // ↑ Spring sait maintenant qui fait la requête
            }
        }

        // ── ÉTAPE 3 : passer à la suite ───────────────────────
        filterChain.doFilter(request, response);
        // ↑ on continue la chaîne de filtres Spring
    }
}