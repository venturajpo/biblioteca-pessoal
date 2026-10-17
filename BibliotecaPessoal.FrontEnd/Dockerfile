FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --chmod=755 basic-auth.sh /docker-entrypoint.d/40-basic-auth.sh
# O sed tira o CRLF de um checkout no Windows, que impediria o sh de rodar o script.
RUN sed -i 's/\r$//' /docker-entrypoint.d/40-basic-auth.sh && touch /etc/nginx/basic-auth.conf
COPY --from=build /app/dist/BibliotecaPessoal.FrontEnd/browser /usr/share/nginx/html
EXPOSE 80
