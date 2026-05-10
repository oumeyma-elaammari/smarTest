package com.smartest.backend.config;

import com.smartest.backend.security.StompJwtChannelInterceptor;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.messaging.simp.config.ChannelRegistration;
import org.springframework.messaging.simp.config.MessageBrokerRegistry;
import org.springframework.scheduling.concurrent.ThreadPoolTaskExecutor;
import org.springframework.web.socket.config.annotation.*;

@Configuration
@Order(Ordered.HIGHEST_PRECEDENCE)
@EnableWebSocketMessageBroker
public class WebSocketConfig implements WebSocketMessageBrokerConfigurer {

    private final StompJwtChannelInterceptor stompJwtChannelInterceptor;

    public WebSocketConfig(StompJwtChannelInterceptor stompJwtChannelInterceptor) {
        this.stompJwtChannelInterceptor = stompJwtChannelInterceptor;
    }

    @Override
    public void configureMessageBroker(MessageBrokerRegistry config) {
        config.enableSimpleBroker("/topic");
        config.setApplicationDestinationPrefixes("/app");
    }

    @Override
    public void registerStompEndpoints(StompEndpointRegistry registry) {
        registry.addEndpoint("/ws").setAllowedOrigins("*");
    }

    @Override
    public void configureClientInboundChannel(ChannelRegistration registration) {
        // Pool par défaut (multi-thread) : SecurityContextHolder / Principal STOMP ne suivent pas entre threads,
        // ce qui peut provoquer AccessDenied ou erreurs « Failed to send … clientInboundChannel » (corps vide).
        ThreadPoolTaskExecutor inbound = new ThreadPoolTaskExecutor();
        inbound.setCorePoolSize(1);
        inbound.setMaxPoolSize(1);
        inbound.setQueueCapacity(Integer.MAX_VALUE);
        inbound.setThreadNamePrefix("stomp-inbound-");
        inbound.initialize();
        registration.taskExecutor(inbound);
        registration.interceptors(stompJwtChannelInterceptor);
    }
}