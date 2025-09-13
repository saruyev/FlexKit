# Azure Resources Setup for FlexKit Configuration Testing

This document contains the Azure CLI commands used to create test resources for FlexKit Azure Configuration Providers testing.

## Prerequisites

1. Install Azure CLI
2. Login to Azure: `az login`
3. Set appropriate subscription: `az account set --subscription <subscription-id>`

## Resource Group Creation

```bash
az group create --name testing-rg --location eastus
```

## Azure Key Vault Setup

### Create Key Vault
```bash
az keyvault create \
    --name flexkittestkv1757629870 \
    --resource-group testing-rg \
    --location eastus \
    --enable-rbac-authorization false
```

**Key Vault URI**: `https://flexkittestkv1757629870.vault.azure.net/`

### Create Test Secrets

#### Simple String Secrets
```bash
# Database configuration secrets
az keyvault secret set --vault-name flexkittestkv1757629870 --name "flexkit--database--host" --value "azure-db.example.com"
az keyvault secret set --vault-name flexkittestkv1757629870 --name "flexkit--database--port" --value "5432"
az keyvault secret set --vault-name flexkittestkv1757629870 --name "flexkit--database--username" --value "flexkit_user"
az keyvault secret set --vault-name flexkittestkv1757629870 --name "flexkit--database--password" --value "SecurePassword123!"
```

#### JSON Secrets (for JSON processing testing)
```bash
# Features configuration as JSON
az keyvault secret set \
    --vault-name flexkittestkv1757629870 \
    --name "flexkit--features--config" \
    --value '{"caching": {"enabled": true, "ttl": 300}, "logging": {"level": "Information", "enableConsole": true}}'

# API configuration as JSON
az keyvault secret set \
    --vault-name flexkittestkv1757629870 \
    --name "flexkit--api--config" \
    --value '{"baseUrl": "https://api.azure.example.com", "timeout": 30, "retryCount": 3}'
```

## Azure App Configuration Setup

### Create App Configuration Store
```bash
az appconfig create \
    --name flexkit-test-config-1757629946 \
    --resource-group testing-rg \
    --location eastus \
    --sku free
```

**App Configuration Endpoint**: `https://flexkit-test-config-1757629946.azconfig.io`

### Create Test Configuration Keys

#### Simple Configuration Values
```bash
az appconfig kv set --name flexkit-test-config-1757629946 --key "flexkit:database:host" --value "appconfig-db.example.com" --yes
az appconfig kv set --name flexkit-test-config-1757629946 --key "flexkit:database:port" --value "5432" --yes
az appconfig kv set --name flexkit-test-config-1757629946 --key "flexkit:api:baseUrl" --value "https://api.appconfig.example.com" --yes
```

#### JSON Configuration Value
```bash
az appconfig kv set \
    --name flexkit-test-config-1757629946 \
    --key "flexkit:features:config" \
    --value '{"featureFlags": {"enableNewUI": true, "enableBetaFeatures": false}, "limits": {"maxUsers": 1000, "maxRequests": 5000}}' \
    --yes
```

#### Labeled Configuration (for environment-specific testing)
```bash
# Development environment
az appconfig kv set \
    --name flexkit-test-config-1757629946 \
    --key "flexkit:logging:level" \
    --value "Debug" \
    --label "development" \
    --yes

# Production environment
az appconfig kv set \
    --name flexkit-test-config-1757629946 \
    --key "flexkit:logging:level" \
    --value "Information" \
    --label "production" \
    --yes
```

## Test Scenarios Covered

### Azure Key Vault Features
1. **Basic Secret Retrieval**: Simple string secrets with hierarchical naming (`flexkit--database--host`)
2. **JSON Processing**: Complex JSON secrets that can be flattened into configuration hierarchy
3. **Secret Name Transformation**: Azure Key Vault naming (`--`) to .NET configuration keys (`:`)
4. **Selective JSON Processing**: Ability to process only specific secrets as JSON

### Azure App Configuration Features
1. **Basic Configuration**: Simple key-value pairs with hierarchical keys
2. **JSON Processing**: Complex JSON values that can be flattened
3. **Label-based Filtering**: Environment-specific configuration using labels
4. **Key Filtering**: Loading only keys matching specific patterns
5. **Connection String vs. Endpoint**: Support for both connection string and endpoint + credential approaches

## Cleanup Commands

When testing is complete, use these commands to clean up resources:

```bash
# Delete the entire resource group (removes all resources)
az group delete --name testing-rg --yes --no-wait

# Or delete individual resources
az keyvault delete --name flexkittestkv1757629870 --resource-group testing-rg
az appconfig delete --name flexkit-test-config-1757629946 --resource-group testing-rg --yes
```

## Security Notes

1. **Access Policies**: The Key Vault is created with access policies (not RBAC) for simplicity
2. **Default Credentials**: The test setup uses default Azure credentials (Azure CLI login)
3. **Test Data**: All secrets and configuration contain non-sensitive test data only
4. **Public Access**: Resources are configured for public access for testing purposes

## Testing Considerations

- Key Vault name follows Azure naming requirements (3-24 alphanumeric characters)
- App Configuration store uses the free tier (sufficient for testing)
- Resources are created in East US region
- All configuration values are designed to test different FlexKit features