FROM node:24-bookworm-slim AS build
WORKDIR /app

RUN npm install --global npm@11.5.1

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/foodsocial-web/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1:8080/health || exit 1
