# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Front-end — Angular compilado e servido por nginx
# Contexto de build: src/web.
# ---------------------------------------------------------------------------

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
# npm ci exige o package-lock.json; enquanto ele não existir, cai no npm install.
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.27-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/biblioteca-pessoal-web/browser /usr/share/nginx/html

EXPOSE 80
