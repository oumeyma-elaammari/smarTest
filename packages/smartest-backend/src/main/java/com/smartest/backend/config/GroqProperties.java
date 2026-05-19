package com.smartest.backend.config;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;

@Getter
@Setter
@ConfigurationProperties(prefix = "groq")
public class GroqProperties {

    /**
     * Clé API Groq (https://console.groq.com). Vide : pas d'appel IA (rédactions restent à 0 / correction manuelle).
     */
    private String apiKey = "";

    private String model = "llama-3.1-8b-instant";

    private int timeoutSeconds = 45;

    private int maxRetries = 2;
}
