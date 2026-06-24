# Remote state backend (S3-only).
#
# Bootstrap (one-time, documented in quickstart.md) creates the S3 bucket BEFORE
# this backend can be initialised. The bucket is intentionally NOT managed by this
# configuration so that `terraform destroy` can never remove the state that
# describes it. There is no DynamoDB lock table: state locking is handled natively
# by S3 (conditional writes) via `use_lockfile`.
#
#   aws s3api create-bucket --bucket <repository-name> --region us-east-1
#   aws s3api put-bucket-versioning --bucket <repository-name> \
#       --versioning-configuration Status=Enabled
#
# The bucket name is supplied at init time (partial backend config) from the
# TF_STATE_BUCKET secret, e.g.:
#
#   terraform init -backend-config="bucket=$TF_STATE_BUCKET"

terraform {
  required_version = ">= 1.10.0"

  backend "s3" {
    # bucket is provided at init time via -backend-config (TF_STATE_BUCKET secret).
    key          = "virtual-wardrobe/terraform.tfstate"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }
}
