# The single application host. Ubuntu 22.04 LTS, 1 GB bundle, bootstrapped by
# cloud-init (Docker + Compose plugin + swap + /opt/app).

resource "aws_lightsail_instance" "app" {
  name              = "vw-app"
  availability_zone = var.availability_zone
  blueprint_id      = "ubuntu_22_04"
  bundle_id         = var.instance_bundle
  key_pair_name     = aws_lightsail_key_pair.app.name
  user_data         = file("${path.module}/cloud-init.yaml")

  tags = {
    Role = "app-host"
  }
}
