# Build
FROM maven:3.9-eclipse-temurin-23 AS build
WORKDIR /app

COPY pom.xml .
RUN mvn -B -q dependency:go-offline -DskipTests

COPY src ./src
RUN mvn -B -DskipTests package

# Runtime
FROM eclipse-temurin:23-jre-alpine
WORKDIR /app

RUN apk add --no-cache wget netcat-openbsd \
    && addgroup -S spring && adduser -S spring -G spring
USER spring:spring

COPY --from=build /app/target/backend-*.jar app.jar

EXPOSE 8081

ENTRYPOINT ["java", "-jar", "app.jar"]
