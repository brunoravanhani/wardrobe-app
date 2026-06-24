# Tasks: Deploy to AWS Lightsail via Terraform + GitHub Actions

**Branch**: `007-deploy-lightsail` | **Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Suggested order: S (app code) → C (containers) → T (terraform) → W (workflows) → R (bring-up). S/C/T can overlap; W depends on C+T; R is last. Runtime secrets are managed manually as GitHub Secrets (Variables/Secrets configured in W4).

## App Code S — Startup Migration + Health

- [x] S1. Add `RunMigrationsOnStartup` (bool, default `false`). `DatabaseMigrationExtensions.ShouldRunMigrationsOnStartup(IConfiguration)` reads the flag; defaults false so local/test are unaffected.
- [x] S2. `MigrateDatabaseIfEnabled()` extension resolves `VirtualWardrobeDbContext` in a scope and calls `Database.Migrate()` only when the flag is on; wired into `Program.cs` between `Build()` and `UseApiHosting()` (before traffic). Logs via source-generated `[LoggerMessage]` to satisfy CA1848.
- [x] S3. Anonymous `HealthController` at `GET /api/health` (`[AllowAnonymous]`, no global auth filter) returns `200 { "status": "ok" }`.
- [x] S4. `StartupContractTests` — flag gate off-by-default + false/False + true/True (5 cases) and `/api/health` returns 200. Contract suite 21/21; unit 34/34; integration 16/16, all green.

## Containers C

- [ ] C1. `backend/src/VirtualWardrobe.Api/Dockerfile` — multi-stage sdk:8.0 → aspnet:8.0, non-root, `ASPNETCORE_HTTP_PORTS=8080`; add `.dockerignore`.
- [ ] C2. `frontend/Dockerfile` — `node:20` build with `VITE_API_BASE_URL=/api`, `VITE_GOOGLE_CLIENT_ID`, `VITE_DEFAULT_LOCALE=pt-BR` build args; copy `dist` into a `caddy:2` image; add `.dockerignore`.
- [ ] C3. `deploy/Caddyfile` — `{$SITE_ADDRESS}`: `/api/*` → `reverse_proxy api:8080`; SPA static + `try_files … /index.html`; `encode gzip`; automatic HTTPS.
- [ ] C4. `deploy/docker-compose.yml` — `web` (80/443, cert volume) + `api` (`env_file /opt/app/.env`, expose 8080), `restart: unless-stopped`, GHCR images tagged `${IMAGE_TAG}`.
- [ ] C5. `deploy/.env.example` — every runtime variable from spec Section 4 as placeholders.
- [ ] C6. Local validation: `docker compose config` parses; build both images locally.

## Terraform T

- [ ] T1. Bootstrap remote state: S3 bucket + DynamoDB lock table; wire `backend.tf`.
- [ ] T2. `providers.tf` + `variables.tf` (region, `instance_bundle` default 1 GB e.g. `nano_2_0`, `db_bundle`, `github_repo`, `ssh_allow_cidrs`, `site_address`).
- [ ] T3. `compute.tf` — `aws_lightsail_instance` (Ubuntu 22.04, 1 GB) + `cloud-init.yaml` (Docker + Compose plugin, swap file for 1 GB headroom, create `/opt/app`).
- [ ] T4. `network.tf` — static IP + attachment; `instance_public_ports` 22 (restricted to `ssh_allow_cidrs`), 80, 443.
- [ ] T5. `keypair.tf` — `aws_lightsail_key_pair`; sensitive private-key output.
- [ ] T6. `database.tf` — `aws_lightsail_database` (PostgreSQL 15, smallest bundle, backups on, `publicly_accessible = false`) + `random_password`.
- [ ] T7. `storage.tf` — `import` existing `wardrobe-assets-…` bucket; public-access block; IAM user + access key + presign-only policy; `prevent_destroy = true` on the bucket.
- [ ] T8. `oidc.tf` — GitHub OIDC provider + role scoped to this repo for infra/destroy workflows.
- [ ] T9. `outputs.tf` — static IP, `sslip_host`, DB endpoint, SSH key, IAM keys (sensitive).
- [ ] T10. `terraform fmt`/`validate`/`plan` clean; idempotent re-plan shows no drift (incl. imported bucket).

## Workflows W

- [ ] W1. `.github/workflows/infra.yml` — PR (`infra/**`): fmt-check + validate + plan; `main`: apply gated by `production` Environment approval; OIDC auth.
- [ ] W2. `.github/workflows/deploy.yml` — push to `main` + dispatch: build/push `vw-api` + `vw-web` to GHCR (`${{ github.sha }}`); SSH write `/opt/app/.env`, set `IMAGE_TAG`, `compose pull && up -d`, prune; curl `/api/health`, fail on non-200.
- [ ] W3. `.github/workflows/destroy.yml` — `workflow_dispatch` only; `confirm` input must equal `destroy-virtual-wardrobe` (fail fast otherwise); `production-destroy` Environment approval; OIDC; `terraform destroy -auto-approve` (S3 bucket + state backend excluded via `prevent_destroy`).
- [ ] W4. Configure repo Environments (`production`, `production-destroy`) with reviewers; add Secrets (DB pieces, JWT key, Google client id/secret, SSH key, AWS presign keys) and Variables (`SITE_ADDRESS`, `VITE_GOOGLE_CLIENT_ID`).

## Bring-Up R

- [ ] R1. `terraform apply`; push sensitive outputs into GitHub Secrets (`gh secret set`).
- [ ] R2. Add `https://<static-ip>.sslip.io` to Google OAuth authorized JS origins + redirect URIs.
- [ ] R3. Run `deploy.yml`; confirm images pulled and stack up.
- [ ] R4. Verify TLS issued, `/api/health` = 200, and a real Google login round-trip succeeds.
- [ ] R5. Sanity-check p95 budgets on the 1 GB instance (list ≤ 2s, create/edit ≤ 3s); note swap/bundle-bump if memory-constrained.

## Final Verification V

- [ ] V1. `infra.yml` plan/apply path works end to end (approval gating verified).
- [ ] V2. `deploy.yml` build → push → deploy → health check green.
- [ ] V3. `destroy.yml` rejects a wrong `confirm` phrase; with the correct phrase + approval, tears down compute/DB/IP/IAM/keypair while S3 bucket + state remain.
- [ ] V4. Repo contains no real secrets; CI secret-audit passes.
