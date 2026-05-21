package com.smartest.backend;

import com.smartest.backend.config.ExamenResultatPublicationProperties;
import com.smartest.backend.config.GroqProperties;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;

@SpringBootApplication
@EnableConfigurationProperties({GroqProperties.class, ExamenResultatPublicationProperties.class})
public class BackendApplication {

	public static void main(String[] args) {
		SpringApplication.run(BackendApplication.class, args);
	}

}
