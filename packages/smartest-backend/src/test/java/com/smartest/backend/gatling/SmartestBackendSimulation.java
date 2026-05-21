package com.smartest.backend.gatling;

import static io.gatling.javaapi.core.CoreDsl.rampUsers;
import static io.gatling.javaapi.core.CoreDsl.scenario;
import static io.gatling.javaapi.http.HttpDsl.http;
import static io.gatling.javaapi.http.HttpDsl.status;

import io.gatling.javaapi.core.ScenarioBuilder;
import io.gatling.javaapi.core.Simulation;
import io.gatling.javaapi.http.HttpProtocolBuilder;

import java.time.Duration;

/**
 * Simulation minimale (DSL Java) : une charge legere sur l'API locale (port 8081).
 * Demarrer l'application avant {@code mvn gatling:test}.
 * <p>
 * Sans Actuator, {@code /actuator/health} renvoie souvent 404 ; les codes acceptes
 * ci-dessous permettent tout de meme de valider que le serveur repond.
 */
public class SmartestBackendSimulation extends Simulation {

    HttpProtocolBuilder httpProtocol = http
            .baseUrl("http://localhost:8081")
            .acceptHeader("application/json");

    ScenarioBuilder healthScenario = scenario("Health probe")
            .exec(
                    http("GET /actuator/health")
                            .get("/actuator/health")
                            .check(status().in(200, 401, 403, 404))
            );

    {
        setUp(
                healthScenario.injectOpen(rampUsers(1).during(Duration.ofSeconds(5)))
        ).protocols(httpProtocol);
    }
}
