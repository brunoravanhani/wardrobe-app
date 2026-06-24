# GitHub OIDC: lets infra.yml / destroy.yml assume an AWS role without long-lived
# keys. Scoped to this repo (main branch + workflow_dispatch refs).

data "tls_certificate" "github" {
  url = "https://token.actions.githubusercontent.com"
}

resource "aws_iam_openid_connect_provider" "github" {
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = [data.tls_certificate.github.certificates[0].sha1_fingerprint]
}

data "aws_iam_policy_document" "github_assume" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    # Only the environment-gated jobs assume this role (infra plan-apply →
    # production; destroy → production-destroy). No bare-branch / pull_request
    # subject is trusted, so PR jobs cannot reach AWS.
    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:sub"
      values = [
        "repo:${var.github_repo}:environment:production",
        "repo:${var.github_repo}:environment:production-destroy",
      ]
    }
  }
}

resource "aws_iam_role" "github_infra" {
  name               = "vw-github-infra"
  assume_role_policy = data.aws_iam_policy_document.github_assume.json
}

# Infra/destroy workflows manage Lightsail + the IAM/OIDC scaffolding. AdministratorAccess
# keeps the role simple for a single-tenant project; tighten to least-privilege if shared.
resource "aws_iam_role_policy_attachment" "github_infra_admin" {
  role       = aws_iam_role.github_infra.name
  policy_arn = "arn:aws:iam::aws:policy/AdministratorAccess"
}
