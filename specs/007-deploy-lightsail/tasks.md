# Tasks: Deploy to AWS Lightsail via Terraform + GitHub Actions

**Branch**: `007-deploy-lightsail` | **Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Suggested order: S (app code) → C (containers) → T (terraform) → W (workflows) → R (bring-up). S/C/T can overlap; W depends on C+T; R is last. Runtime secrets are managed manually as GitHub Secrets (Variables/Secrets configured in W4).

## App Code S — Startup Migration + Health

- [x] S1. Add `RunMigrationsOnStartup` (bool, default `false`). `DatabaseMigrationExtensions.ShouldRunMigrationsOnStartup(IConfiguration)` reads the flag; defaults false so local/test are unaffected.
- [x] S2. `MigrateDatabaseIfEnabled()` extension resolves `VirtualWardrobeDbContext` in a scope and calls `Database.Migrate()` only when the flag is on; wired into `Program.cs` between `Build()` and `UseApiHosting()` (before traffic). Logs via source-generated `[LoggerMessage]` to satisfy CA1848.
- [x] S3. Anonymous `HealthController` at `GET /api/health` (`[AllowAnonymous]`, no global auth filter) returns `200 { "status": "ok" }`.
- [x] S4. `StartupContractTests` — flag gate off-by-default + false/False + true/True (5 cases) and `/api/health` returns 200. Contract suite 21/21; unit 34/34; integration 16/16, all green.

## Containers C

- [x] C1. `backend/src/VirtualWardrobe.Api/Dockerfile` — multi-stage sdk:8.0 → aspnet:8.0, non-root `app` user, `ASPNETCORE_HTTP_PORTS=8080`, cached restore layer (copies `Directory.Build.props` + the 4 src csproj before `dotnet restore`). `backend/.dockerignore` added. Build context = `backend/`.
- [x] C2. `frontend/Dockerfile` — `node:20-alpine` → `caddy:2-alpine` serving `/srv`. Uses **pnpm** (`npm i -g pnpm@9`, `pnpm install --frozen-lockfile --ignore-workspace`) to match CLAUDE.md + the maintained `pnpm-lock.yaml`. Builds via `pnpm exec vite build` (see C6 note). VITE_* passed as build args. `frontend/.dockerignore` added.
- [x] C3. `deploy/Caddyfile` — `{$SITE_ADDRESS}`: `handle /api/*` → `reverse_proxy api:8080`; SPA `root /srv` + `try_files {path} /index.html`; `encode gzip`; automatic HTTPS from the hostname. Provided to the web container via bind mount (kept as editable infra config, not baked).
- [x] C4. `deploy/docker-compose.yml` — `web` (Caddy, 80/443, `caddy_data`/`caddy_config` volumes, Caddyfile bind mount) + `api` (`env_file: .env`, expose 8080), both `restart: unless-stopped`, GHCR images `ghcr.io/${GHCR_OWNER}/vw-{api,web}:${IMAGE_TAG}`. `build:` sections included for local builds; instance uses `pull` + `up -d` (no build).
- [x] C5. `deploy/.env.example` — every runtime variable from spec Section 4 as `<placeholder>` values (passes the CI secret-audit).
- [x] C6. Validated locally: `docker compose config` parses; **both images build**; end-to-end smoke test passed — Caddy serves the SPA (200) and proxies `/api/health` → `api:8080` → `200 {"status":"ok"}` (single origin, no CORS).

### C — Pre-existing issues found (not fixed; out of scope, flagged for follow-up)
- `frontend` `pnpm build` (`tsc -b && vite build`) is **broken repo-wide**: `vite@^8` vs `vitest@^3.2` (expects vite 7) makes the `test` field in `vite.config.ts` fail `tsc -b`. The image therefore runs `vite build` directly (the bundler step that produces `dist`); type-checking stays a CI concern. `vite.config.ts` left unchanged.
- `frontend/package-lock.json` is **stale / out of sync** with `package.json` (so `npm ci` fails) — the Dockerfile uses pnpm instead. Consider regenerating or removing the npm lockfile.
- `frontend/pnpm-workspace.yaml` contains placeholder junk (`esbuild: set this to true or false`), which breaks `pnpm install`; the build uses `--ignore-workspace` to bypass it.

## Terraform T

- [x] T1. `backend.tf` wires the S3 + DynamoDB lock backend. The bucket + table are a documented one-time bootstrap (commands in the file header), intentionally **not** managed here so `destroy` can't remove the state describing it.
- [x] T2. `providers.tf` (aws/random/tls pinned, `default_tags`) + `variables.tf` (region, `availability_zone`, `instance_bundle` default **`micro_2_0` = 1 GB** — note `nano_2_0` is 512 MB, not 1 GB — `db_bundle`, `db_name`/`db_username`, `github_repo`, `ssh_allow_cidrs`, `assets_bucket_name`).
- [x] T3. `compute.tf` — `aws_lightsail_instance` (`ubuntu_22_04`, 1 GB) + `cloud-init.yaml` (Docker CE + compose plugin from the official repo, 2 GB swap file + `vm.swappiness=10`, creates `/opt/app`).
- [x] T4. `network.tf` — `aws_lightsail_static_ip` + attachment; `aws_lightsail_instance_public_ports` 22 (restricted to `ssh_allow_cidrs`), 80, 443.
- [x] T5. `keypair.tf` — `aws_lightsail_key_pair`; private key exported as a sensitive output.
- [x] T6. `database.tf` — `aws_lightsail_database` (`postgres_15`, `micro_2_0`, backups on, `publicly_accessible = false`, `skip_final_snapshot`) + `random_password` (no specials, to keep the connection string clean).
- [x] T7. `storage.tf` — `import {}` block adopts the existing `wardrobe-assets-087730237728` bucket; public-access block; presign IAM user + access key + scoped `GetObject`/`PutObject`/`DeleteObject` policy; `prevent_destroy = true` on the bucket.
- [x] T8. `oidc.tf` — GitHub OIDC provider (thumbprint via `tls_certificate` data source) + role scoped to `repo:<owner/repo>` main / `production` / `production-destroy` envs.
- [x] T9. `outputs.tf` — static IP, `sslip_host`, `site_address`, DB endpoint/port, ready-built `connection_string`, SSH key, presign IAM keys, OIDC role ARN (sensitive where applicable).
- [x] T10. `terraform fmt -recursive` clean; `terraform init -backend=false` + `terraform validate` → **"Success! The configuration is valid."** (Terraform v1.14.7). `plan` needs real AWS creds + the bootstrapped backend → runs in `infra.yml` / first bring-up, not locally.

## Workflows W

- [x] W1. `.github/workflows/infra.yml` — `plan` job on PR + push (paths `infra/terraform/**`): OIDC creds, `fmt -check`, `init`, `validate`, `plan`. `apply` job on push only, `environment: production` (manual approval). Pinned Terraform 1.14.7.
- [x] W2. `.github/workflows/deploy.yml` — push to `main` (paths backend/frontend/deploy) + dispatch. `build-push` job: buildx + GHCR login, build/push `vw-api` + `vw-web` tagged `${{ github.sha }}` & `latest` (gha cache); web gets `VITE_*` build args. `deploy` job (`environment: production`): renders `/opt/app/.env` from Secrets/Variables, `scp` compose+Caddyfile+.env, SSH `docker login` + `compose pull && up -d --remove-orphans` + `image prune`, then polls `/api/health` 30×10s and fails on non-200.
- [x] W3. `.github/workflows/destroy.yml` — `workflow_dispatch` only; first step fails unless `inputs.confirm == 'destroy-virtual-wardrobe'`; `environment: production-destroy` (manual approval); OIDC; `terraform destroy -auto-approve`. S3 bucket + state backend excluded via `prevent_destroy` / unmanaged backend.
- [x] W4. Documented in [quickstart.md](./quickstart.md): create `production` + `production-destroy` Environments with reviewers; Variables (`AWS_REGION`, `AWS_ROLE_ARN`, `AWS_S3_BUCKET`, `INSTANCE_HOST`, `SITE_ADDRESS`, `VITE_GOOGLE_CLIENT_ID`) and Secrets (`DB_CONNECTION_STRING`, `JWT_SIGNING_KEY`, `GOOGLE_CLIENT_SECRET`, `PRESIGN_ACCESS_KEY_ID`, `PRESIGN_SECRET_ACCESS_KEY`, `SSH_PRIVATE_KEY`) — with `gh` one-liners sourcing Terraform outputs. **Manual repo-admin step; not automatable from here.**

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
