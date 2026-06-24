# syntax=docker/dockerfile:1
# Build context is the `frontend/` directory:
#   docker build -t vw-web frontend/

FROM node:20-alpine AS build
WORKDIR /app
RUN npm install -g pnpm@9

# Vite bakes these at build time (non-secret). VITE_API_BASE_URL=/api keeps the
# SPA same-origin behind Caddy, so no CORS.
ARG VITE_API_BASE_URL=/api
ARG VITE_GOOGLE_CLIENT_ID
ARG VITE_DEFAULT_LOCALE=pt-BR
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL \
    VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID \
    VITE_DEFAULT_LOCALE=$VITE_DEFAULT_LOCALE

# Install deps as a cached layer (pnpm matches CLAUDE.md + pnpm-lock.yaml).
# --ignore-workspace: this is a single package; the repo's pnpm-workspace.yaml is
# not a valid workspace definition, so we skip workspace resolution entirely.
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile --ignore-workspace

COPY . .
# Build the production bundle. We invoke `vite build` directly rather than the
# `build` script (which also runs `tsc -b`); type-checking is enforced in CI.
RUN pnpm --ignore-workspace exec vite build

FROM caddy:2-alpine AS runtime
# Static SPA bundle; Caddyfile is provided at runtime via bind mount (deploy/Caddyfile).
COPY --from=build /app/dist /srv
EXPOSE 80 443
