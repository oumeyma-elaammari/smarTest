package com.smartest.backend;

import com.smartest.backend.service.EmailService;
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.transaction.annotation.Transactional;

@SpringBootTest
@ActiveProfiles("test")
@Transactional
class BackendApplicationTests {

    @MockitoBean
    private EmailService emailService;

    @Test
    void contextLoads() {
        // GIVEN / WHEN / THEN — contexte Spring + H2 (profil test) démarre sans erreur
    }
}
