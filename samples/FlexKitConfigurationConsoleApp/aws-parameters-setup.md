# AWS Parameter Store Setup for FlexKit Configuration Demo

This file contains all the AWS CLI commands used to create the test parameters for demonstrating FlexKit.Configuration.Providers.Aws features.

## Prerequisites

1. AWS CLI installed and configured
2. AWS credentials with appropriate permissions for Parameter Store
3. Appropriate IAM permissions for creating and reading parameters

## Required IAM Permissions

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ssm:GetParameter",
                "ssm:GetParameters",
                "ssm:GetParametersByPath",
                "ssm:PutParameter",
                "ssm:DeleteParameter",
                "ssm:DescribeParameters"
            ],
            "Resource": [
                "arn:aws:ssm:*:*:parameter/flexkit/*",
                "arn:aws:ssm:*:*:parameter/flexkit-test/*"
            ]
        }
    ]
}
```

## Parameter Store Structure

The test parameters follow this hierarchy:
```
/flexkit-test/
├── development/
│   ├── application/
│   ├── database/
│   ├── api/
│   ├── features/
│   └── monitoring/
├── production/
│   ├── application/
│   ├── database/
│   ├── api/
│   └── features/
└── shared/
    ├── cache/
    ├── logging/
    └── security/
```

## AWS CLI Commands to Create Test Parameters

### 1. Application Configuration Parameters

```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/application/name" \
    --value "FlexKit Configuration Demo (AWS Parameter Store)" \
    --type "String" \
    --description "Application name from Parameter Store"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/application/version" \
    --value "2.1.0" \
    --type "String" \
    --description "Application version from Parameter Store"
```
```bash

aws ssm put-parameter \
    --name "/flexkit-test/development/application/environment" \
    --value "Development" \
    --type "String" \
    --description "Current application environment"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/application/debug-enabled" \
    --value "true" \
    --type "String" \
    --description "Debug mode enabled flag"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/application/max-concurrent-users" \
    --value "500" \
    --type "String" \
    --description "Maximum concurrent users allowed"
```

### 2. Database Configuration Parameters

```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/database/connection-string" \
    --value "Server=aws-db.example.com;Database=FlexKitDemo_AWS;Integrated Security=true;TrustServerCertificate=true;" \
    --type "SecureString" \
    --description "Database connection string (encrypted)"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/database/command-timeout" \
    --value "60" \
    --type "String" \
    --description "Database command timeout in seconds"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/database/max-retries" \
    --value "5" \
    --type "String" \
    --description "Maximum database retry attempts"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/database/connection-pool-size" \
    --value "25" \
    --type "String" \
    --description "Database connection pool size"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/database/providers" \
    --value '{
        "Primary": {
            "Host": "aws-primary-db.example.com",
            "Port": 5432,
            "Database": "flexkit_aws_primary",
            "Username": "aws_primary_user",
            "SslMode": "Require",
            "MaxConnections": 50
        },
        "ReadReplica": {
            "Host": "aws-replica-db.example.com", 
            "Port": 5432,
            "Database": "flexkit_aws_primary",
            "Username": "aws_readonly_user",
            "SslMode": "Require",
            "ReadOnly": true,
            "MaxConnections": 25
        },
        "Analytics": {
            "Host": "aws-analytics-db.example.com",
            "Port": 5432,
            "Database": "flexkit_aws_analytics",
            "Username": "aws_analytics_user",
            "SslMode": "Require",
            "MaxConnections": 10
        }
    }' \
    --type "String" \
    --description "Database providers configuration (JSON)"
```

### 3. API Configuration Parameters

```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/api/base-url" \
    --value "https://api.aws.flexkit-demo.com" \
    --type "String" \
    --description "Base URL for external API"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/api/timeout" \
    --value "45000" \
    --type "String" \
    --description "API timeout in milliseconds"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/api/retry-count" \
    --value "5" \
    --type "String" \
    --description "API retry attempts"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/api/api-key" \
    --value "aws-secret-api-key-67890" \
    --type "SecureString" \
    --description "API key for external service (encrypted)"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/api/external-services" \
    --value '{
        "PaymentGateway": {
            "Name": "AWS Stripe Payment Gateway",
            "BaseUrl": "https://api.stripe.com/v1",
            "Timeout": 30000,
            "RetryCount": 3,
            "Authentication": {
                "Type": "Bearer",
                "TokenEndpoint": "https://connect.stripe.com/oauth/token"
            },
            "RateLimit": {
                "RequestsPerMinute": 2000,
                "BurstLimit": 100
            }
        },
        "NotificationService": {
            "Name": "AWS SES Notification Service",
            "BaseUrl": "https://email.us-east-1.amazonaws.com",
            "Channels": {
                "Email": {
                    "Provider": "AWS SES",
                    "DefaultFromAddress": "noreply@aws.flexkit-demo.com"
                },
                "SNS": {
                    "Provider": "AWS SNS",
                    "TopicArn": "arn:aws:sns:us-east-1:123456789012:flexkit-notifications"
                }
            }
        }
    }' \
    --type "String" \
    --description "External services configuration (JSON)"
```

### 4. Features Configuration Parameters

```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/features/enable-new-dashboard" \
    --value "true" \
    --type "String" \
    --description "Enable new React dashboard"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/features/enable-analytics" \
    --value "true" \
    --type "String" \
    --description "Enable Google Analytics integration"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/features/enable-advanced-search" \
    --value "false" \
    --type "String" \
    --description "Enable advanced search features"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/features/cache-expiry-minutes" \
    --value "90" \
    --type "String" \
    --description "Cache expiration time in minutes"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/features/advanced-features" \
    --value '{
        "Dashboard": {
            "RefreshIntervalSeconds": 45,
            "MaxWidgets": 15,
            "EnableCustomization": true,
            "DefaultLayout": "grid"
        },
        "Search": {
            "Provider": "AWS OpenSearch",
            "IndexPrefix": "flexkit_aws_",
            "MaxResults": 2000,
            "EnableAutoComplete": true,
            "EnableSpellCheck": true
        },
        "UserProfiles": {
            "EnableProfilePictures": true,
            "MaxProfilePictureSize": 10485760,
            "AllowedFormats": ["jpg", "jpeg", "png", "webp"]
        }
    }' \
    --type "String" \
    --description "Advanced features configuration (JSON)"
```

### 5. Monitoring and Logging Parameters

```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/monitoring/metrics-interval" \
    --value "20" \
    --type "String" \
    --description "Metrics collection interval in seconds"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/monitoring/health-check-interval" \
    --value "30" \
    --type "String" \
    --description "Health check interval in seconds"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/monitoring/enable-detailed-logging" \
    --value "true" \
    --type "String" \
    --description "Enable detailed application logging"
```
```bash

aws ssm put-parameter \
    --name "/flexkit-test/development/monitoring/cloudwatch-config" \
    --value '{
        "LogGroup": "/aws/flexkit/configuration-demo",
        "LogStream": "application-logs",
        "MetricsNamespace": "FlexKit/Configuration",
        "EnableXRayTracing": true,
        "CustomMetrics": [
            "configuration_loads_total",
            "parameter_store_requests_total",
            "configuration_errors_total"
        ],
        "Alarms": {
            "ConfigurationErrorRate": {
                "Threshold": 0.05,
                "ComparisonOperator": "GreaterThanThreshold",
                "EvaluationPeriods": 2
            }
        }
    }' \
    --type "String" \
    --description "CloudWatch monitoring configuration (JSON)"
```

### 6. Shared Configuration Parameters

```bash
aws ssm put-parameter \
    --name "/flexkit-test/shared/cache/provider" \
    --value "AWS ElastiCache" \
    --type "String" \
    --description "Cache provider name"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/shared/cache/connection-string" \
    --value "redis-cluster.aws.example.com:6379" \
    --type "String" \
    --description "Cache connection string"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/shared/cache/default-ttl-minutes" \
    --value "60" \
    --type "String" \
    --description "Default cache TTL in minutes"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/shared/logging/log-level" \
    --value "Information" \
    --type "String" \
    --description "Default log level"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/shared/logging/enable-structured-logging" \
    --value "true" \
    --type "String" \
    --description "Enable structured logging"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/shared/security/jwt-secret" \
    --value "aws-jwt-super-secret-key-for-flexkit-demo-app" \
    --type "SecureString" \
    --description "JWT signing secret (encrypted)"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/shared/security/encryption-key" \
    --value "aws-encryption-key-for-sensitive-data-protection" \
    --type "SecureString" \
    --description "Data encryption key (encrypted)"
```

### 7. Production Environment Parameters (for comparison)

```bash
aws ssm put-parameter \
    --name "/flexkit-test/production/application/name" \
    --value "FlexKit Configuration Demo (Production - AWS)" \
    --type "String" \
    --description "Production application name"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/production/application/debug-enabled" \
    --value "false" \
    --type "String" \
    --description "Debug mode disabled in production"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/production/application/max-concurrent-users" \
    --value "10000" \
    --type "String" \
    --description "Production concurrent users limit"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/production/database/connection-string" \
    --value "Server=prod-aws-db.example.com;Database=FlexKitDemo_Prod_AWS;User Id=prod_user;Password=prod_secure_password;" \
    --type "SecureString" \
    --description "Production database connection (encrypted)"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/production/database/command-timeout" \
    --value "30" \
    --type "String" \
    --description "Production database timeout"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/production/database/max-retries" \
    --value "3" \
    --type "String" \
    --description "Production database retries"
```

### 8. Array Configuration Examples

```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/servers/0/name" \
    --value "AWS Web Server 1" \
    --type "String" \
    --description "First web server name"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/servers/0/host" \
    --value "web1.aws.flexkit-demo.com" \
    --type "String" \
    --description "First web server host"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/servers/0/port" \
    --value "8080" \
    --type "String" \
    --description "First web server port"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/servers/0/is-active" \
    --value "true" \
    --type "String" \
    --description "First web server active status"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/servers/1/name" \
    --value "AWS API Server" \
    --type "String" \
    --description "API server name"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/servers/1/host" \
    --value "api.aws.flexkit-demo.com" \
    --type "String" \
    --description "API server host"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/servers/1/port" \
    --value "8090" \
    --type "String" \
    --description "API server port"
```
```bash
aws ssm put-parameter \
    --name "/flexkit-test/development/servers/1/is-active" \
    --value "true" \
    --type "String" \
    --description "API server active status"
```

## Parameter Store Testing Features

The created parameters demonstrate:

1. **String Parameters**: Simple key-value pairs
2. **SecureString Parameters**: Encrypted sensitive data
3. **JSON Parameters**: Complex hierarchical structures
4. **Array Structures**: Using indexed parameter names
5. **Environment-specific Paths**: Development vs Production
6. **Shared Parameters**: Cross-environment configuration
7. **Various Data Types**: Strings, numbers, booleans, complex objects

## Verification Commands

To verify the parameters were created successfully:

```bash
aws ssm describe-parameters --filters "Key=Name,Values=/flexkit-test"
```
```bash
aws ssm get-parameters-by-path --path "/flexkit-test/development" --recursive
```
```bash 
aws ssm get-parameters-by-path --path "/flexkit-test/shared" --recursive
```
```bash
aws ssm get-parameter --name "/flexkit-test/development/application/name"
```
```bash
aws ssm get-parameter --name "/flexkit-test/development/database/connection-string" --with-decryption
```

## Cleanup Commands (DO NOT RUN - for reference only)

```bash
aws ssm delete-parameters --names $(aws ssm describe-parameters --filters "Key=Name,Values=/flexkit-test" --query "Parameters[].Name" --output text)
```

## Cost Considerations

- Standard parameters: Free tier includes 10,000 requests per month
- Advanced parameters: $0.05 per 10,000 requests
- SecureString parameters: Use AWS KMS for encryption (additional costs may apply)
- Monitor usage through AWS Cost Explorer and CloudWatch