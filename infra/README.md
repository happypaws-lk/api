<a href="https://github.com/happypaws-lk/happypaws-api/tree/main/infra" align="center">
    <img src="../.github/assets/banner.jpg" alt="HappyPaws API Infrastructure">
</a>

<p align="center">The central Infrastructure as Code (IaC) for the HappyPaws .NET Backend.</p>
  
<!-- Badges -->
<p align="center">
  <img src="https://img.shields.io/badge/Terraform-1.5+-7B42BC?style=flat&logo=terraform&labelColor=171717" alt="Terraform" />
  <img src="https://img.shields.io/badge/AWS-Amazon_Web_Services-232F3E?style=flat&logo=amazon-web-services&labelColor=171717" alt="AWS" />
  <img src="https://img.shields.io/badge/GitHub_Actions-2088FF?style=flat&logo=github-actions&labelColor=171717" alt="GitHub Actions" />
</p>

<h4 align="center">
    <a href="#introduction">Introduction</a> 
    <span> · </span>    
    <a href="#tech-stack">Tech stack</a>
    <span> · </span>
    <a href="#architecture">Architecture</a>
    <span> · </span>
    <a href="#agent-skills--best-practices">Agent skills</a>
</h4>

<br />

## Introduction

This repository contains the Terraform configurations required to provision the AWS production environment for the HappyPaws API. Our architecture is designed for a highly cost-optimized, secure, and minimal footprint suitable for early-stage deployments.

## Tech stack

- **Terraform 1.5+**: State management and provisioning.
- **AWS S3**: Remote backend for Terraform state, using native S3 state locking.
- **AWS Elastic Beanstalk**: Compute tier running `64bit Amazon Linux 2023 v4.13.5 running Docker`.
- **Amazon RDS (PostgreSQL 16.x)**: Managed relational database.
- **Amazon ECR**: Immutable container image registry for backend releases.
- **AWS SSM Parameter Store**: Centralized configuration management.

## Architecture

Our production environment (`environments/prod`) provisions the following components via modular Terraform blocks:

1. **Amazon VPC (`modules/aws/vpc`)**
   A custom VPC configured without NAT Gateways to minimize costs. It provides 2 Public Subnets for the compute tier and 2 Isolated Subnets for the database tier.

2. **Amazon ECR (`modules/aws/ecr`)**
   Stores our compiled .NET API Docker images. Configured with immutable image tags to prevent accidental overwrites of production images, and automatic vulnerability scanning on push.

3. **Amazon RDS (`modules/aws/rds`)**
   Runs PostgreSQL 16.x on a `db.t3.micro` instance in the isolated subnets. It includes deletion protection, KMS storage encryption at rest, and automated minor version upgrades. Credentials are automatically managed via AWS Secrets Manager.

4. **AWS Elastic Beanstalk (`modules/aws/elastic_beanstalk`)**
   Hosts our Minimal API inside a Docker container. It is configured as a `SingleInstance` environment running on a `t3.small` EC2 instance in the public subnets, eliminating Application Load Balancer costs. It securely connects to the RDS instance via an isolated security group configuration.

### AWS SSM Parameter Store

After Terraform initializes the infrastructure, essential outputs are exported to the AWS Systems Manager (SSM) Parameter Store. This allows other tools, scripts, or developers to easily retrieve the latest infrastructure values without running Terraform.

**Standard Parameters (Free):**
- `/happypaws/prod/vpc/vpc_id` (VPC ID)
- `/happypaws/prod/database/endpoint` (RDS Host endpoint)
- `/happypaws/prod/database/port` (RDS Port)
- `/happypaws/prod/database/name` (Database Name)
- `/happypaws/prod/eb/environment_url` (Elastic Beanstalk public URL)
- `/happypaws/prod/ecr/repository_url` (ECR Repo URL for CI/CD)

**Sensitive Parameters (SecureString):**
- `/happypaws/prod/database/secret_arn` (The ARN to the Secrets Manager payload containing the master DB password). 

You can retrieve these values via the AWS CLI or SDKs (e.g., `aws ssm get-parameter --name "/happypaws/prod/database/endpoint"`).

### State management

Terraform state is stored securely in an S3 bucket (`happypaws-terraform-state-prod`) in `ap-southeast-1`. State locking is handled natively by S3 (supported in Terraform >= 1.5.0), eliminating the need for a separate DynamoDB table.

## Deployment workflow

Follow these steps to provision your AWS infrastructure from scratch.

### 1. Prerequisites

Install the AWS CLI and Terraform (v1.5.0 or later) on your machine.

### 2. Authenticate with AWS

Provide the AWS CLI with credentials so Terraform can act on your behalf. Run the following command:

```bash
aws configure
```

You will be prompted for your IAM access key, IAM secret key, default region (`ap-southeast-1`), and output format (`json`). If your organization uses AWS SSO, run `aws sso login` instead.

### 3. Create the S3 state bucket

Terraform cannot create the S3 bucket where it plans to store its own state file. You must create this bucket manually before running Terraform.

```bash
aws s3api create-bucket \
  --bucket happypaws-terraform-state-prod \
  --region ap-southeast-1 \
  --create-bucket-configuration LocationConstraint=ap-southeast-1
```

Enable bucket versioning on this bucket via the AWS console to protect against accidental state deletion.

### 4. Initialize Terraform

Navigate to the production environment directory and initialize the project.

```bash
cd api/infra/environments/prod
terraform init
```

Run `terraform init` again if you add a new module, change a provider version, or update the backend configuration.

### 5. Provision the infrastructure

Always run a plan first to see exactly what AWS resources Terraform is going to create.

```bash
terraform plan
```

If the plan looks correct, execute the deployment. Type `yes` when prompted.

```bash
terraform apply
```

This will take about 5 to 10 minutes while the RDS database and Elastic Beanstalk environments spin up.

### 6. Access outputs and secrets

Once Terraform finishes, it exports your public endpoints and IDs to the SSM Parameter Store. You can retrieve them securely via the CLI later.

To get standard parameters like the database endpoint, run this command:

```bash
aws ssm get-parameter \
  --name "/happypaws/prod/database/endpoint" \
  --query "Parameter.Value" --output text
```

AWS Secrets Manager dynamically generated the database password natively, and we stored its ARN in a SecureString. To view the actual database password, get the secret ARN from SSM and then retrieve the payload.

```bash
SECRET_ARN=$(aws ssm get-parameter --name "/happypaws/prod/database/secret_arn" --with-decryption --query "Parameter.Value" --output text)
aws secretsmanager get-secret-value --secret-id $SECRET_ARN --query "SecretString" --output text
```

The payload is a JSON string containing the generated username and password.

### 7. Deploy the application

Terraform provisions the servers, but it does not deploy your actual application code. 

1. Build your Docker image locally.
2. Authenticate Docker to your new ECR repository.
   ```bash
   aws ecr get-login-password --region ap-southeast-1 | docker login --username AWS --password-stdin <your-aws-account-id>.dkr.ecr.ap-southeast-1.amazonaws.com
   ```
3. Tag and push your image to the ECR repository created by Terraform.
4. Deploy to Elastic Beanstalk using the EB CLI (`eb deploy`) or GitHub Actions. Provide it with a `docker-compose.yml` or `Dockerrun.aws.json` that points to the new image in ECR.
