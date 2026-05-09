package com.smartest.backend.security;

import io.jsonwebtoken.JwtException;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.http.MediaType;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.security.web.authentication.WebAuthenticationDetailsSource;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.nio.charset.StandardCharsets;

@Component
public class JwtAuthFilter extends OncePerRequestFilter {

    private static final String JSON_UNAUTHORIZED = "{\"error\":\"Token invalide ou expiré\"}";

    private final JwtUtil jwtUtil;
    private final UserDetailsServiceImpl userDetailsService;

    public JwtAuthFilter(JwtUtil jwtUtil,
                         UserDetailsServiceImpl userDetailsService) {
        this.jwtUtil = jwtUtil;
        this.userDetailsService = userDetailsService;
    }

    @Override
    protected boolean shouldNotFilter(HttpServletRequest request) {
        String path = request.getServletPath();
        return path.equals("/auth/register")
                || path.equals("/auth/register/etudiant")
                || path.equals("/auth/login")
                || path.equals("/auth/verify-email")
                || path.equals("/auth/verify-email/code")
                || path.equals("/auth/verify-email/resend")
                || path.equals("/auth/verify-email/resend/etudiant")
                || path.equals("/auth/forgot-password/etudiant")
                || path.equals("/auth/forgot-password/professeur")
                || path.equals("/auth/reset-password/etudiant")
                || path.equals("/auth/reset-password/professeur");
    }

    @Override
    protected void doFilterInternal(HttpServletRequest request,
                                    HttpServletResponse response,
                                    FilterChain filterChain)
            throws ServletException, IOException {

        String header = request.getHeader("Authorization");

        if (header != null && header.startsWith("Bearer ")) {
            String token = header.substring(7).trim();
            if (token.isEmpty()) {
                ecrire401(response);
                return;
            }

            String email;
            try {
                email = jwtUtil.extractEmail(token);
            } catch (JwtException ex) {
                logger.warn("JWT rejeté : " + ex.getMessage());
                ecrire401(response);
                return;
            }

            if (!jwtUtil.validateToken(token)) {
                logger.warn("JWT non valide après extraction (expiré ou signature incorrecte)");
                ecrire401(response);
                return;
            }

            if (email != null && SecurityContextHolder.getContext().getAuthentication() == null) {
                final UserDetails userDetails;
                try {
                    userDetails = userDetailsService.loadUserByUsername(email);
                } catch (UsernameNotFoundException ex) {
                    logger.warn("Utilisateur JWT inconnu : " + email);
                    ecrire401(response);
                    return;
                }

                UsernamePasswordAuthenticationToken authentication =
                        new UsernamePasswordAuthenticationToken(
                                userDetails,
                                null,
                                userDetails.getAuthorities()
                        );

                authentication.setDetails(
                        new WebAuthenticationDetailsSource()
                                .buildDetails(request)
                );

                SecurityContextHolder.getContext()
                        .setAuthentication(authentication);
            }
        }

        filterChain.doFilter(request, response);
    }

    private static void ecrire401(HttpServletResponse response) throws IOException {
        response.setStatus(HttpServletResponse.SC_UNAUTHORIZED);
        response.setCharacterEncoding(StandardCharsets.UTF_8.name());
        response.setContentType(MediaType.APPLICATION_JSON_VALUE);
        response.getWriter().write(JSON_UNAUTHORIZED);
    }
}
