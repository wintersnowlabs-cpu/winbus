FROM node:20-alpine AS build
WORKDIR /app

COPY monitoring-ui/package.json monitoring-ui/package-lock.json ./
RUN npm ci

COPY monitoring-ui ./
ARG VITE_MONITORING_API_BASE_URL=""
ARG VITE_MONITORING_API_KEY=""
ENV VITE_MONITORING_API_BASE_URL=${VITE_MONITORING_API_BASE_URL}
ENV VITE_MONITORING_API_KEY=${VITE_MONITORING_API_KEY}
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY monitoring-ui/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
