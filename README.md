# smarTest

[![Tests](https://github.com/oumeyma-elaammari/smarTest/actions/workflows/tests.yml/badge.svg)](https://github.com/oumeyma-elaammari/smarTest/actions/workflows/tests.yml)

Monorepo : backend Spring Boot, client web React (Vite), application bureau .NET (Windows).

## Tests en local

| Paquet | Commande |
|--------|------------|
| Backend (`packages/smartest-backend`) | `./mvnw test` (Linux/macOS) ou `mvnw.cmd test` (Windows) |
| Web (`packages/smartest-web`) | `npm run test:run` — CI : `npm run test:ci` (JUnit + reporters verbeux) |
| Bureau (`packages/smartest-desktop`) | `dotnet test smartest-desktop.Tests/smartest-desktop.Tests.csproj` |

Les tests backend utilisent le profil **test** (H2 en mémoire, pas de MySQL). Gatling est optionnel : `mvn -Pgatling gatling:test` avec un serveur déjà démarré.

## Audits Lighthouse

Non exécutés dans le workflow CI (URL déployée requise). Lancez un audit manuellement après `npm run build` / `npm run preview` si besoin.
