package com.smartest.backend.dto.response;


import lombok.*;


@AllArgsConstructor
@Getter
@Setter
@NoArgsConstructor
public class AuthResponse {

    private String token;

    private String role;

    private String nom;

    private String email;
}