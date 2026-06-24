# Quickstart: Lightsail Bring-Up Runbook

This is the one-time, mostly-manual path to get Virtual Wardrobe running on
Lightsail. After it, every push to `main` deploys automatically.

## 0. Prerequisites

- AWS account with the existing private S3 bucket holding wardrobe media (name supplied via secrets).
- AWS CLI + Terraform ≥ 1.6 locally (only for the bootstrap + first apply).
- Repo admin rights (to create Environments, Secrets, Variables).

## 1. Bootstrap the Terraform state backend (once)

These two resources are deliberately **not** managed by Terraform so `destroy.yml`
can never delete the state that describes the stack.

```bash
aws s3api create-bucket --bucket virtual-wardrobe-tfstate --region us-east-1
aws s3api put-bucket-versioning --bucket virtual-wardrobe-tfstate \
    --versioning-configuration Status=Enabled
aws dynamodb create-table --table-name virtual-wardrobe-tflock \
    --attribute-definitions AttributeName=LockID,AttributeType=S \
    --key-schema AttributeName=LockID,KeyType=HASH \
    --billing-mode PAY_PER_REQUEST --region us-east-1
```

## 2. First `terraform apply` (locally, with admin creds)

```bash
cd infra/terraform
terraform init
terraform apply        # imports the S3 bucket, provisions everything else
```

Capture the outputs (sensitive ones need `-raw`):

```bash
terraform output static_ip
terraform output sslip_host
terraform output -raw connection_string
terraform output -raw ssh_private_key
terraform output -raw presign_access_key_id
terraform output -raw presign_secret_access_key
terraform output github_oidc_role_arn
```

## 3. Configure GitHub Environments (W4)

Settings → Environments → create two, each with **required reviewers** (yourself):

| Environment          | Used by                       |
| -------------------- | ----------------------------- |
| `production`         | `infra.yml` apply, `deploy.yml` |
| `production-destroy` | `destroy.yml`                 |

## 4. Configure repo Variables and Secrets (W4)

**Variables** (Settings → Secrets and variables → Actions → Variables) — non-secret:

| Variable               | Value (from outputs / known)            |
| ---------------------- | --------------------------------------- |
| `AWS_REGION`           | `us-east-1`                             |
| `AWS_ROLE_ARN`         | `terraform output github_oidc_role_arn` |
| `AWS_S3_BUCKET`        | existing private media bucket name      |
| `INSTANCE_HOST`        | `terraform output static_ip`            |
| `SITE_ADDRESS`         | `terraform output sslip_host` (e.g. `1-2-3-4.sslip.io`) |
| `VITE_GOOGLE_CLIENT_ID`| Google OAuth client id (public)         |

**Secrets** (same screen → Secrets) — sensitive:

| Secret                       | Source                                         |
| ---------------------------- | ---------------------------------------------- |
| `DB_CONNECTION_STRING`       | `terraform output -raw connection_string`      |
| `JWT_SIGNING_KEY`            | your 32+ char key                              |
| `GOOGLE_CLIENT_SECRET`       | Google OAuth client secret                     |
| `PRESIGN_ACCESS_KEY_ID`      | `terraform output -raw presign_access_key_id`  |
| `PRESIGN_SECRET_ACCESS_KEY`  | `terraform output -raw presign_secret_access_key` |
| `SSH_PRIVATE_KEY`            | `terraform output -raw ssh_private_key`        |

`gh` one-liners (run from repo root):

```bash
gh variable set AWS_REGION --body us-east-1
gh variable set AWS_ROLE_ARN --body "$(terraform -chdir=infra/terraform output -raw github_oidc_role_arn)"
gh variable set INSTANCE_HOST --body "$(terraform -chdir=infra/terraform output -raw static_ip)"
gh variable set SITE_ADDRESS --body "$(terraform -chdir=infra/terraform output -raw sslip_host)"
gh secret set DB_CONNECTION_STRING --body "$(terraform -chdir=infra/terraform output -raw connection_string)"
gh secret set SSH_PRIVATE_KEY --body "$(terraform -chdir=infra/terraform output -raw ssh_private_key)"
gh secret set PRESIGN_ACCESS_KEY_ID --body "$(terraform -chdir=infra/terraform output -raw presign_access_key_id)"
gh secret set PRESIGN_SECRET_ACCESS_KEY --body "$(terraform -chdir=infra/terraform output -raw presign_secret_access_key)"
# JWT_SIGNING_KEY, GOOGLE_CLIENT_SECRET, VITE_GOOGLE_CLIENT_ID set by hand.
```

## 5. Google OAuth

In the Google Cloud console, add to the OAuth client:

- **Authorized JavaScript origins**: `https://<SITE_ADDRESS>`
- **Authorized redirect URIs**: whatever the app uses (e.g. `https://<SITE_ADDRESS>`)

## 6. Deploy

```bash
gh workflow run deploy.yml      # or just push to main
```

`deploy.yml` builds + pushes the images to GHCR, SSHes in, writes `/opt/app/.env`,
`docker compose pull && up -d`, then polls `https://<SITE_ADDRESS>/api/health`
until it returns 200 (TLS issuance on first boot can take a minute).

## 7. Verify

- `https://<SITE_ADDRESS>/api/health` → `200 {"status":"ok"}`
- Open the site, complete a real Google login round-trip.
- Spot-check p95 budgets (list ≤ 2s, create/edit ≤ 3s). If memory-bound on the
  1 GB box, the swap file already helps; bumping `instance_bundle` to `small_2_0`
  (2 GB) is a one-line change + apply.

## Tear-down

Actions → **Destroy** → type `destroy-virtual-wardrobe` → approve the
`production-destroy` environment. Removes compute / DB / static IP / IAM / key
pair. The S3 assets bucket (`prevent_destroy`) and the state backend remain.
