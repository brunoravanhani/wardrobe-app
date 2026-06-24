variable "region" {
  description = "AWS region for all resources."
  type        = string
  default     = "us-east-1"
}

variable "availability_zone" {
  description = "Availability zone for the Lightsail instance and database."
  type        = string
  default     = "us-east-1a"
}

variable "instance_bundle" {
  description = "Lightsail instance bundle. micro_2_0 = 1 GB RAM (nano_2_0 is 512 MB)."
  type        = string
  default     = "micro_2_0"
}

variable "db_bundle" {
  description = "Lightsail managed-database bundle (smallest PostgreSQL = micro_2_0)."
  type        = string
  default     = "micro_2_0"
}

variable "db_name" {
  description = "Initial database name created on the managed PostgreSQL instance."
  type        = string
  default     = "virtualwardrobe"
}

variable "db_username" {
  description = "Master username for the managed PostgreSQL instance."
  type        = string
  default     = "vwadmin"
}

variable "github_repo" {
  description = "GitHub repository (owner/name) allowed to assume the OIDC role."
  type        = string
  default     = "brunoravanhani/virtual-wardrobe"
}

variable "ssh_allow_cidrs" {
  description = "CIDR blocks permitted to reach SSH (port 22) on the instance."
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "assets_bucket_name" {
  description = "Existing private S3 bucket holding wardrobe media (imported, retained). Provided via secrets/tfvars."
  type        = string
}
