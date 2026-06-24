output "static_ip" {
  description = "Public static IP of the app host."
  value       = aws_lightsail_static_ip.app.ip_address
}

output "sslip_host" {
  description = "Hostname for TLS + Google OAuth origins (no custom domain in v1)."
  value       = "${replace(aws_lightsail_static_ip.app.ip_address, ".", "-")}.sslip.io"
}

output "site_address" {
  description = "Full HTTPS site address (SITE_ADDRESS / Caddy site)."
  value       = "https://${replace(aws_lightsail_static_ip.app.ip_address, ".", "-")}.sslip.io"
}

output "db_endpoint" {
  description = "Managed PostgreSQL private endpoint."
  value       = aws_lightsail_database.app.master_endpoint_address
}

output "db_port" {
  description = "Managed PostgreSQL port."
  value       = aws_lightsail_database.app.master_endpoint_port
}

output "db_password" {
  description = "Managed PostgreSQL master password."
  value       = random_password.db.result
  sensitive   = true
}

output "connection_string" {
  description = "Ready-to-use ConnectionStrings__Default for /opt/app/.env."
  value = format(
    "Host=%s;Port=%d;Database=%s;Username=%s;Password=%s;SSL Mode=Require;Trust Server Certificate=true",
    aws_lightsail_database.app.master_endpoint_address,
    aws_lightsail_database.app.master_endpoint_port,
    var.db_name,
    var.db_username,
    random_password.db.result,
  )
  sensitive = true
}

output "ssh_private_key" {
  description = "Private SSH key for deploy.yml (store as a GitHub Secret)."
  value       = aws_lightsail_key_pair.app.private_key
  sensitive   = true
}

output "presign_access_key_id" {
  description = "AWS__AccessKeyId for the presign IAM user."
  value       = aws_iam_access_key.presign.id
  sensitive   = true
}

output "presign_secret_access_key" {
  description = "AWS__SecretAccessKey for the presign IAM user."
  value       = aws_iam_access_key.presign.secret
  sensitive   = true
}

output "github_oidc_role_arn" {
  description = "Role ARN for infra.yml / destroy.yml (configure as repo variable AWS_ROLE_ARN)."
  value       = aws_iam_role.github_infra.arn
}
