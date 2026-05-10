package com.smartest.backend.security;

import io.jsonwebtoken.JwtException;
import lombok.RequiredArgsConstructor;
import org.springframework.lang.NonNull;
import org.springframework.messaging.Message;
import org.springframework.messaging.MessageChannel;
import org.springframework.messaging.simp.stomp.StompCommand;
import org.springframework.messaging.simp.stomp.StompHeaderAccessor;
import org.springframework.messaging.support.ChannelInterceptor;
import org.springframework.messaging.support.MessageHeaderAccessor;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 * Authentifie les trames STOMP CONNECT (et renforce SEND) via l’en-tête {@code Authorization: Bearer}.
 * Les élèves en session QR n’envoient pas JWT : ils restent anonymes pour les autres commandes.
 * <p>
 * Doit s’exécuter <b>avant</b> {@code AuthorizationChannelInterceptor} (Spring Security WebSocket) pour que
 * {@code SecurityContextHolder} soit renseigné lors du contrôle {@code authenticated()} sur {@code /app/...}.
 * L’ordre réel est forcé par {@link com.smartest.backend.config.StompJwtFirstInboundChannelCustomizer}.
 */
@Component
@RequiredArgsConstructor
public class StompJwtChannelInterceptor implements ChannelInterceptor {

    private final JwtUtil jwtUtil;
    private final UserDetailsServiceImpl userDetailsService;

    @Override
    public Message<?> preSend(@NonNull Message<?> message, @NonNull MessageChannel channel) {
        StompHeaderAccessor accessor = MessageHeaderAccessor.getAccessor(message, StompHeaderAccessor.class);
        if (accessor == null) {
            return message;
        }
        StompCommand cmd = accessor.getCommand();
        if (cmd != StompCommand.CONNECT && cmd != StompCommand.SEND) {
            return message;
        }

        List<String> authHeaders = accessor.getNativeHeader("Authorization");
        if (authHeaders == null || authHeaders.isEmpty()) {
            return message;
        }
        String raw = authHeaders.getFirst();
        if (raw == null || !raw.startsWith("Bearer ")) {
            return message;
        }
        String token = raw.substring(7).trim();
        if (token.isEmpty()) {
            return message;
        }

        try {
            String email = jwtUtil.extractEmail(token);
            if (!jwtUtil.validateToken(token)) {
                return message;
            }
            UserDetails userDetails = userDetailsService.loadUserByUsername(email);
            UsernamePasswordAuthenticationToken authentication =
                    new UsernamePasswordAuthenticationToken(
                            userDetails, null, userDetails.getAuthorities());
            SecurityContextHolder.getContext().setAuthentication(authentication);
            accessor.setUser(authentication);
        } catch (JwtException | UsernameNotFoundException ignored) {
            SecurityContextHolder.clearContext();
        }
        return message;
    }

    @Override
    public void afterSendCompletion(Message<?> message, MessageChannel channel, boolean sent, Exception ex) {
        SecurityContextHolder.clearContext();
    }
}
