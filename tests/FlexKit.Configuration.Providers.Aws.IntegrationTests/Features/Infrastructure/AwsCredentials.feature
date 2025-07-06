Feature: AWS Credentials Simple Configuration
As a developer testing AWS integration
I want to configure AWS credentials for LocalStack
So that AWS clients can connect properly

    @Infrastructure @AwsCredentials @Simple
    Scenario: Configure anonymous credentials for LocalStack
        Given I have started LocalStack container
        When I configure AWS with anonymous credentials
        Then AWS configuration should use anonymous credentials
        And AWS region should be "us-east-1"
        And I should be able to create AWS clients

    @Infrastructure @AwsCredentials @CustomOptions
    Scenario: Configure custom AWS options
        Given I have started LocalStack container
        When I configure AWS with custom region "us-west-2"
        Then AWS configuration should use "us-west-2" region
        And I should be able to create AWS clients for custom region

    @Infrastructure @AwsCredentials @ClientCreation
    Scenario: Create AWS service clients successfully
        Given I have started LocalStack container
        And I have configured AWS credentials for LocalStack
        When I create Parameter Store client
        Then Parameter Store client should be created successfully
        And Parameter Store client should connect to LocalStack
        When I create Secrets Manager client
        Then Secrets Manager client should be created successfully
        And Secrets Manager client should connect to LocalStack