# Implementation Plan: Deploy to AWS Lightsail via Terraform + GitHub Actions

**Branch**: `007-deploy-lightsail` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

## Summary

Provision and continuously deliver Virtual Wardrobe on Amazon Lightsail:

- **Compute** — one **1 GB Lightsail instance** running Docker Compose with a
  Caddy `web` container (serves the SPA + reverse-proxies `/api/*` + auto TLS via
  `<static-ip>.sslip.io`) and a .NET 8 `api` container. Single origin → no CORS.
- **Database** — a **Lightsail managed PostgreSQL** instance, public mode off,
  private endpoint + TLS.
- **Storage** — the **existing S3 bucket is imported** and retained; presign access
  via a scoped IAM user (Lightsail has no instance role).
- **IaC** — a `infra/terraform/` module with an S3 + DynamoDB state backend.
- **CI/CD** — `infra.yml` (plan/apply), `deploy.yml` (build → GHCR → SSH →
  compose → health check), and a manual, guarded `destroy.yml`.
- **App change** — a guarded `RunMigrationsOnStartup` startup migration + a
  `/api/health` endpoint for the deploy health check.

Recommended delivery order: **App code change → Containers → Terraform → CI/CD
workflows → first bring-up**. Infra and app container work can proceed in
parallel; bring-up is last. Runtime secrets are managed manually as GitHub
Actions Secrets.

## Technical Context

**Language/Version**: C# 12 on .NET 8 (API), TypeScript / React 19 + Vite
(frontend). **Terraform** ≥ 1.6, **AWS provider** ≥ 5.x. **Caddy** 2, **Docker
Compose** v2.

**Region**: default `us-east-1` (variable). **Registry**: GHCR.

**Migrations**: applied on API startup behind `RunMigrationsOnStartup`; no CI
migration job (managed DB private endpoint is unreachable from GitHub runners).

**Secrets**: managed manually as GitHub Actions Secrets, injected into
`/opt/app/.env` on the instance at deploy time. This feature introduces no new
secret values into source.

## Repository Additions

```
infra/terraform/
  backend.tf            # S3 remote state + DynamoDB lock
  providers.tf          # aws provider + region var
  variables.tf          # region, instance_bundle (default 1 GB), db_bundle, github_repo, ssh_allow_cidrs, site_address
  network.tf            # static IP + attachment + instance_public_ports (22/80/443)
  compute.tf            # aws_lightsail_instance (1 GB) + cloud-init user_data
  database.tf           # aws_lightsail_database (postgres) + random_password
  storage.tf            # imported S3 bucket + public-access block + IAM user/policy/key (presign-only)
  keypair.tf            # aws_lightsail_key_pair (SSH); private key sensitive output
  oidc.tf               # GitHub OIDC provider + role for infra/destroy workflows
  outputs.tf            # static_ip, sslip_host, db_endpoint, ssh_key, iam keys (sensitive)
  cloud-init.yaml       # installs docker + compose plugin; configures swap (1 GB headroom)

deploy/
  docker-compose.yml    # web (Caddy) + api; env_file /opt/app/.env; IMAGE_TAG
  Caddyfile             # {$SITE_ADDRESS}: TLS + static SPA + reverse_proxy /api
  .env.example          # documents every runtime variable

backend/src/VirtualWardrobe.Api/Dockerfile     # multi-stage sdk 8.0 -> aspnet 8.0, non-root
backend/src/VirtualWardrobe.Api/.dockerignore
frontend/Dockerfile                             # build dist -> copy into caddy:2 image
frontend/.dockerignore

.github/workflows/
  infra.yml             # fmt/validate/plan on PR; apply on main (env approval, OIDC)
  deploy.yml            # build+push GHCR; ssh deploy; health check
  destroy.yml           # manual workflow_dispatch; confirm phrase + env approval; terraform destroy
```

## Section 1 — Application Code Change (do first)

1. Add a `RunMigrationsOnStartup` boolean to configuration (default `false`).
2. In the API hosting startup (`AddApiHosting`/`UseApiHosting` extension), when
   the flag is true, resolve the `DbContext` in a scope and call
   `Database.Migrate()` **before** the app serves traffic.
3. Ensure a health endpoint exists at `/api/health` (add a minimal
   `MapHealthChecks("/api/health")` or equivalent if not already present) that
   does not require auth.
4. Tests: a unit/integration test that the flag gates the migration call (off by
   default → no migration in test) and that `/api/health` returns 200. Existing
   suite must stay green.

## Section 2 — Containers

5. **API `Dockerfile`** — multi-stage: `mcr.microsoft.com/dotnet/sdk:8.0`
   restores/publishes `VirtualWardrobe.Api`; `mcr.microsoft.com/dotnet/aspnet:8.0`
   runs as a non-root user; `ASPNETCORE_HTTP_PORTS=8080`. Add `.dockerignore`.
6. **Frontend `Dockerfile`** — `node:20` runs `pnpm install --frozen-lockfile`
   and `pnpm build` with `VITE_API_BASE_URL=/api`, `VITE_GOOGLE_CLIENT_ID`,
   `VITE_DEFAULT_LOCALE=pt-BR` as build args; copy `dist` into a `caddy:2` image
   together with the `Caddyfile`. Add `.dockerignore`.
7. **`Caddyfile`** — single site `{$SITE_ADDRESS}`: `handle /api/*` →
   `reverse_proxy api:8080`; `handle` → `root * /srv` + `try_files {path}
   /index.html` (SPA fallback) + `file_server`; `encode gzip`. Automatic HTTPS
   from the hostname.
8. **`docker-compose.yml`** — services `web` (ports 80/443, named volume for
   Caddy cert/data, depends_on api) and `api` (`env_file: /opt/app/.env`,
   `expose: 8080`); both `restart: unless-stopped`; images
   `ghcr.io/<owner>/vw-web:${IMAGE_TAG}` / `vw-api:${IMAGE_TAG}`.
9. **`deploy/.env.example`** — list every variable from spec Section 4 with
   placeholders (no real values).

## Section 3 — Terraform

10. **State backend** — bootstrap an S3 bucket + DynamoDB lock table (documented
    one-time step in `quickstart.md`); wire `backend.tf`.
11. **providers/variables** — `aws` provider pinned; vars for region,
    `instance_bundle` (default the 1 GB bundle, e.g. `nano_2_0`), `db_bundle`,
    `github_repo`, `ssh_allow_cidrs`, `site_address`.
12. **compute.tf** — `aws_lightsail_instance` (Ubuntu 22.04, 1 GB) with
    `cloud-init.yaml` user_data (install Docker + Compose plugin; create a swap
    file to relieve the 1 GB memory pressure; create `/opt/app`).
13. **network.tf** — `aws_lightsail_static_ip` + attachment;
    `aws_lightsail_instance_public_ports` opening 22 (restricted to
    `ssh_allow_cidrs`), 80, 443.
14. **keypair.tf** — `aws_lightsail_key_pair`; export private key as a sensitive
    output.
15. **database.tf** — `aws_lightsail_database` (PostgreSQL 15, smallest bundle,
    backups on, `publicly_accessible = false`); `random_password` for master
    credential.
16. **storage.tf** — `import` the existing `wardrobe-assets-…` bucket; add a
    public-access block; create an `aws_iam_user` + `aws_iam_access_key` + scoped
    policy (`s3:GetObject`/`s3:PutObject` on the bucket ARN). Add `lifecycle {
    prevent_destroy = true }` on the bucket.
17. **oidc.tf** — `aws_iam_openid_connect_provider` for GitHub + a role assumable
    by `infra.yml`/`destroy.yml` scoped to this repo's `main` and dispatch refs.
18. **outputs.tf** — static IP, `sslip_host` (`<ip>.sslip.io`), DB endpoint, SSH
    private key, IAM keys (all sensitive where applicable).

## Section 4 — CI/CD Workflows

19. **`infra.yml`** — PR (paths `infra/**`): `terraform fmt -check`, `validate`,
    `plan` (post plan). `main`: `apply` gated by a `production` Environment manual
    approval. AWS auth via OIDC role; remote state from `backend.tf`.
20. **`deploy.yml`** — push to `main` + `workflow_dispatch`: build/push
    `vw-api` and `vw-web` images to GHCR tagged `${{ github.sha }}`; `ssh-action`
    to the instance to write `/opt/app/.env` from secrets, set `IMAGE_TAG`,
    `docker compose pull && up -d`, `docker image prune -f`; then curl
    `https://${SITE_ADDRESS}/api/health` and fail on non-200.
21. **`destroy.yml`** — `workflow_dispatch` only, input `confirm`; first step
    fails unless `confirm == 'destroy-virtual-wardrobe'`; targets a
    `production-destroy` Environment (manual approval); OIDC role; `terraform
    destroy -auto-approve`. The `prevent_destroy` S3 bucket and the state backend
    are excluded, so destroy removes compute/DB/IP/IAM/keypair only.
22. Configure GitHub repo: Environments (`production`, `production-destroy`) with
    reviewers; Secrets (DB connection pieces, JWT key, Google client id/secret,
    SSH key, AWS presign keys) and Variables (`SITE_ADDRESS`,
    `VITE_GOOGLE_CLIENT_ID`).

## Section 5 — First Bring-Up (runbook, see quickstart.md)

23. Bootstrap state backend → `terraform apply` → push sensitive outputs into
    GitHub Secrets → add `https://<host>` to Google OAuth origins/redirects → set
    remaining secrets → run `deploy.yml` → verify health + a real login
    round-trip.

## Constitution Checks

- **Testing Gate**: `RunMigrationsOnStartup` gating and `/api/health` get tests
  that fail before / pass after; Terraform guarded by `validate`/`plan`; deploy
  health check is the integration gate. Existing suites unchanged.
- **Reuse Gate**: One reverse-proxy origin (Caddy) reused for SPA + API instead of
  a separate CORS-configured frontend host; existing S3 bucket reused, not
  recreated.
- **Architecture Gate**: No change to layering or domain; the only app code is a
  hosting-startup migration toggle + health endpoint. `Program.cs` stays thin.
- **DB Versioning Gate**: Schema reaches prod only via EF Core migrations
  (startup-applied); roll-forward remains the remediation path; no manual schema
  edits.
- **Secret Management Gate**: Runtime secrets are managed manually as GitHub
  Secrets and injected into `/opt/app/.env` at deploy; no new secret values enter
  source; the existing CI secret-audit job still passes.
- **Observability Gate**: Structured JSON logs to stdout captured by Docker on the
  instance; auth / S3 presign / wishlist-conversion events stay instrumented.
- **UX Consistency Gate**: No user-facing copy change; pt-BR content unaffected.
- **Performance Gate**: Same-origin proxy avoids CORS preflights; p95 budgets
  re-validated on the deployed 1 GB instance (the sizing is the variable to
  watch; swap + an easy bundle bump are the mitigations).
