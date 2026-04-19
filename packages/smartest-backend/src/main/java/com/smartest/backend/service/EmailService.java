package com.smartest.backend.service;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.stereotype.Service;

@Service
public class EmailService {

    private final JavaMailSender mailSender;

    @Value("${spring.mail.username}")
    private String fromEmail;

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
                "Bonjour,\n\n" +
                        "Merci de vous être inscrit sur SmarTest.\n\n" +
                        "Cliquez sur le lien ci-dessous pour confirmer votre email :\n\n" +
                        link + "\n\n" +
                        "Ce lien expire dans 24h.\n\n" +
                        "L'équipe SmarTest"
        );
        mailSender.send(message);
    }

    public void sendVerificationCode(String email, String code) {
        SimpleMailMessage message = new SimpleMailMessage();
        message.setTo(email);
        message.setSubject("SmarTest — Code de vérification");
        message.setText(
                "Bonjour,\n\n" +
                        "Votre code de vérification est : " + code + "\n\n" +
                        "Ce code expire dans 15 minutes.\n\n" +
                        "L'équipe SmarTest"
        );
        mailSender.send(message);
    }


    public void sendResetPasswordEmail(String toEmail, String token, String role) {

        SimpleMailMessage message = new SimpleMailMessage();
        message.setFrom(fromEmail);
        message.setTo(toEmail);
        message.setSubject("SmarTest — Réinitialisation de votre mot de passe");

        if ("PROFESSEUR".equals(role)) {
            message.setText(
                    "Bonjour,\n\n" +
                            "Vous avez demandé à réinitialiser votre mot de passe.\n\n" +
                            "Votre code de réinitialisation (valable 15 minutes) :\n\n" +
                            "━━━━━━━━━━━━━━━━━━━━━━\n" +
                            token + "\n" +
                            "━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                            "Copiez ce code dans l'application SmarTest Desktop.\n\n" +
                            "Si vous n'avez pas fait cette demande, ignorez cet email.\n\n" +
                            "L'équipe SmarTest"
            );
        } else {
            String link = "http://localhost:5173/reset-password?token=" + token;
            message.setText(
                    "Bonjour,\n\n" +
                            "Vous avez demandé à réinitialiser votre mot de passe.\n\n" +
                            "Cliquez sur le lien ci-dessous (valable 15 minutes) :\n\n" +
                            link + "\n\n" +
                            "Si vous n'avez pas fait cette demande, ignorez cet email.\n\n" +
                            "L'équipe SmarTest"
            );
        }

        mailSender.send(message);
    }
}