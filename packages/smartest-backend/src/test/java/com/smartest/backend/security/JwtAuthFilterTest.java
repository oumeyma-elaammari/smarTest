package com.smartest.backend.security;

import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.MalformedJwtException;
import jakarta.servlet.FilterChain;
import jakarta.servlet.http.HttpServletResponse;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.junit.jupiter.api.AfterEach;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.mock.web.MockHttpServletRequest;
import org.springframework.mock.web.MockHttpServletResponse;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;

import java.nio.charset.StandardCharsets;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
@DisplayName("JwtAuthFilter — Tests unitaires")
class JwtAuthFilterTest {
    private static final String TEST_EMAIL = "u@test.com";
    private static final String AUTHORIZATION = "Authorization";
    private static final String BEARER_PREFIX = "Bearer ";

    @Mock
    private JwtUtil jwtUtil;

    @Mock
    private UserDetailsServiceImpl userDetailsService;

    @Mock
    private FilterChain filterChain;

    private JwtAuthFilter filter;

    @BeforeEach
    void setUp() {
        filter = new JwtAuthFilter(jwtUtil, userDetailsService);
        SecurityContextHolder.clearContext();
    }

    @AfterEach
    void tearDown() {
        SecurityContextHolder.clearContext();
    }

    @Test
    @DisplayName("Sans header Authorization → chaîne poursuivie, pas d'authentification")
    void sansHeaderPasseLeFiltre() throws Exception {
        MockHttpServletRequest request = new MockHttpServletRequest();
        MockHttpServletResponse response = new MockHttpServletResponse();

        filter.doFilterInternal(request, response, filterChain);

        verify(filterChain).doFilter(request, response);
        assertThat(SecurityContextHolder.getContext().getAuthentication()).isNull();
        assertThat(response.getStatus()).isEqualTo(HttpServletResponse.SC_OK);
    }

    @Test
    @DisplayName("Token valide → SecurityContext authentifié")
    void tokenValideAuthentifie() throws Exception {
        String token = "signed.jwt.token";
        UserDetails ud = new User(TEST_EMAIL, "pw", List.of());

        when(jwtUtil.extractEmail(token)).thenReturn(TEST_EMAIL);
        when(jwtUtil.validateToken(token)).thenReturn(true);
        when(userDetailsService.loadUserByUsername(TEST_EMAIL)).thenReturn(ud);

        MockHttpServletRequest request = new MockHttpServletRequest();
        request.addHeader(AUTHORIZATION, BEARER_PREFIX + token);
        MockHttpServletResponse response = new MockHttpServletResponse();

        filter.doFilterInternal(request, response, filterChain);

        verify(filterChain).doFilter(request, response);
        assertThat(SecurityContextHolder.getContext().getAuthentication()).isNotNull();
        assertThat(SecurityContextHolder.getContext().getAuthentication().getName()).isEqualTo(TEST_EMAIL);
    }

    @Test
    @DisplayName("Token expiré → 401 JSON")
    void tokenExpire401() throws Exception {
        String token = "expired";
        when(jwtUtil.extractEmail(token)).thenThrow(
                new ExpiredJwtException(null, null, "JWT expired"));

        MockHttpServletRequest request = new MockHttpServletRequest();
        request.addHeader(AUTHORIZATION, BEARER_PREFIX + token);
        MockHttpServletResponse response = new MockHttpServletResponse();

        filter.doFilterInternal(request, response, filterChain);

        assertThat(response.getStatus()).isEqualTo(401);
        assertThat(response.getContentAsString(StandardCharsets.UTF_8))
                .contains("Token invalide ou expiré");
    }

    @Test
    @DisplayName("Token malformé → 401 JSON")
    void tokenMalforme401() throws Exception {
        String token = "@@@";
        when(jwtUtil.extractEmail(token)).thenThrow(new MalformedJwtException("bad"));

        MockHttpServletRequest request = new MockHttpServletRequest();
        request.addHeader(AUTHORIZATION, BEARER_PREFIX + token);
        MockHttpServletResponse response = new MockHttpServletResponse();

        filter.doFilterInternal(request, response, filterChain);

        assertThat(response.getStatus()).isEqualTo(401);
        assertThat(response.getContentAsString(StandardCharsets.UTF_8))
                .contains("Token invalide ou expiré");
    }

    @Test
    @DisplayName("Route /auth/login → shouldNotFilter true")
    void routeAuthLoginNeAppliquePasLeFiltre() {
        MockHttpServletRequest request = new MockHttpServletRequest();
        request.setServletPath("/auth/login");

        assertThat(filter.shouldNotFilter(request)).isTrue();
    }
}
