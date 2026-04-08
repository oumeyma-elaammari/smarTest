package com.smartest.backend.dto.response;

import lombok.Data;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

@Data
public class MessageResponse {

    private String message;

    private boolean success;

    private Integer statusCode;

    private String timestamp;

    // Constructeur par défaut
    public MessageResponse() {
        this.timestamp = getCurrentTimestamp();
    }

    // Constructeur avec message et success
    public MessageResponse(String message, boolean success) {
        this.message = message;
        this.success = success;
        this.timestamp = getCurrentTimestamp();
    }

    // Constructeur complet
    public MessageResponse(String message, boolean success, Integer statusCode) {
        this.message = message;
        this.success = success;
        this.statusCode = statusCode;
        this.timestamp = getCurrentTimestamp();
    }

    // Méthode utilitaire pour le timestamp
    private String getCurrentTimestamp() {
        return LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss"));
    }

    // Méthodes statiques pour une création facile
    public static MessageResponse success(String message) {
        return new MessageResponse(message, true, 200);
    }

    public static MessageResponse success(String message, Integer statusCode) {
        return new MessageResponse(message, true, statusCode);
    }

    public static MessageResponse error(String message) {
        return new MessageResponse(message, false, 400);
    }

    public static MessageResponse error(String message, Integer statusCode) {
        return new MessageResponse(message, false, statusCode);
    }

    public static MessageResponse notFound(String entityName) {
        return new MessageResponse(entityName + " non trouvé(e)", false, 404);
    }

    public static MessageResponse created(String entityName) {
        return new MessageResponse(entityName + " créé(e) avec succès", true, 201);
    }

    public static MessageResponse deleted(String entityName) {
        return new MessageResponse(entityName + " supprimé(e) avec succès", true, 200);
    }

    public static MessageResponse updated(String entityName) {
        return new MessageResponse(entityName + " mis(e) à jour avec succès", true, 200);
    }
}
