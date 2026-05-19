package com.smartest.backend.security;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.SignatureAlgorithm;
import io.jsonwebtoken.security.Keys;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.security.Key;
import java.util.Date;
import java.util.Locale;
import java.util.Optional;

@Slf4j
@Component
public class JwtUtil {

    @Value("${app.jwt.secret}")
    private String secret;

    @Value("${app.jwt.expiration}")
    private long expiration;

    private Key getSigningKey() {
        return Keys.hmacShaKeyFor(secret.getBytes());
    }

    public String generateToken(String email, String role) {
        return generateToken(email, role, null);
    }

    /**
     * @param userId identifiant base (prof ou étudiant), exposé en claim {@code userId} pour le client web
     *               (ex. passage examen : paramètre {@code etudiantId} aligné sur le JWT).
     */
    public String generateToken(String email, String role, Long userId) {
        String subject = email == null ? "" : email.trim().toLowerCase(Locale.ROOT);
        var builder = Jwts.builder()
                .setSubject(subject)
                .claim("role", role)
                .setIssuedAt(new Date())
                .setExpiration(new Date(System.currentTimeMillis() + expiration));
        if (userId != null) {
            builder.claim("userId", userId);
        }
        return builder.signWith(getSigningKey(), SignatureAlgorithm.HS256).compact();
    }

    public String extractEmail(String token) {
        return extractAllClaims(token).getSubject();
    }

    public String extractRole(String token) {
        return (String) extractAllClaims(token).get("role");
    }

    /**
     * Extrait email / rôle / userId d'un JWT encore signé correctement, même expiré
     * (renouvellement de session sans ressaisir le mot de passe).
     */
    public Optional<ParsedToken> parseForRefresh(String token) {
        if (token == null || token.isBlank()) {
            return Optional.empty();
        }
        try {
            return Optional.of(toParsed(extractAllClaims(token)));
        } catch (ExpiredJwtException e) {
            return Optional.of(toParsed(e.getClaims()));
        } catch (JwtException e) {
            log.debug("Refresh refusé : {}", e.getMessage());
            return Optional.empty();
        }
    }

    private static ParsedToken toParsed(Claims claims) {
        String email = claims.getSubject();
        String role = (String) claims.get("role");
        Long userId = null;
        Object uid = claims.get("userId");
        if (uid instanceof Number n) {
            userId = n.longValue();
        } else if (uid != null) {
            try {
                userId = Long.parseLong(uid.toString());
            } catch (NumberFormatException ignored) {
                /* pas d'id */
            }
        }
        return new ParsedToken(email, role, userId);
    }

    public record ParsedToken(String email, String role, Long userId) {}

    public boolean validateToken(String token) {
        // ✅ Vérification null/empty
        if (token == null || token.isEmpty()) {
            return false;
        }
        try {
            extractAllClaims(token);
            return true;
        } catch (ExpiredJwtException e) {
            log.debug("Token expiré : {}", e.getMessage());
            return false;
        } catch (JwtException e) {
            log.debug("Token invalide : {}", e.getMessage());
            return false;
        }
    }

    private Claims extractAllClaims(String token) {
        return Jwts.parserBuilder()
                .setSigningKey(getSigningKey())
                .build()
                .parseClaimsJws(token)
                .getBody();
    }
}