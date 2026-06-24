# The existing wardrobe media bucket is RETAINED, not recreated. The import block
# adopts it into state on first apply; prevent_destroy guards it against destroy.yml.
# Lightsail has no instance role, so presign access uses a scoped IAM user whose
# access keys are injected into /opt/app/.env.

import {
  to = aws_s3_bucket.assets
  id = var.assets_bucket_name
}

resource "aws_s3_bucket" "assets" {
  bucket = var.assets_bucket_name

  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_s3_bucket_public_access_block" "assets" {
  bucket = aws_s3_bucket.assets.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_iam_user" "presign" {
  name = "vw-presign"
}

resource "aws_iam_access_key" "presign" {
  user = aws_iam_user.presign.name
}

data "aws_iam_policy_document" "presign" {
  statement {
    sid    = "PresignObjectAccess"
    effect = "Allow"
    actions = [
      "s3:GetObject",
      "s3:PutObject",
      "s3:DeleteObject",
    ]
    resources = ["${aws_s3_bucket.assets.arn}/*"]
  }
}

resource "aws_iam_user_policy" "presign" {
  name   = "vw-presign-s3"
  user   = aws_iam_user.presign.name
  policy = data.aws_iam_policy_document.presign.json
}
