package com.smartest.backend.service;

import com.smartest.backend.exception.EmailSendException;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.mail.MailException;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.stereotype.Service;

@Service
public class EmailService {
    private static final String BONJOUR = "Bonjour,\n\n";
    private static final String SIGNATURE = "L'équipe SmarTest";

    private final JavaMailSender mailSender;

    @Value("${spring.mail.username}")
    private String fromEmail;

    @Value("${app.web.dashboard-url:http://localhost:5173/dashboard}")
    private String webDashboardUrl;

    public EmailService(JavaMailSender mailSender) {
        this.mailSender = mailSender;
    }

    public void sendVerificationEmail(String toEmail, String token, String role) {
        String link = "http://localhost:8081/auth/verify-email?token=" + token + "&role=" + role;

        SimpleMailMessage message = new SimpleMailMessage();
        message.setFrom(fromEmail);
        message.setTo(toEmail);
        message.setSubject("SmarTest — Confirmez votre email");
        message.setText(
                BONJOUR +
                        "Merci de vous être inscrit sur SmarTest.\n\n" +
                        "Cliquez sur le lien ci-dessous pour confirmer votre email :\n\n" +
                        link + "\n\n" +
                        "Ce lien expire dans 24h.\n\n" +
                        SIGNATURE
        );
        envoyer(message);
    }

    public void sendVerificationCode(String email, String code) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setFrom(fromEmail);
        message.setTo(email);
        message.setSubject("SmarTest — Code de vérification");
        message.setText(
                BONJOUR +
                        "Votre code de vérification est : " + code + "\n\n" +
                        "Ce code expire dans 15 minutes.\n\n" +
                        SIGNATURE
        );
        envoyer(message);
    }


    public void sendResetPasswordEmail(String toEmail, String token, String role) {

        SimpleMailMessage message = new SimpleMailMessage();
        message.setFrom(fromEmail);
        message.setTo(toEmail);
        message.setSubject("SmarTest — Réinitialisation de votre mot de passe");

        if ("PROFESSEUR".equals(role)) {
            message.setText(
                    BONJOUR +
                            "Vous avez demandé à réinitialiser votre mot de passe.\n\n" +
                            "Votre code de réinitialisation (valable 15 minutes) :\n\n" +
                            "━━━━━━━━━━━━━━━━━━━━━━\n" +
                            token + "\n" +
                            "━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                            "Copiez ce code dans l'application SmarTest Desktop.\n\n" +
                            "Si vous n'avez pas fait cette demande, ignorez cet email.\n\n" +
                            SIGNATURE
            );
        } else {
            String link = "http://localhost:5173/reset-password?token=" + token;
            message.setText(
                    BONJOUR +
                            "Vous avez demandé à réinitialiser votre mot de passe.\n\n" +
                            "Cliquez sur le lien ci-dessous (valable 15 minutes) :\n\n" +
                            link + "\n\n" +
                            "Si vous n'avez pas fait cette demande, ignorez cet email.\n\n" +
                            SIGNATURE
            );
        }

        envoyer(message);
    }

    /**
     * Notifie un étudiant autorisé qu’un quiz vient d’être publié sur le web.
     */
    public void sendQuizWebPublishedEmail(String toEmail, String professeurNom, String quizTitre) {
        String nom = professeurNom != null && !professeurNom.isBlank() ? professeurNom.trim() : "Votre professeur";
        String titre = quizTitre != null && !quizTitre.isBlank() ? quizTitre.trim() : "Quiz";

        SimpleMailMessage message = new SimpleMailMessage();
        message.setFrom(fromEmail);
        message.setTo(toEmail);
        message.setSubject("SmarTest — Nouveau quiz publié");
        message.setText(
                BONJOUR +
                        "Le professeur " + nom + " a publié un nouveau quiz : « " + titre + " ».\n\n" +
                        "Connectez-vous à votre espace pour le passer :\n" +
                        webDashboardUrl + "\n\n" +
                        "— L'équipe SmarTest"
        );
        envoyer(message);
    }

    private void envoyer(SimpleMailMessage message) {
        try {
            mailSender.send(message);
        } catch (MailException ex) {
            throw new EmailSendException("Échec de l'envoi de l'email (service de messagerie indisponible).", ex);
        }
    }
}
