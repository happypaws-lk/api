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
- **Amazon RDS (PostgreSQL 16.3)**: Managed relational database.
- **Amazon ECR**: Immutable container image registry for backend releases.
- **AWS SSM Parameter Store**: Centralized configuration management.

## Architecture

Our production environment (`environments/prod`) provisions the following components via modular Terraform blocks:

1. **Amazon VPC (`modules/aws/vpc`)**
   A custom VPC configured without NAT Gateways to minimize costs. It provides 2 Public Subnets for the compute tier and 2 Isolated Subnets for the database tier.

2. **Amazon ECR (`modules/aws/ecr`)**
   Stores our compiled .NET API Docker images. Configured with immutable image tags to prevent accidental overwrites of production images, and automatic vulnerability scanning on push.

3. **Amazon RDS (`modules/aws/rds`)**
   Runs PostgreSQL 16.3 on a `db.t3.micro` instance in the isolated subnets. It includes deletion protection, KMS storage encryption at rest, and automated minor version upgrades. Credentials are automatically managed via AWS Secrets Manager.

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

### State Management

Terraform state is stored securely in an S3 bucket (`happypaws-terraform-state-prod`) in `ap-southeast-1`. State locking is handled natively by S3 (supported in Terraform >= 1.5.0), eliminating the need for a separate DynamoDB table.
