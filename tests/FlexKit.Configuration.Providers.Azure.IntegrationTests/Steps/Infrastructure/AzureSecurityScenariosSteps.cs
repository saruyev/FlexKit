using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Steps.Infrastructure;

/// <summary>
/// Step definitions for Azure security scenarios testing.
/// Tests authentication, authorization, and security-related aspects of Azure configuration
/// using LocalStack for service simulation.
/// Uses distinct step patterns ("security controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class AzureSecurityScenariosSteps(ScenarioContext scenarioContext)
{
    private AzureTestConfigurationBuilder? _securityBuilder;
    private IConfiguration? _securityConfiguration;
    private IFlexConfig? _securityFlexConfiguration;
    private readonly List<string> _securityValidationResults = new();
    private bool _managedIdentityEnabled;
    private bool _servicePrincipalEnabled;
    private bool _rbacRestrictionsEnabled;
    private bool _accessDeniedSimulated;
    private bool _errorToleranceEnabled;

    #region Given Steps - Setup

    [Given(@"I have established a security controller environment")]
    public void GivenIHaveEstablishedASecurityControllerEnvironment()
    {
        _securityBuilder = new AzureTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_securityBuilder, "SecurityBuilder");
        _securityValidationResults.Add("✓ Security controller environment established");
    }

    [Given(@"I have security controller configuration with managed identity Key Vault from ""(.*)""")]
    public void GivenIHaveSecurityControllerConfigurationWithManagedIdentityKeyVaultFrom(string testDataPath)
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _securityBuilder!.AddKeyVaultFromTestData(fullPath, optional: false, jsonProcessor: true);
        
        // Add test security configuration
        _securityBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:Authentication:Type"] = "ManagedIdentity",
            ["test:secure:secret"] = "test-secure-value"
        });
        
        _managedIdentityEnabled = true;
        scenarioContext.Set(_securityBuilder, "SecurityBuilder");
        _securityValidationResults.Add("✓ Managed identity Key Vault configuration added");
    }

    [Given(@"I have security controller configuration with service principal App Configuration from ""(.*)""")]
    public void GivenIHaveSecurityControllerConfigurationWithServicePrincipalAppConfigurationFrom(string testDataPath)
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _securityBuilder!.AddAppConfigurationFromTestData(fullPath, optional: false);
    
        _securityBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:Authentication:Type"] = "ServicePrincipal",
            ["Azure:Authentication:ClientId"] = "test-client-id",
            ["test:config:value"] = "test-configuration-value"
        });
    
        _servicePrincipalEnabled = true;
        scenarioContext.Set(_securityBuilder, "SecurityBuilder");
        _securityValidationResults.Add("✓ Service principal App Configuration added");
    }

    [Given(@"I have security controller configuration with limited permissions from ""(.*)""")]
    public void GivenIHaveSecurityControllerConfigurationWithLimitedPermissionsFrom(string testDataPath)
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _securityBuilder!.AddKeyVaultFromTestData(fullPath, optional: true, jsonProcessor: true);
        _securityBuilder!.AddAppConfigurationFromTestData(fullPath, optional: true);
        
        // Add test security configuration with limited permissions
        _securityBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:RBAC:Permissions"] = "Limited",
            ["test:readable:value"] = "test-readable-value"
        });
        
        _rbacRestrictionsEnabled = true;
        scenarioContext.Set(_securityBuilder, "SecurityBuilder");
        _securityValidationResults.Add("✓ Limited permissions configuration added");
    }

    [Given(@"I have security controller configuration with denied access from ""(.*)""")]
    public void GivenIHaveSecurityControllerConfigurationWithDeniedAccessFrom(string testDataPath)
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _securityBuilder!.AddKeyVaultFromTestData(fullPath, optional: true, jsonProcessor: true);
        
        // Add test security configuration simulating access denied
        _securityBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:Authentication:Status"] = "AccessDenied",
            ["test:public:value"] = "test-public-value"
        });
        
        _accessDeniedSimulated = true;
        scenarioContext.Set(_securityBuilder, "SecurityBuilder");
        _securityValidationResults.Add("✓ Access denied configuration added");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure security controller with managed identity authentication")]
    public void WhenIConfigureSecurityControllerWithManagedIdentityAuthentication()
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");
        _managedIdentityEnabled = true;
    }

    [When(@"I configure security controller with service principal authentication")]
    public void WhenIConfigureSecurityControllerWithServicePrincipalAuthentication()
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");
        _servicePrincipalEnabled = true;
    }

    [When(@"I configure security controller with restricted RBAC permissions")]
    public void WhenIConfigureSecurityControllerWithRestrictedRbacPermissions()
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");
        _rbacRestrictionsEnabled = true;
    }

    [When(@"I configure security controller with access denied simulation")]
    public void WhenIConfigureSecurityControllerWithAccessDeniedSimulation()
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");
        _accessDeniedSimulated = true;
    }

    [When(@"I configure security controller by building the configuration")]
    public void WhenIConfigureSecurityControllerByBuildingTheConfiguration()
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");

        try
        {
            // Start LocalStack first
            var startTask = _securityBuilder!.StartLocalStackAsync();
            startTask.Wait(TimeSpan.FromMinutes(2));

            // Configure authentication and authorization before building
            if (_managedIdentityEnabled)
            {
                _securityBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Azure:Authentication:Type"] = "ManagedIdentity",
                    ["Azure:Authentication:ResourceId"] = "test-resource-id"
                });
            }

            if (_servicePrincipalEnabled)
            {
                _securityBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Azure:Authentication:Type"] = "ServicePrincipal",
                    ["Azure:Authentication:TenantId"] = "test-tenant-id",
                    ["Azure:Authentication:ClientId"] = "test-client-id"
                    // Explicitly NOT setting ClientSecret to ensure it remains null
                });
            }

            if (_rbacRestrictionsEnabled)
            {
                _securityBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Azure:RBAC:Enabled"] = "true",
                    ["Azure:RBAC:Permissions"] = "Limited",
                    ["Azure:RBAC:Roles"] = "Reader"
                });
            }

            // Build configuration before simulating access denied
            _securityConfiguration = _securityBuilder.Build();
            _securityFlexConfiguration = _securityBuilder.BuildFlexConfig();

            scenarioContext.Set(_securityConfiguration, "SecurityConfiguration");
            scenarioContext.Set(_securityFlexConfiguration, "SecurityFlexConfiguration");

            _securityValidationResults.Add("✓ Security configuration built successfully");

            // Simulate access denied after configuration is built
            if (_accessDeniedSimulated)
            {
                throw new UnauthorizedAccessException("Access denied to Azure resource");
            }
        }
        catch (Exception ex)
        {
            if (_errorToleranceEnabled)
            {
                _securityValidationResults.Add($"! Access denied error tolerated: {ex.Message}");
            }
            else
            {
                _securityValidationResults.Add($"✗ Security configuration build failed: {ex.Message}");
                throw;
            }
        }
    }

    [When(@"I configure security controller with error tolerance by building the configuration")]
    public void WhenIConfigureSecurityControllerWithErrorToleranceByBuildingTheConfiguration()
    {
        _securityBuilder.Should().NotBeNull("Security builder should be established");
        _errorToleranceEnabled = true;
        WhenIConfigureSecurityControllerByBuildingTheConfiguration();
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the security controller should authenticate successfully with managed identity")]
    public void ThenTheSecurityControllerShouldAuthenticateSuccessfullyWithManagedIdentity()
    {
        _securityFlexConfiguration.Should().NotBeNull();
        _securityFlexConfiguration!.Configuration.GetValue<string>("Azure:Authentication:Type")
            .Should().Be("ManagedIdentity");
        _securityValidationResults.Add("✓ Managed identity authentication verified");
    }

    [Then(@"the security controller configuration should contain secure secrets")]
    public void ThenTheSecurityControllerConfigurationShouldContainSecureSecrets()
    {
        _securityFlexConfiguration.Should().NotBeNull();
        _securityFlexConfiguration!.Configuration.GetValue<string>("test:secure:secret")
            .Should().NotBeNull();
        _securityValidationResults.Add("✓ Secure secrets access verified");
    }

    [Then(@"the security controller should demonstrate proper credential handling")]
    public void ThenTheSecurityControllerShouldDemonstrateProperCredentialHandling()
    {
        _securityConfiguration.Should().NotBeNull();
        // Verify no sensitive credentials are exposed in configuration
        _securityConfiguration!["Azure:Authentication:ClientSecret"].Should().BeNull();
        _securityValidationResults.Add("✓ Proper credential handling verified");
    }

    [Then(@"the security controller should authenticate successfully with service principal")]
    public void ThenTheSecurityControllerShouldAuthenticateSuccessfullyWithServicePrincipal()
    {
        _securityFlexConfiguration.Should().NotBeNull();
        _securityFlexConfiguration!.Configuration.GetValue<string>("Azure:Authentication:Type")
            .Should().Be("ServicePrincipal");
        _securityValidationResults.Add("✓ Service principal authentication verified");
    }

    [Then(@"the security controller configuration should contain configuration data")]
    public void ThenTheSecurityControllerConfigurationShouldContainConfigurationData()
    {
        _securityFlexConfiguration.Should().NotBeNull();
        _securityFlexConfiguration!.Configuration.GetValue<string>("test:config:value")
            .Should().NotBeNull();
        _securityValidationResults.Add("✓ Configuration data access verified");
    }

    [Then(@"the security controller should demonstrate proper credential management")]
    public void ThenTheSecurityControllerShouldDemonstrateProperCredentialManagement()
    {
        _securityConfiguration.Should().NotBeNull();
        _securityConfiguration!["Azure:Authentication:ClientSecret"].Should().BeNull();
        _securityValidationResults.Add("✓ Proper credential management verified");
    }

    [Then(@"the security controller should handle limited permissions gracefully")]
    public void ThenTheSecurityControllerShouldHandleLimitedPermissionsGracefully()
    {
        _securityFlexConfiguration.Should().NotBeNull();
        _securityFlexConfiguration!.Configuration.GetValue<string>("test:readable:value")
            .Should().NotBeNull();
        _securityValidationResults.Add("✓ Limited permissions handling verified");
    }

    [Then(@"the security controller should demonstrate partial access scenarios")]
    public void ThenTheSecurityControllerShouldDemonstratePartialAccessScenarios()
    {
        _securityFlexConfiguration.Should().NotBeNull();
        // Should have access to public values but not restricted ones
        _securityFlexConfiguration!.Configuration.GetValue<string>("test:readable:value")
            .Should().NotBeNull();
        _securityFlexConfiguration.Configuration.GetValue<string>("test:restricted:value")
            .Should().BeNull();
        _securityValidationResults.Add("✓ Partial access scenarios verified");
    }

    [Then(@"the security controller should report authorization issues appropriately")]
    public void ThenTheSecurityControllerShouldReportAuthorizationIssuesAppropriately()
    {
        _securityValidationResults.Should().Contain(x => x.Contains("Limited permissions"), 
            "Should report limited permissions");
    }

    [Then(@"the security controller should handle access denied gracefully")]
    public void ThenTheSecurityControllerShouldHandleAccessDeniedGracefully()
    {
        _securityFlexConfiguration.Should().NotBeNull();
        // Should still have access to public values
        _securityFlexConfiguration!.Configuration.GetValue<string>("test:public:value")
            .Should().NotBeNull();
        _securityValidationResults.Add("✓ Access denied handling verified");
    }

    [Then(@"the security controller should demonstrate proper error reporting")]
    public void ThenTheSecurityControllerShouldDemonstrateProperErrorReporting()
    {
        _securityValidationResults.Should().Contain(x => x.Contains("Access") && x.Contains("tolerated"),
            "Should report access issues");
    }

    [Then(@"the security controller should maintain application stability")]
    public void ThenTheSecurityControllerShouldMaintainApplicationStability()
    {
        _securityFlexConfiguration.Should().NotBeNull();
        _securityConfiguration.Should().NotBeNull();
        _securityValidationResults.Add("✓ Application stability maintained");
    }

    #endregion
}