package com.smartest.backend.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.messaging.Message;
import org.springframework.messaging.simp.SimpMessageType;
import org.springframework.messaging.support.ChannelInterceptor;
import org.springframework.security.authorization.AuthorizationManager;
import org.springframework.security.config.annotation.web.socket.EnableWebSocketSecurity;
import org.springframework.security.messaging.access.intercept.MessageMatcherDelegatingAuthorizationManager;

/**
 * Autorise les abonnements publics au flux QR éphémère ; le reste des topics reste authentifié.
 * <p>
 * Spring Security enregistre par défaut {@code XorCsrfChannelInterceptor} sur le canal entrant STOMP,
 * qui exige un jeton CSRF sur {@code CONNECT} (body vide → erreur « clientInboundChannel … content-length:0 »).
 * {@code HttpSecurity.csrf().disable()} ne s’applique pas à ce contrôle. Un bean nommé exactement
 * {@code csrfChannelInterceptor} remplace cet intercepteur (voir {@code WebSocketMessageBrokerSecurityConfiguration}).
 * Ici API stateless (JWT en en-tête), desktop sans cookie / même pas de CSRF STOMP.
 */
@Configuration
@EnableWebSocketSecurity
public class WebSocketSecurityConfig {

    /**
     * Remplace {@code XorCsrfChannelInterceptor} pour autoriser CONNECT STOMP sans en-tête CSRF
     * (clients hors navigateur, JWT Bearer uniquement).
     */
    @Bean(name = "csrfChannelInterceptor")
    ChannelInterceptor noopStompCsrfChannelInterceptor() {
        return new ChannelInterceptor() {};
    }

    @Bean
    AuthorizationManager<Message<?>> messageAuthorizationManager(
            MessageMatcherDelegatingAuthorizationManager.Builder messages) {
        messages
                .simpTypeMatchers(SimpMessageType.CONNECT, SimpMessageType.UNSUBSCRIBE,
                        SimpMessageType.DISCONNECT, SimpMessageType.HEARTBEAT)
                .permitAll()
                .simpDestMatchers("/app/quiz-qr/student/**").permitAll()
                .simpSubscribeDestMatchers("/topic/quiz-qr/**").permitAll()
                // Prof envoie JWT dans les en-têtes STOMP ; si l’ordre des intercepteurs RESTe inchangé,
                // authenticated() refuserait avant que StompJwtChannelInterceptor n’applique le Bearer.
                // L’accès reste contrôlé par Principal dans QuizWebSocketController + JWT sur la trame.
                .simpDestMatchers("/app/quiz-qr/prof/init").permitAll()
                .simpDestMatchers("/app/quiz-qr/prof/**").authenticated()
                .simpSubscribeDestMatchers("/topic/**").authenticated()
                .simpDestMatchers("/app/**").authenticated();
        return messages.build();
    }
}
