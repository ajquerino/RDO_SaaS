# ---- build (Vite) ----
FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# VITE_API_URL é lido em BUILD TIME pelo Vite (src/lib/api.ts). Precisa existir ANTES do build.
# Passe via --build-arg VITE_API_URL=... (o docker-compose já faz isso a partir do .env).
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# ---- runtime (nginx servindo os estáticos) ----
FROM nginx:stable-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
