package com.smartest.backend.testsupport;

import org.springframework.core.MethodParameter;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.test.web.servlet.request.RequestPostProcessor;
import org.springframework.web.bind.support.WebDataBinderFactory;
import org.springframework.web.context.request.NativeWebRequest;
import org.springframework.web.method.support.HandlerMethodArgumentResolver;
import org.springframework.web.method.support.ModelAndViewContainer;

/**
 * MockMvc en {@code standaloneSetup} : remplit {@link SecurityContextHolder} pour que
 * {@code @AuthenticationPrincipal UserDetails} soit résolu dans les contrôleurs.
 */
public final class MockMvcAuthenticationSupport {

    private MockMvcAuthenticationSupport() {
    }

    public static HandlerMethodArgumentResolver authenticationPrincipalUserDetailsResolver() {
        return new UserDetailsFromSecurityContextResolver();
    }

    /**
     * Associe un utilisateur au contexte de sécurité pour la requête simulée.
     */
    public static RequestPostProcessor principal(UserDetails userDetails) {
        return request -> {
            UsernamePasswordAuthenticationToken auth =
                    new UsernamePasswordAuthenticationToken(userDetails, null, userDetails.getAuthorities());
            SecurityContextHolder.getContext().setAuthentication(auth);
            return request;
        };
    }

    private static final class UserDetailsFromSecurityContextResolver implements HandlerMethodArgumentResolver {

        @Override
        public boolean supportsParameter(MethodParameter parameter) {
            return parameter.hasParameterAnnotation(AuthenticationPrincipal.class)
                    && UserDetails.class.isAssignableFrom(parameter.getParameterType());
        }

        @Override
        public Object resolveArgument(MethodParameter parameter, ModelAndViewContainer mavContainer,
                NativeWebRequest webRequest, WebDataBinderFactory binderFactory) {
            Authentication auth = SecurityContextHolder.getContext().getAuthentication();
            if (auth == null || auth.getPrincipal() == null) {
                return null;
            }
            Object p = auth.getPrincipal();
            return p instanceof UserDetails ud ? ud : null;
        }
    }
}
