# Etapa 1: Construcción
FROM node:20-alpine AS build-stage
WORKDIR /app

# Argumento para la URL del API (se puede pasar en el build)
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL

COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Etapa 2: Servidor de Producción (Nginx)
FROM nginx:stable-alpine
COPY --from=build-stage /app/dist /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
