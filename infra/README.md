<a href="https://github.com/happypaws-lk/api/tree/main/infra" align="center">
    <img src="../.github/assets/banner.jpg" alt="HappyPaws API Infrastructure">
</a>

<p align="center">Infrastructure as Code for the HappyPaws .NET API.</p>

<!-- Badges -->
<p align="center">
  <img src="https://img.shields.io/badge/Terraform-1.10+-7B42BC?style=flat&logo=terraform&labelColor=171717" alt="Terraform" />
  <img src="https://img.shields.io/badge/AWS-Amazon_Web_Services-232F3E?style=flat&logo=amazon-web-services&labelColor=171717" alt="AWS" />
  <img src="https://img.shields.io/badge/GitHub_Actions-2088FF?style=flat&logo=github-actions&labelColor=171717" alt="GitHub Actions" />
</p>

<h4 align="center">
    <a href="#stack">Stack</a>
    <span> · </span>
    <a href="#architecture">Architecture</a>
    <span> · </span>
    <a href="#ssm-parameters">SSM parameters</a>
    <span> · </span>
    <a href="#bootstrap">Bootstrap</a>
    <span> · </span>
    <a href="#deployment">Deployment</a>
    <span> · </span>
    <a href="#scaling">Scaling</a>
</h4>

<br />

## Stack

- **Terraform 1.10+** with native S3 state locking (no DynamoDB required)
- **AWS Lightsail Container Service** — managed container hosting, ~$10/mo for `small`
- **AWS Lightsail Managed PostgreSQL 16** — managed database, ~$15/mo for `micro_2_0`
- **AWS SSM Parameter Store** — all configuration lives here, SecureString for secrets
- **GitHub Actions OIDC** — zero static AWS credentials stored anywhere

**Total estimated cost: ~$25–26/mo at MVP scale.**

## Architecture

```
GitHub Actions (OIDC role — no static keys)
    │
    │  1. docker build
    │  2. aws lightsail push-container-image
    │  3. aws ssm get-parameters (fetch all secrets)
    │  4. aws lightsail create-container-service-deployment
    │
    ▼
Lightsail Container Service "happypaws-api"
  power=small, scale=1 (adjustable in tfvars)
    │
    │ private Lightsail network (DB not public)
    ▼
Lightsail Managed PostgreSQL "happypaws-db"
  postgres_16, micro_2_0
```

The container service and managed DB share Lightsail's private internal network. The database is not publicly accessible — traffic from outside AWS cannot reach it directly.

## File structure

```
infra/
├── environments/
│   └── prod/
│       ├── terraform.tf             # backend + required_providers
│       ├── providers.tf             # AWS provider, default tags
│       ├── variables.tf             # all input variables
│       ├── locals.tf                # DATABASE_URL + SSM path registry
│       ├── main.tf                  # all resources
│       ├── outputs.tf               # post-apply reference values
│       └── terraform.tfvars.example # copy → terraform.tfvars to get started
└── .agents/                         # agent skills for Terraform authoring
```

## SSM parameters

All configuration is stored in SSM. The GitHub Actions workflow reads these at deploy time and injects them as container environment variables. Terraform writes every value — you never set them manually.

### Plain strings (free)

| Path | Value |
|---|---|
| `/happypaws/prod/container_service_name` | Lightsail service name |
| `/happypaws/prod/container_service_url` | Public URL of the API |
| `/happypaws/prod/storage/account_id` | Cloudflare account ID |

### SecureStrings (~$0.05/param/mo)

| Path | Source |
|---|---|
| `/happypaws/prod/database_url` | Built by Terraform from DB outputs |
| `/happypaws/prod/ses/access_key_id` | Auto-generated IAM access key |
| `/happypaws/prod/ses/secret_access_key` | Auto-generated IAM secret |
| `/happypaws/prod/jwt_key` | From `terraform.tfvars` |
| `/happypaws/prod/gemini_api_key` | From `terraform.tfvars` |
| `/happypaws/prod/firebase_service_account_json` | From `terraform.tfvars` |
| `/happypaws/prod/storage/access_key` | From `terraform.tfvars` |
| `/happypaws/prod/storage/secret_key` | From `terraform.tfvars` |

**SES note:** Lightsail Container Service does not expose the EC2 Instance Metadata Service, so IAM instance roles do not work inside containers. Terraform provisions a dedicated least-privilege IAM user (`happypaws-ses-sender`) with only `ses:SendEmail` and `ses:SendRawEmail`, generates its access key, and stores both values in SSM automatically. You never touch SES credentials manually.

## Bootstrap

Follow these steps once to provision the production environment from scratch.

### 1. Prerequisites

Install the AWS CLI and Terraform 1.10 or later.

```bash
aws configure   # or aws sso login
```

Set the default region to `ap-southeast-1`.

### 2. Create the S3 state bucket

Terraform cannot create the bucket it stores its own state in.

```bash
# Create the bucket
aws s3api create-bucket \
  --bucket happypaws-terraform-state \
  --region ap-southeast-1 \
  --create-bucket-configuration LocationConstraint=ap-southeast-1

# Enable versioning to protect against accidental state deletion
aws s3api put-bucket-versioning \
  --bucket happypaws-terraform-state \
  --versioning-configuration Status=Enabled
```

### 3. Prepare your tfvars file

```bash
cd api/infra/environments/prod
cp terraform.tfvars.example terraform.tfvars
# Fill in all placeholder values
```

### 4. Initialise and apply

```bash
terraform init
terraform plan   # review the plan
terraform apply  # type yes when prompted
```

Apply takes roughly 5–10 minutes while the managed database spins up.

### 5. Retrieve the deploy role ARN

After apply, copy the role ARN to your GitHub repository's secrets as `AWS_DEPLOY_ROLE_ARN`.

```bash
terraform output github_actions_role_arn
```

That is the only secret you add to GitHub. All application config is in SSM.

### 6. Retrieve other outputs

```bash
# API public URL
terraform output container_service_url

# Database host (for troubleshooting — use DATABASE_URL for the API)
terraform output database_endpoint
```

To read any SSM value:

```bash
aws ssm get-parameter \
  --name "/happypaws/prod/database_url" \
  --with-decryption \
  --query "Parameter.Value" \
  --output text
```

## Deployment

Terraform provisions the infrastructure but does not deploy the application. Deployments run via the `CD` GitHub Actions workflow, which:

1. Authenticates with AWS via OIDC (no stored credentials)
2. Builds the Docker image
3. Pushes it to the Lightsail built-in container registry via `aws lightsail push-container-image`
4. Reads all configuration from SSM
5. Creates a new deployment with the image and all env vars injected inline
6. Polls until the service reaches `RUNNING` state

Trigger a manual deploy from the GitHub Actions tab using `workflow_dispatch`.

## Scaling

To switch to high-availability mode (three nodes with automatic load balancing):

```hcl
# terraform.tfvars
container_service_scale = 3
```

Then run `terraform apply`. No redeployment of the application is needed — Lightsail handles node provisioning and traffic distribution automatically.

**Note:** SignalR is stateful. If you scale beyond one node, add a Redis backplane (`AddStackExchangeRedis()` in `AddSignalR()`). Upstash Redis (serverless, free tier) is the simplest option.

## Rotating secrets

Most SSM SecureStrings use `lifecycle { ignore_changes = [value] }` in Terraform, meaning a re-apply will not overwrite a value you have rotated manually. To rotate:

```bash
aws ssm put-parameter \
  --name "/happypaws/prod/jwt_key" \
  --value "<new-value>" \
  --type SecureString \
  --overwrite
```

The new value takes effect on the next deployment (the workflow always reads fresh SSM values).

The SES access key is tied to the `aws_iam_access_key.ses_sender` Terraform resource and does not have `ignore_changes`. To rotate it, run `terraform apply` after running `terraform taint aws_iam_access_key.ses_sender`.
