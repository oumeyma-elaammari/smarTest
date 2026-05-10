package com.smartest.backend.config;

import com.smartest.backend.security.StompJwtChannelInterceptor;
import org.springframework.beans.factory.SmartInitializingSingleton;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.messaging.support.AbstractSubscribableChannel;
import org.springframework.messaging.support.ChannelInterceptor;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 * Force {@link StompJwtChannelInterceptor} en première position sur {@code clientInboundChannel}.
 * <p>
 * Les intercepteurs ne sont pas triés via {@link org.springframework.core.Ordered} par Spring Messaging ;
 * si Spring Security enregistre les siens avant {@link WebSocketConfig}, le JWT sur SEND arrive après
 * {@link org.springframework.security.messaging.access.intercept.AuthorizationChannelInterceptor},
 * ce qui provoque l'échec « Failed to send message … clientInboundChannel » côté STOMP.
 */
@Component
@Order(Ordered.LOWEST_PRECEDENCE)
public final class StompJwtFirstInboundChannelCustomizer implements SmartInitializingSingleton {

    private final AbstractSubscribableChannel clientInboundChannel;
    private final StompJwtChannelInterceptor stompJwtChannelInterceptor;

    public StompJwtFirstInboundChannelCustomizer(
            @Qualifier("clientInboundChannel") AbstractSubscribableChannel clientInboundChannel,
            StompJwtChannelInterceptor stompJwtChannelInterceptor) {
        this.clientInboundChannel = clientInboundChannel;
        this.stompJwtChannelInterceptor = stompJwtChannelInterceptor;
    }

    @Override
    public void afterSingletonsInstantiated() {
        // Ne pas utiliser uniquement removeInterceptor(beanInjecté) : la liste peut contenir une autre
        // référence (proxy ou ordre de création), auquel cas le JWT restait en fin de chaîne.
        List<ChannelInterceptor> current = clientInboundChannel.getInterceptors();
        StompJwtChannelInterceptor found = null;
        for (ChannelInterceptor ch : current) {
            if (ch instanceof StompJwtChannelInterceptor jwt) {
                found = jwt;
                break;
            }
        }
        if (found != null) {
            clientInboundChannel.removeInterceptor(found);
            clientInboundChannel.addInterceptor(0, found);
        } else {
            clientInboundChannel.addInterceptor(0, stompJwtChannelInterceptor);
        }
    }
}
