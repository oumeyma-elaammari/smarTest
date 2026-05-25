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

## Déploiement Docker

Stack serveur : **MySQL**, **backend Spring Boot**, **frontend React** (nginx). L’application bureau .NET n’est pas conteneurisée (client Windows).

```bash
cp .env.example .env
# Éditer .env (mot de passe MySQL, GROQ_API_KEY, JWT, etc.)

docker compose up -d --build
```

| Service   | URL par défaut        | Dockerfile |
|-----------|------------------------|------------|
| Frontend  | http://localhost       | `packages/smartest-web/Dockerfile` |
| Backend   | http://localhost:8081  | `packages/smartest-backend/Dockerfile` |
| MySQL     | localhost:3307 (hôte ; 3306 en interne Docker) | image `mysql:8.4` |

Le conteneur **frontend** proxifie `/api`, `/auth` et `/ws` vers le backend (comme le proxy Vite en développement). Pour une API sur un autre domaine, reconstruire le frontend avec `VITE_API_URL` / `VITE_WS_BASE_URL` (arguments de build du Dockerfile web).

Si le port **3306** est déjà pris (MySQL local), le défaut expose MySQL sur **3307** (`MYSQL_PORT` dans `.env`). Le backend utilise le réseau Docker sans avoir besoin d’exposer MySQL sur l’hôte.

```bash
docker compose down          # arrêter
docker compose logs -f backend
docker compose down -v       # supprimer aussi le volume MySQL
```
