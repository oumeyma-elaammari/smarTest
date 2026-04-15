package com.smartest.backend.service;

import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.*;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.test.util.ReflectionTestUtils;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("EmailService — Tests complets")
class EmailServiceTest {

    @Mock private JavaMailSender mailSender;

    @InjectMocks private EmailService emailService;

    @Captor private ArgumentCaptor<SimpleMailMessage> messageCaptor;

    @BeforeEach
    void setUp() {
        ReflectionTestUtils.setField(emailService, "fromEmail", "smarttest320@gmail.com");
    }

    // ══════════════════════════════════════════════════════
    //  SEND VERIFICATION EMAIL
    // ══════════════════════════════════════════════════════
    @Nested
    @DisplayName("sendVerificationEmail")
    class SendVerificationEmailTests {

        @Test
        @DisplayName("✅ Email envoyé à l'étudiant")
        void sendVerificationEmail_Etudiant() {
            emailService.sendVerificationEmail("nissrine@ump.ac.ma", "token123", "ETUDIANT");

            verify(mailSender).send(messageCaptor.capture());
            SimpleMailMessage msg = messageCaptor.getValue();

            assertThat(msg.getTo()).contains("nissrine@ump.ac.ma");
            assertThat(msg.getSubject()).isEqualTo("SmarTest — Confirmez votre email");
            assertThat(msg.getText()).contains("token123");
            assertThat(msg.getText()).contains("ETUDIANT");
            assertThat(msg.getText()).contains("http://localhost:8081/auth/verify-email");
        }

        @Test
        @DisplayName("✅ Email envoyé au professeur")
        void sendVerificationEmail_Professeur() {
            emailService.sendVerificationEmail("ikram@ensa.ma", "tokenProf", "PROFESSEUR");

            verify(mailSender).send(messageCaptor.capture());
            SimpleMailMessage msg = messageCaptor.getValue();

            assertThat(msg.getTo()).contains("ikram@ensa.ma");
            assertThat(msg.getText()).contains("PROFESSEUR");
            assertThat(msg.getText()).contains("tokenProf");
        }

        @Test
        @DisplayName("✅ From email correct")
        void sendVerificationEmail_FromEmailCorrect() {
            emailService.sendVerificationEmail("nissrine@ump.ac.ma", "token123", "ETUDIANT");

            verify(mailSender).send(messageCaptor.capture());
            assertThat(messageCaptor.getValue().getFrom())
                .isEqualTo("smarttest320@gmail.com");
        }

        @Test
        @DisplayName("✅ Lien contient le token et le role")
        void sendVerificationEmail_LinkContainsTokenAndRole() {
            emailService.sendVerificationEmail("nissrine@ump.ac.ma", "abc-123", "ETUDIANT");

            verify(mailSender).send(messageCaptor.capture());
            String text = messageCaptor.getValue().getText();

            assertThat(text).contains("token=abc-123");
            assertThat(text).contains("role=ETUDIANT");
        }

        @Test
        @DisplayName("✅ mailSender.send() appelé exactement une fois")
        void sendVerificationEmail_SendCalledOnce() {
            emailService.sendVerificationEmail("nissrine@ump.ac.ma", "token123", "ETUDIANT");
            verify(mailSender, times(1)).send(any(SimpleMailMessage.class));
        }
    }

    // ══════════════════════════════════════════════════════
    //  SEND RESET PASSWORD EMAIL
    // ══════════════════════════════════════════════════════

    @Nested
    @DisplayName("sendResetPasswordEmail")
    class SendResetPasswordEmailTests {

        @Test
        @DisplayName("✅ Email de reset envoyé — Etudiant")
        void sendResetPasswordEmail_Success() {
            // ✅ Ajoutez "ETUDIANT"
            emailService.sendResetPasswordEmail("nissrine@ump.ac.ma", "reset-token-123", "ETUDIANT");

            verify(mailSender).send(messageCaptor.capture());
            SimpleMailMessage msg = messageCaptor.getValue();

            assertThat(msg.getTo()).contains("nissrine@ump.ac.ma");
            assertThat(msg.getSubject())
                    .isEqualTo("SmarTest — Réinitialisation de votre mot de passe");
            assertThat(msg.getText()).contains("reset-token-123");
            assertThat(msg.getText()).contains("http://localhost:5173/reset-password");
        }

        @Test
        @DisplayName("✅ Lien contient le token — Etudiant")
        void sendResetPasswordEmail_LinkContainsToken() {
            // ✅ Ajoutez "ETUDIANT"
            emailService.sendResetPasswordEmail("nissrine@ump.ac.ma", "my-reset-token", "ETUDIANT");

            verify(mailSender).send(messageCaptor.capture());
            assertThat(messageCaptor.getValue().getText())
                    .contains("my-reset-token");
        }

        @Test
        @DisplayName("✅ From email correct")
        void sendResetPasswordEmail_FromEmailCorrect() {
            // ✅ Ajoutez "PROFESSEUR"
            emailService.sendResetPasswordEmail("ikram@ensa.ma", "reset-token", "PROFESSEUR");

            verify(mailSender).send(messageCaptor.capture());
            assertThat(messageCaptor.getValue().getFrom())
                    .isEqualTo("smarttest320@gmail.com");
        }

        @Test
        @DisplayName("✅ mailSender.send() appelé exactement une fois")
        void sendResetPasswordEmail_SendCalledOnce() {
            // ✅ Ajoutez "ETUDIANT"
            emailService.sendResetPasswordEmail("ikram@ensa.ma", "reset-token", "ETUDIANT");
            verify(mailSender, times(1)).send(any(SimpleMailMessage.class));
        }

        @Test
        @DisplayName("✅ Message mentionne 15 minutes — Etudiant")
        void sendResetPasswordEmail_Mentions15Minutes() {
            // ✅ Ajoutez "ETUDIANT"
            emailService.sendResetPasswordEmail("ikram@ensa.ma", "reset-token", "ETUDIANT");

            verify(mailSender).send(messageCaptor.capture());
            assertThat(messageCaptor.getValue().getText()).contains("15 minutes");
        }

        @Test
        @DisplayName("✅ Professeur reçoit le token directement")
        void sendResetPasswordEmail_ProfesseurReceivesToken() {
            // ✅ Test spécifique pour le professeur
            emailService.sendResetPasswordEmail("ikram@ensa.ma", "prof-token-123", "PROFESSEUR");

            verify(mailSender).send(messageCaptor.capture());
            SimpleMailMessage msg = messageCaptor.getValue();

            assertThat(msg.getText()).contains("prof-token-123");
            // Le prof reçoit le token directement, pas un lien web
            assertThat(msg.getText()).doesNotContain("localhost:5173");
        }
    }
}
