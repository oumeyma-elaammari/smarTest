# Build
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Optionnel : URL API distante (sinon nginx proxy sur le même hôte, URLs relatives)
ARG VITE_API_URL=
ARG VITE_WS_BASE_URL=
ENV VITE_API_URL=$VITE_API_URL
ENV VITE_WS_BASE_URL=$VITE_WS_BASE_URL

RUN npm run build

# Runtime
FROM nginx:1.27-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
