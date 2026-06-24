# Managed PostgreSQL. Public mode off (private endpoint only), daily backups on.
# The instance reaches it over the Lightsail private network; GitHub runners do
# not — which is why migrations run on API startup, not in CI.

resource "random_password" "db" {
  length  = 32
  special = false # avoid connection-string escaping pain in the .env
}

resource "aws_lightsail_database" "app" {
  relational_database_name = "vw-db"
  availability_zone        = var.availability_zone
  blueprint_id             = "postgres_15"
  bundle_id                = var.db_bundle

  master_database_name = var.db_name
  master_username      = var.db_username
  master_password      = random_password.db.result

  publicly_accessible      = false
  backup_retention_enabled = true
  skip_final_snapshot      = true
  apply_immediately        = true
  preferred_backup_window  = "06:00-06:30"

  tags = {
    Role = "database"
  }
}
