package com.smartest.backend.dto.qr;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class QrLiveSessionCreatedResponse {
    private String sessionToken;
}
