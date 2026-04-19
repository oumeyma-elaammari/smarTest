package com.smartest.backend.controller;

import org.springframework.messaging.handler.annotation.MessageMapping;
import org.springframework.stereotype.Controller;

@Controller
public class ExamenWebSocketController {

    @MessageMapping("/examen/reponse")
    public void recevoirReponse() {
        // logique temps réel (à compléter)
    }
}