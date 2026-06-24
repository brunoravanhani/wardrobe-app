# Remote state backend.
#
# Bootstrap (one-time, documented in quickstart.md) creates the S3 bucket and the
# DynamoDB lock table BEFORE this backend can be initialised. These two resources
# are intentionally NOT managed by this configuration so that `terraform destroy`
# can never remove the state that describes it.
#
#   aws s3api create-bucket --bucket virtual-wardrobe-tfstate --region us-east-1
#   aws s3api put-bucket-versioning --bucket virtual-wardrobe-tfstate \
#       --versioning-configuration Status=Enabled
#   aws dynamodb create-table --table-name virtual-wardrobe-tflock \
#       --attribute-definitions AttributeName=LockID,AttributeType=S \
#       --key-schema AttributeName=LockID,KeyType=HASH \
#       --billing-mode PAY_PER_REQUEST --region us-east-1

terraform {
  required_version = ">= 1.6.0"

  backend "s3" {
    bucket         = "virtual-wardrobe-tfstate"
    key            = "lightsail/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "virtual-wardrobe-tflock"
    encrypt        = true
  }
}
